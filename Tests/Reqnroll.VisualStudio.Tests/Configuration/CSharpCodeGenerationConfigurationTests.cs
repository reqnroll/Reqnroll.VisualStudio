using Reqnroll.VisualStudio.Configuration;
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
}