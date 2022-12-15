using System.Runtime.Versioning;
using TechTalk.SpecFlow.CommonModels;

namespace SpecFlowConnector;

public class ReflectionExecutor
{
    public static ConnectorResult Execute(DiscoveryOptions options,
        Func<AssemblyLoadContext, string, Assembly> testAssemblyFactory, ILogger _log, IAnalyticsContainer analytics)
    {
        _log.Info($"Loading {options.AssemblyFile}");
        var testAssemblyContext = new TestAssemblyLoadContext(options.AssemblyFile, testAssemblyFactory, _log);
        analytics.AddAnalyticsProperty("ImageRuntimeVersion", testAssemblyContext.TestAssembly.ImageRuntimeVersion);

        testAssemblyContext.TestAssembly.CustomAttributes
            .Where(a => a.AttributeType == typeof(TargetFrameworkAttribute))
            .FirstOrNone()
            .Tie(tf => analytics
                .AddAnalyticsProperty("TargetFramework", tf.ConstructorArguments.First().ToString().Trim('\"'))
            );

        return TypeFromAssemblyLoadContext(typeof(ReflectionExecutor), testAssemblyContext)
            .Map(CreateInstance)
            .Map(instance => instance.ReflectionCallMethod<string>(
                    nameof(Execute),
                    JsonSerialization.SerializeObject(options, _log), testAssemblyContext.TestAssembly, testAssemblyContext,
                    analytics)
                .Map(s => JsonSerialization.DeserializeObjectRunnerResult(s, _log)
                    .Map(result =>
                    {
                        var (log, analyticsProperties, discoveryResult, errorMessage) = result;
                        _log.Info(log);
                        if (discoveryResult != null)
                        {
                            return new ConnectorResult(
                                discoveryResult.StepDefinitions,
                                discoveryResult.SourceFiles,
                                discoveryResult.TypeNames,
                                analyticsProperties,
                                errorMessage);
                        }
                        return new ConnectorResult(ImmutableArray<StepDefinition>.Empty,
                            ImmutableSortedDictionary<string, string>.Empty,
                            ImmutableSortedDictionary<string, string>.Empty,
                            analytics.ToImmutable(),
                            errorMessage != null ? $"{errorMessage}{Environment.NewLine}{log}" : log);
                    })))
            .Reduce(new ConnectorResult(ImmutableArray<StepDefinition>.Empty,
                ImmutableSortedDictionary<string, string>.Empty,
                ImmutableSortedDictionary<string, string>.Empty,
                analytics.ToImmutable(),
                $"Could not create instance from: {typeof(ReflectionExecutor)}")
            );
    }

    public static Type TypeFromAssemblyLoadContext(Type reType, TestAssemblyLoadContext testAssemblyContext) 
        => testAssemblyContext.LoadFromAssemblyPath(reType.Assembly.Location).GetType(reType.FullName!)!;

    private static Option<object> CreateInstance(Type reflectedType) =>
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

        return discoveryOptions
            .Map(options => EitherAdapters.Try(
                    () => new CommandFactory(log, options, testAssembly, analytics)
                        .Map(factory => factory.CreateCommand())
                        .Map(cmd => cmd.Execute(assemblyLoadContext))
                )
                .Map(dr => new RunnerResult(log.ToString(), analytics.ToImmutable(), dr, null))
                .Reduce(ex =>
                {
                    var errorMessage = ex.ToString();
                    log.Error(errorMessage);
                    return new RunnerResult(
                        log.ToString(),
                        analytics,
                        null,
                        errorMessage);
                })
                
            )
            .Map(result => JsonSerialization.SerializeRunnerResult(result, log));
    }
    public record RunnerResult(string Log, ImmutableSortedDictionary<string, string> AnalyticsProperties, DiscoveryResult? DiscoveryResult, string? errorMessage);
}