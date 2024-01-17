namespace ReqnrollConnector.ReqnrollProxies;

public record StepDefinition(
    string Type,
    string? Regex,
    string Method,
    string? ParamTypes,
    StepScope? Scope,
    string? Expression,
    string? Error,
    string SourceLocation
);
