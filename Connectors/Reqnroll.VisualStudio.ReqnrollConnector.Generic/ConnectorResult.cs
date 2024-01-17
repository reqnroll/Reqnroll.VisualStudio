namespace ReqnrollConnector.Discovery;

public record ConnectorResult(
    ImmutableArray<StepDefinition> StepDefinitions,
    ImmutableSortedDictionary<string, string> SourceFiles,
    ImmutableSortedDictionary<string, string> TypeNames,
    ImmutableSortedDictionary<string, string> AnalyticsProperties,
    string? ErrorMessage
);
