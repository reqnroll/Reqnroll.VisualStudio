namespace ReqnrollConnector.Discovery;

public record DiscoveryResult(
    ImmutableArray<StepDefinition> StepDefinitions,
    ImmutableSortedDictionary<string, string> SourceFiles,
    ImmutableSortedDictionary<string, string> TypeNames
);
