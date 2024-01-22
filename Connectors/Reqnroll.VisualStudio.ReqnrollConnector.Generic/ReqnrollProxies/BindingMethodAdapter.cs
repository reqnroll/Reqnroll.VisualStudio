using Reqnroll.Bindings.Provider.Data;

namespace ReqnrollConnector.ReqnrollProxies;

public record BindingMethodAdapter(BindingSourceMethodData? Adaptee)
{
    public bool IsProvided => Adaptee != null;
    public string? DeclaringTypeAssemblyName => Adaptee?.Assembly;
    public string? DeclaringTypeFullName => Adaptee?.Type;
    public string? DeclaringTypeName => DeclaringTypeFullName?.Split('.').Last();
    public string? MethodSignatureWithoutReturnType => Adaptee?.FullName?.Split(new[] { ' ' }, 2).ElementAtOrDefault(1)?.Trim();

    public int MetadataToken => Adaptee?.MetadataToken ?? 0;
    public override string? ToString() => IsProvided ? $"{DeclaringTypeName}.{MethodSignatureWithoutReturnType}" : "???";
}
