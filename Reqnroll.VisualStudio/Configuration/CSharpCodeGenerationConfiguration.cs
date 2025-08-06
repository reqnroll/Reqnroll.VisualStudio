namespace Reqnroll.VisualStudio.Configuration;

public class CSharpCodeGenerationConfiguration
{
    /// <summary>
    /// Specifies the namespace declaration style for generated C# code.
    /// Uses file-scoped namespaces when set to "file_scoped", otherwise uses block-scoped namespaces.
    /// </summary>
    [EditorConfigSetting("csharp_style_namespace_declarations")]
    public string NamespaceDeclarationStyle { get; set; } = "block_scoped";

    /// <summary>
    /// Determines if file-scoped namespaces should be used based on the EditorConfig setting.
    /// </summary>
    public bool UseFileScopedNamespaces => 
        NamespaceDeclarationStyle != null && 
        NamespaceDeclarationStyle.StartsWith("file_scoped", StringComparison.OrdinalIgnoreCase);
}