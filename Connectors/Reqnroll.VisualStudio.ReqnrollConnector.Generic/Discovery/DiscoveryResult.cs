namespace ReqnrollConnector.Discovery;

public record DiscoveryResult(
    ImmutableArray<StepDefinition> StepDefinitions,
    ImmutableArray<Reqnroll.VisualStudio.ReqnrollConnector.Models.Hook> Hooks,
    ImmutableSortedDictionary<string, string> SourceFiles,
    ImmutableSortedDictionary<string, string> TypeNames
);
