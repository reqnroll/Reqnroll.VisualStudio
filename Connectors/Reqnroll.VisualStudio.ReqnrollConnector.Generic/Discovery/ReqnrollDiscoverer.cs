using Reqnroll.VisualStudio.ReqnrollConnector.Models;
using StepDefinition = ReqnrollConnector.ReqnrollProxies.StepDefinition;

namespace ReqnrollConnector.Discovery;

public class ReqnrollDiscoverer
{
    private readonly IAnalyticsContainer _analytics;
    private readonly SymbolReaderCache _symbolReaders;
    // ReSharper disable once NotAccessedField.Local
    private readonly ILogger _log;

    public ReqnrollDiscoverer(ILogger log, IAnalyticsContainer analytics)
    {
        _log = log;
        _analytics = analytics;
        _symbolReaders = new SymbolReaderCache(log);
    }

    public DiscoveryResult Discover(IBindingRegistryFactory bindingRegistryFactory,
        AssemblyLoadContext assemblyLoadContext,
        Assembly testAssembly,
        Option<FileDetails> configFile)
    {
        var typeNames = ImmutableSortedDictionary.CreateBuilder<string, string>();
        var sourcePaths = ImmutableSortedDictionary.CreateBuilder<string, string>();

        var bindingRegistryAdapter = bindingRegistryFactory
            .GetBindingRegistry(assemblyLoadContext, testAssembly, configFile);

        var stepDefinitions = bindingRegistryAdapter
            .GetStepDefinitions()
            .Select(sdb => CreateStepDefinition(sdb,
                sdb2 => GetParamTypes(sdb2.ParamTypes, parameterTypeName => GetKey(typeNames, parameterTypeName)),
                sourcePath => GetKey(sourcePaths, sourcePath),
                assemblyLoadContext,
                testAssembly)
            )
            .OrderBy(sd => sd.SourceLocation)
            .ToImmutableArray();

        var hooks = bindingRegistryAdapter.
            GetHooks()
            .Select(sdb => CreateHook(sdb,
                sourcePath => GetKey(sourcePaths, sourcePath),
                assemblyLoadContext,
                testAssembly)
            )
            .OrderBy(sd => sd.SourceLocation)
            .ToImmutableArray();


        _analytics.AddAnalyticsProperty("TypeNames", typeNames.Count.ToString());
        _analytics.AddAnalyticsProperty("SourcePaths", sourcePaths.Count.ToString());
        _analytics.AddAnalyticsProperty("StepDefinitions", stepDefinitions.Length.ToString());

        return new DiscoveryResult(
            stepDefinitions,
            hooks,
            sourcePaths.ToImmutable(),
            typeNames.ToImmutable()
        );
    }

    private StepDefinition CreateStepDefinition(StepDefinitionBindingAdapter sdb,
        Func<StepDefinitionBindingAdapter, string?> getParameterTypes, Func<string, string> getSourcePathId,
        AssemblyLoadContext assemblyLoadContext, Assembly testAssembly)
    {
        var sourceLocation = GetSourceLocation(sdb.Method, getSourcePathId, assemblyLoadContext, testAssembly);
        var stepDefinition = new StepDefinition
        (
            sdb.StepDefinitionType,
            sdb.Regex,
            sdb.Method.ToString()!,
            getParameterTypes(sdb),
            GetScope(sdb),
            GetSourceExpression(sdb),
            sdb.Error,
            sourceLocation.Reduce((string) null!)
        );

        return stepDefinition;
    }

    private Hook CreateHook(HookBindingAdapter sdb,
        Func<string, string> getSourcePathId,
        AssemblyLoadContext assemblyLoadContext, Assembly testAssembly)
    {
        var sourceLocation = GetSourceLocation(sdb.Method, getSourcePathId, assemblyLoadContext, testAssembly);
        var stepDefinition = new Hook
        {
            Type = sdb.HookType,
            HookOrder = sdb.HookOrder,
            Method = sdb.Method.ToString(),
            Scope = GetScope(sdb), 
            SourceLocation = sourceLocation.Reduce((string)null!),
            Error = sdb.Error
        };

        return stepDefinition;
    }

    private string GetKey(ImmutableSortedDictionary<string, string>.Builder dictionary, string value)
    {
        KeyValuePair<string, string> found = dictionary
            .FirstOrDefault(kvp => kvp.Value == value);
        if (found.Key is null)
        {
            found = new KeyValuePair<string, string>(dictionary.Count.ToString(), value);
            dictionary.Add(found);
        }

        return found.Key;
    }

    private string? GetParamTypes(string[] parameterTypeNames, Func<string, string> getKey)
    {
        var paramTypes = string.Join("|",
            parameterTypeNames.Select(parameterTypeName => GetParamType(parameterTypeName, getKey)));
        return paramTypes.Length == 0 ? null : paramTypes;
    }

    private string GetParamType(string parameterTypeName, Func<string, string> getKey)
    {
        if (TypeShortcuts.FromType.TryGetValue(parameterTypeName, out var shortcut))
            return shortcut;

        var key = getKey(parameterTypeName);
        return $"#{key}";
    }

    private static StepScope? GetScope(IScopedBindingAdapter scopedBinding)
    {
        if (!scopedBinding.IsScoped)
            return null;

        return new StepScope
        {
            Tag = scopedBinding.BindingScopeTag,
            FeatureTitle = scopedBinding.BindingScopeFeatureTitle,
            ScenarioTitle = scopedBinding.BindingScopeScenarioTitle,
            Error = scopedBinding.BindingScopeError
        };
    }

    private static string? GetSourceExpression(StepDefinitionBindingAdapter sdb)
        => sdb.Expression ?? GetSpecifiedExpressionFromRegex(sdb);

    private static string? GetSpecifiedExpressionFromRegex(StepDefinitionBindingAdapter sdb) =>
        sdb.Regex?
            .Map(regex => regex.ToString())
            .Map(regexString =>
            {
                if (regexString.StartsWith("^"))
                    regexString = regexString.Substring(1);
                if (regexString.EndsWith("$"))
                    regexString = regexString.Substring(0, regexString.Length - 1);
                return regexString;
            });

    private Option<string> GetSourceLocation(BindingMethodAdapter bindingMethod, Func<string, string> getSourcePathId,
        AssemblyLoadContext assemblyLoadContext, Assembly testAssembly)
    {
        if (!bindingMethod.IsProvided)
            return None.Value;
        return bindingMethod
            .Map(mi => (reader: (mi.DeclaringTypeAssemblyName??testAssembly.FullName!)
                        .Map(assemblyName => assemblyLoadContext.LoadFromAssemblyName(new AssemblyName(assemblyName)))
                        .Map(assembly => _symbolReaders[assembly].Reduce(() => default!)),
                    mi.MetadataToken
                ))
            .Map(x => x.reader.ReadMethodSymbol(x.MetadataToken))
            //.Reduce(ImmutableArray<MethodSymbolSequencePoint>.Empty)
            .Map(sequencePoints => sequencePoints
                .Aggregate(
                    (startSequencePoint: None<MethodSymbolSequencePoint>.Value,
                        endSequencePoint: None<MethodSymbolSequencePoint>.Value),
                    (acc, cur) => acc.startSequencePoint is None<MethodSymbolSequencePoint>
                        ? (cur, cur)
                        : (acc.startSequencePoint, cur)
                )
                .Map(x => x.startSequencePoint is Some<MethodSymbolSequencePoint> some
                    ? (some.Content, ((Some<MethodSymbolSequencePoint>)x.endSequencePoint).Content)
                    : None<(MethodSymbolSequencePoint startSequencePoint, MethodSymbolSequencePoint endSequencePoint)>
                        .Value)
            )
            .Map(border =>
                $"#{getSourcePathId(border.startSequencePoint.SourcePath)}|{border.startSequencePoint.StartLine}|{border.startSequencePoint.StartColumn}|{border.endSequencePoint.EndLine}|{border.endSequencePoint.EndColumn}");
    }
}
