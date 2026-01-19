using System.Runtime.Versioning;

namespace ReqnrollConnector;

public class ReflectionExecutor
{
    public static ConnectorResult Execute(DiscoveryOptions options,
        Func<AssemblyLoadContext, string, Assembly> testAssemblyFactory, ILogger _log, IAnalyticsContainer analytics)
    {
        _log.Info($"Loading {options.AssemblyFile}");
        var testAssemblyContext = new TestAssemblyLoadContext(options.AssemblyFile, testAssemblyFactory, _log);
        analytics.AddAnalyticsProperty("ImageRuntimeVersion", testAssemblyContext.TestAssembly.ImageRuntimeVersion);

        var targetFrameworkAttributes = testAssemblyContext.TestAssembly.CustomAttributes
            .Where(a => a.AttributeType == typeof(TargetFrameworkAttribute))
            .ToList();
        
        if (targetFrameworkAttributes.Count > 0)
        {
            var tf = targetFrameworkAttributes[0];
            analytics.AddAnalyticsProperty("TargetFramework", tf.ConstructorArguments.First().ToString().Trim('\"'));
        }

        var reflectionType = TypeFromAssemblyLoadContext(typeof(ReflectionExecutor), testAssemblyContext);
        if (reflectionType == null)
        {
            return CreateErrorResult(analytics, $"Could not create instance from: {typeof(ReflectionExecutor)}");
        }

        var reflectedInstance = CreateInstance(reflectionType);
        if (reflectedInstance == null)
        {
            return CreateErrorResult(analytics, $"Could not create instance from: {typeof(ReflectionExecutor)}");
        }
        
        var resultJson = reflectedInstance.ReflectionCallMethod<string>(
            nameof(Execute),
            JsonSerialization.SerializeObject(options, _log), testAssemblyContext.TestAssembly, testAssemblyContext,
            analytics);

        var deserializedResult = JsonSerialization.DeserializeObjectRunnerResult(resultJson, _log);
        if (deserializedResult == null)
        {
            return CreateErrorResult(analytics, $"Could not deserialize result from: {typeof(ReflectionExecutor)}");
        }

        var (log, analyticsProperties, discoveryResult, errorMessage) = deserializedResult;
        _log.Info(log);
        
        if (discoveryResult != null)
        {
            return new ConnectorResult(
                discoveryResult.StepDefinitions,
                discoveryResult.Hooks,
                discoveryResult.SourceFiles,
                discoveryResult.TypeNames,
                analyticsProperties,
                errorMessage);
        }
        
        return new ConnectorResult(ImmutableArray<StepDefinition>.Empty,
            ImmutableArray<Reqnroll.VisualStudio.ReqnrollConnector.Models.Hook>.Empty,
            ImmutableSortedDictionary<string, string>.Empty,
            ImmutableSortedDictionary<string, string>.Empty,
            analytics.ToImmutable(),
            errorMessage != null ? $"{errorMessage}{Environment.NewLine}{log}" : log);
    }

    private static ConnectorResult CreateErrorResult(IAnalyticsContainer analytics, string errorMessage)
    {
        return new ConnectorResult(ImmutableArray<StepDefinition>.Empty,
            ImmutableArray<Reqnroll.VisualStudio.ReqnrollConnector.Models.Hook>.Empty,
            ImmutableSortedDictionary<string, string>.Empty,
            ImmutableSortedDictionary<string, string>.Empty,
            analytics.ToImmutable(),
            errorMessage);
    }

    public static Type TypeFromAssemblyLoadContext(Type reType, TestAssemblyLoadContext testAssemblyContext) 
        => testAssemblyContext.LoadFromAssemblyPath(reType.Assembly.Location).GetType(reType.FullName!)!;

    private static object? CreateInstance(Type reflectedType) =>
        Activator.CreateInstance(reflectedType);

    public string Execute(string optionsJson, Assembly testAssembly,
        AssemblyLoadContext assemblyLoadContext, IDictionary<string, string> analyticsProperties)
    {
        var analytics = new AnalyticsContainer(analyticsProperties);
        var log = new StringWriterLogger();
        DiscoveryOptions discoveryOptions;
        try
        {
            discoveryOptions = JsonSerialization.DeserializeDiscoveryOptions(optionsJson, log);
        }
        catch (Exception ex)
        {
            return JsonSerialization.SerializeRunnerResult(new RunnerResult(log.ToString(), analytics, null,
                $"Unable to deserialize discovery options:  {ex.Message} ({optionsJson})"));
        }

        try
        {
            var commandFactory = new CommandFactory(log, discoveryOptions, testAssembly, analytics);
            var command = commandFactory.CreateCommand();
            var discoveryResult = command.Execute(assemblyLoadContext);
            var runnerResult = new RunnerResult(log.ToString(), analytics.ToImmutable(), discoveryResult, null);
            return JsonSerialization.SerializeRunnerResult(runnerResult, log);
        }
        catch (Exception ex)
        {
            var errorMessage = ex.ToString();
            log.Error(errorMessage);
            var errorResult = new RunnerResult(
                log.ToString(),
                analytics,
                null,
                errorMessage);
            return JsonSerialization.SerializeRunnerResult(errorResult, log);
        }
    }
    
    public record RunnerResult(string Log, ImmutableSortedDictionary<string, string> AnalyticsProperties, DiscoveryResult? DiscoveryResult, string? errorMessage);
}