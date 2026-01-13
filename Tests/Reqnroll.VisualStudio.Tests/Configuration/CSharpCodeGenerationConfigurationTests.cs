using Reqnroll.VisualStudio.Configuration;
using Reqnroll.VisualStudio.Editor.Services.EditorConfig;
using Xunit;

namespace Reqnroll.VisualStudio.Tests.Configuration;

public class CSharpCodeGenerationConfigurationTests
{
    [Fact]
    public void UseFileScopedNamespaces_WhenFileScopedSet_ReturnsTrue()
    {
        // Arrange
        var config = new CSharpCodeGenerationConfiguration
        {
            NamespaceDeclarationStyle = "file_scoped"
        };

        // Act & Assert
        Assert.True(config.UseFileScopedNamespaces);
    }

    [Fact]
    public void UseFileScopedNamespaces_WhenFileScopedWithSeveritySet_ReturnsTrue()
    {
        // Arrange
        var config = new CSharpCodeGenerationConfiguration
        {
            NamespaceDeclarationStyle = "file_scoped:warning"
        };

        // Act & Assert
        Assert.True(config.UseFileScopedNamespaces);
    }

    [Fact]
    public void UseFileScopedNamespaces_WhenBlockScopedSet_ReturnsFalse()
    {
        // Arrange
        var config = new CSharpCodeGenerationConfiguration
        {
            NamespaceDeclarationStyle = "block_scoped"
        };

        // Act & Assert
        Assert.False(config.UseFileScopedNamespaces);
    }

    [Fact]
    public void UseFileScopedNamespaces_WhenDefaultValue_ReturnsFalse()
    {
        // Arrange
        var config = new CSharpCodeGenerationConfiguration();

        // Act & Assert
        Assert.False(config.UseFileScopedNamespaces);
    }

    [Fact]
    public void UseFileScopedNamespaces_WhenNullValue_ReturnsFalse()
    {
        // Arrange
        var config = new CSharpCodeGenerationConfiguration
        {
            NamespaceDeclarationStyle = null
        };

        // Act & Assert
        Assert.False(config.UseFileScopedNamespaces);
    }

    [Fact]
    public void UseFileScopedNamespaces_WhenUnknownValue_ReturnsFalse()
    {
        // Arrange
        var config = new CSharpCodeGenerationConfiguration
        {
            NamespaceDeclarationStyle = "unknown_style"
        };

        // Act & Assert
        Assert.False(config.UseFileScopedNamespaces);
    }

    [Fact]
    public void UpdateFromEditorConfig_WhenFileScopedValue_SetsCorrectValue()
    {
        // Arrange
        var config = new CSharpCodeGenerationConfiguration();
        var editorConfigOptions = new TestEditorConfigOptions("file_scoped:silent");

        // Act
        editorConfigOptions.UpdateFromEditorConfig(config);

        // Assert
        Assert.Equal("file_scoped:silent", config.NamespaceDeclarationStyle);
        Assert.True(config.UseFileScopedNamespaces);
    }

    [Fact]
    public void UpdateFromEditorConfig_WhenBlockScopedValue_SetsCorrectValue()
    {
        // Arrange
        var config = new CSharpCodeGenerationConfiguration();
        var editorConfigOptions = new TestEditorConfigOptions("block_scoped");

        // Act
        editorConfigOptions.UpdateFromEditorConfig(config);

        // Assert
        Assert.Equal("block_scoped", config.NamespaceDeclarationStyle);
        Assert.False(config.UseFileScopedNamespaces);
    }

    [Fact]
    public void UpdateFromEditorConfig_WhenNoValue_KeepsDefault()
    {
        // Arrange
        var config = new CSharpCodeGenerationConfiguration();
        var editorConfigOptions = new TestEditorConfigOptions(null);

        // Act
        editorConfigOptions.UpdateFromEditorConfig(config);

        // Assert
        Assert.Equal("block_scoped", config.NamespaceDeclarationStyle); // Should keep default
        Assert.False(config.UseFileScopedNamespaces);
    }
}

// Test EditorConfig options provider that simulates reading specific values
public class TestEditorConfigOptions : IEditorConfigOptions
{
    private readonly string _namespaceStyle;
    
    public TestEditorConfigOptions(string namespaceStyle)
    {
        _namespaceStyle = namespaceStyle;
    }

    public TResult GetOption<TResult>(string editorConfigKey, TResult defaultValue)
    {
        if (editorConfigKey == "csharp_style_namespace_declarations" && _namespaceStyle != null)
        {
            return (TResult)(object)_namespaceStyle;
        }
        
        return defaultValue;
    }

    public bool GetBoolOption(string editorConfigKey, bool defaultValue)
    {
        return defaultValue;
    }
}