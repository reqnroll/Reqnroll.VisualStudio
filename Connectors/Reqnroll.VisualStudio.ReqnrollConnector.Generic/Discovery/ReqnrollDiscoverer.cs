using Reqnroll.VisualStudio.ReqnrollConnector.Models;
using ReqnrollConnector.Utils;
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
        FileDetails? configFile)
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
        var sourceLocationStr = GetSourceLocation(sdb.Method, getSourcePathId, assemblyLoadContext, testAssembly);

        var stepDefinition = new StepDefinition
        (
            sdb.StepDefinitionType,
            sdb.Regex,
            sdb.Method.ToString()!,
            getParameterTypes(sdb),
            GetScope(sdb),
            GetSourceExpression(sdb),
            sdb.Error,
            sourceLocationStr!
        );

        return stepDefinition;
    }

    private Hook CreateHook(HookBindingAdapter sdb,
        Func<string, string> getSourcePathId,
        AssemblyLoadContext assemblyLoadContext, Assembly testAssembly)
    {
        var sourceLocationStr = GetSourceLocation(sdb.Method, getSourcePathId, assemblyLoadContext, testAssembly);

        var stepDefinition = new Hook
        {
            Type = sdb.HookType,
            HookOrder = sdb.HookOrder,
            Method = sdb.Method.ToString(),
            Scope = GetScope(sdb), 
            SourceLocation = sourceLocationStr!,
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

    private static string? GetSpecifiedExpressionFromRegex(StepDefinitionBindingAdapter sdb)
    {
        if (sdb.Regex == null)
            return null;

        string regexString = sdb.Regex.ToString();
        if (regexString.StartsWith("^"))
            regexString = regexString.Substring(1);
        if (regexString.EndsWith("$"))
            regexString = regexString.Substring(0, regexString.Length - 1);
        return regexString;
    }

    private string? GetSourceLocation(BindingMethodAdapter bindingMethod, Func<string, string> getSourcePathId,
        AssemblyLoadContext assemblyLoadContext, Assembly testAssembly)
    {
        if (!bindingMethod.IsProvided)
            return null;

        var methodInfo = bindingMethod;
        
        var assemblyNameStr = methodInfo.DeclaringTypeAssemblyName ?? testAssembly.FullName!;
        var assemblyNameObj = new AssemblyName(assemblyNameStr);
        var assembly = assemblyLoadContext.LoadFromAssemblyName(assemblyNameObj);
        var reader = _symbolReaders[assembly];
        
        if (reader == null)
            return null;

        var sequencePoints = reader.ReadMethodSymbol(methodInfo.MetadataToken);
        
        // Find start and end sequence points
        var (startSequencePoint, endSequencePoint) = sequencePoints.Aggregate(
            (startSequencePoint: (MethodSymbolSequencePoint?)null,
             endSequencePoint: (MethodSymbolSequencePoint?)null),
            (acc, cur) =>
            {
                if (acc.startSequencePoint == null)
                    return (cur, cur);
                else
                    return (acc.startSequencePoint, cur);
            }
        );

        // Extract the points
        if (startSequencePoint != null && endSequencePoint != null)
        {
            var locationStr = $"#{getSourcePathId(startSequencePoint.SourcePath)}|{startSequencePoint.StartLine}|{startSequencePoint.StartColumn}|{endSequencePoint.EndLine}|{endSequencePoint.EndColumn}";
            return locationStr;
        }

        return null;
    }
}
