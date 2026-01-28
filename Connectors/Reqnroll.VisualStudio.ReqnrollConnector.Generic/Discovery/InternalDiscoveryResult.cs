namespace ReqnrollConnector.Discovery;

public record InternalDiscoveryResult(
    Reqnroll.VisualStudio.ReqnrollConnector.Models.StepDefinition[] StepDefinitions,
    Reqnroll.VisualStudio.ReqnrollConnector.Models.Hook[] Hooks,
    IDictionary<string, string> SourceFiles,
    IDictionary<string, string> TypeNames,
    string[] GenericBindingErrors,
    string[] TypeLoadErrors
);
