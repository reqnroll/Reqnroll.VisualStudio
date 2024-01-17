#nullable disable
namespace Reqnroll.VisualStudio.ReqnrollConnector.Discovery;

public abstract class BaseDiscoverer : RemoteContextObject, IReqnrollDiscoverer, IDiscoveryResultDiscoverer
{
    private readonly Dictionary<string, int> _sourceFiles = new();
    private readonly Dictionary<Assembly, IDeveroomSymbolReader> _symbolReaders = new(2);
    private readonly Dictionary<string, int> _typeNames = new();

    public DiscoveryResult DiscoverInternal(Assembly testAssembly, string testAssemblyPath, string configFilePath)
    {
        try
        {
            var bindingRegistry = GetBindingRegistry(testAssembly, configFilePath);

            var result = new DiscoveryResult();
            var warningCollector = new WarningCollector();

            _sourceFiles.Clear();
            GetOrCreateSymbolReader(testAssembly, warningCollector, testAssemblyPath);

            result.StepDefinitions =
                GetStepDefinitions(bindingRegistry)
                    .Select(sdb => CreateStepDefinition(sdb, warningCollector))
                    .Distinct() //TODO: Reqnroll discoverers bindings from external assemblies twice -- needs fix
                    .ToArray();
            result.SourceFiles = _sourceFiles.ToDictionary(sf => sf.Value.ToString(), sf => sf.Key);
            result.TypeNames = _typeNames.ToDictionary(sf => sf.Value.ToString(), sf => sf.Key);
            result.Warnings = warningCollector.Warnings;
            result.ReqnrollVersion = typeof(IStepDefinitionBinding).Assembly.Location;
            return result;
        }
        catch (Exception ex) when (GetRegexError(ex) != null)
        {
            var regexError = GetRegexError(ex);
            return new DiscoveryResult
            {
                ReqnrollVersion = typeof(IStepDefinitionBinding).Assembly.Location,
                StepDefinitions = new[]
                {
                    new StepDefinition
                    {
                        Method = "Unknown method",
                        Error = $"Step definition regular expression error: {regexError.Message}"
                    }
                }
            };
        }
        catch (Exception)
        {
            Console.Error.WriteLine($"Exception in:{GetType().FullName}");
            throw;
        }
    }

    public string Discover(Assembly testAssembly, string testAssemblyPath, string configFilePath)
    {
        var result = DiscoverInternal(testAssembly, testAssemblyPath, configFilePath);

        var resultJson = JsonSerialization.SerializeObject(result);
        return resultJson;
    }

    public void Dispose()
    {
        foreach (var symbolReader in _symbolReaders.Values) symbolReader.Dispose();
        _symbolReaders.Clear();
    }

    private Exception GetRegexError(Exception exception)
    {
        if (exception is TargetInvocationException && exception.InnerException != null)
            return GetRegexError(exception.InnerException);

        if (exception.StackTrace?.Contains("System.Text.RegularExpressions") ?? false)
            return exception;
        if (exception.InnerException == null)
            return null;
        return GetRegexError(exception.InnerException);
    }

    private StepDefinition CreateStepDefinition(IStepDefinitionBinding sdb, WarningCollector warningCollector)
    {
        var stepDefinition = new StepDefinition
        {
            Type = sdb.StepDefinitionType.ToString(),
            Regex = sdb.Regex?.ToString(),
            Method = sdb.Method.ToString(),
            ParamTypes = GetParamTypes(sdb.Method),
            Scope = GetScope(sdb),
            SourceLocation = GetSourceLocation(sdb.Method, warningCollector),
            Expression = GetSourceExpression(sdb),
            Error = GetError(sdb)
        };

        return stepDefinition;
    }

    private string GetError(IStepDefinitionBinding sdb)
    {
        const string propertyName = "ErrorMessage";
        return sdb.ReflectionHasProperty(propertyName) ? sdb.ReflectionGetProperty<string>(propertyName) : null;
    }

    private string GetSourceExpression(IStepDefinitionBinding sdb)
    {
        const string propertyName = "SourceExpression";
        return sdb.ReflectionHasProperty(propertyName) ? sdb.ReflectionGetProperty<string>(propertyName) : null;
    }

    protected virtual IDeveroomSymbolReader CreateSymbolReader(string assemblyFilePath,
        WarningCollector warningCollector)
    {
        try
        {
            return new DnLibDeveroomSymbolReader(assemblyFilePath);
        }
        catch (Exception ex)
        {
            warningCollector.AddWarning($"Unable to create symbol reader for '{assemblyFilePath}'.", ex);
            return new NullDeveroomSymbolReader();
        }
    }

    private IDeveroomSymbolReader GetOrCreateSymbolReader(Assembly assembly, WarningCollector warningCollector,
        string assemblyFilePath = null)
    {
        if (!_symbolReaders.TryGetValue(assembly, out var symbolReader))
        {
            assemblyFilePath = assemblyFilePath ?? new Uri(assembly.Location).LocalPath;
            symbolReader = CreateSymbolReader(assemblyFilePath, warningCollector);
            _symbolReaders.Add(assembly, symbolReader);
        }

        return symbolReader;
    }

    private string GetSourceLocation(IBindingMethod bindingMethod, WarningCollector warningCollector)
    {
        if (bindingMethod is not RuntimeBindingMethod runtimeBindingMethod ||
            runtimeBindingMethod.MethodInfo.DeclaringType == null) return null;

        try
        {
            var symbolReader = GetOrCreateSymbolReader(runtimeBindingMethod.MethodInfo.DeclaringType.Assembly,
                warningCollector);
            var methodSymbol = symbolReader.ReadMethodSymbol(runtimeBindingMethod.MethodInfo.MetadataToken);
            var startSequencePoint = methodSymbol?.SequencePoints?.FirstOrDefault(sp => !sp.IsHidden);
            if (startSequencePoint == null)
                return null;
            var sourceKey = GetKey(_sourceFiles, startSequencePoint.SourcePath);
            var sourceLocation = $"#{sourceKey}|{startSequencePoint.StartLine}|{startSequencePoint.StartColumn}";
            var endSequencePoint = methodSymbol.SequencePoints.LastOrDefault(sp => !sp.IsHidden);
            if (endSequencePoint != null)
                sourceLocation = sourceLocation + $"|{endSequencePoint.EndLine}|{endSequencePoint.EndColumn}";

            return sourceLocation;
        }
        catch (Exception ex)
        {
            warningCollector.AddWarning("GetSourceLocation", ex);
            return null;
        }
    }

    private int GetKey(Dictionary<string, int> dictionary, string value)
    {
        if (!dictionary.TryGetValue(value, out var key))
        {
            key = dictionary.Count;
            dictionary.Add(value, key);
        }

        return key;
    }

    private string GetParamTypes(IBindingMethod bindingMethod)
    {
        var paramTypes = string.Join("|", bindingMethod.Parameters.Select(GetParamType));
        return paramTypes.Length == 0 ? null : paramTypes;
    }

    private string GetParamType(IBindingParameter bindingParameter)
    {
        var typeFullName = bindingParameter.Type.FullName;
        if (TypeShortcuts.FromType.TryGetValue(typeFullName, out var shortcut))
            return shortcut;

        var key = GetKey(_typeNames, typeFullName);
        return $"#{key}";
    }

    protected abstract IBindingRegistry GetBindingRegistry(Assembly testAssembly, string configFilePath);
    protected abstract IEnumerable<IStepDefinitionBinding> GetStepDefinitions(IBindingRegistry bindingRegistry);

    private StepScope GetScope(IStepDefinitionBinding stepDefinitionBinding)
    {
        if (!stepDefinitionBinding.IsScoped)
            return null;

        return new StepScope
        {
            Tag = stepDefinitionBinding.BindingScope.Tag == null
                ? null
                : "@" + stepDefinitionBinding.BindingScope.Tag,
            FeatureTitle = stepDefinitionBinding.BindingScope.FeatureTitle,
            ScenarioTitle = stepDefinitionBinding.BindingScope.ScenarioTitle
        };
    }
}
