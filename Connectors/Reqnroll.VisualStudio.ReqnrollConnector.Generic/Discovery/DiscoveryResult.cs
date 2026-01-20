namespace ReqnrollConnector.Discovery;

public record DiscoveryResult(
    ImmutableArray<StepDefinition> StepDefinitions,
    ImmutableArray<Reqnroll.VisualStudio.ReqnrollConnector.Models.Hook> Hooks,
    ImmutableSortedDictionary<string, string> SourceFiles,
    ImmutableSortedDictionary<string, string> TypeNames
);

public record DiscoveryResult2(
    Reqnroll.VisualStudio.ReqnrollConnector.Models.StepDefinition[] StepDefinitions,
    Reqnroll.VisualStudio.ReqnrollConnector.Models.Hook[] Hooks,
    IDictionary<string, string> SourceFiles,
    IDictionary<string, string> TypeNames
);
