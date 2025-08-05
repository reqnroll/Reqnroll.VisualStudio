#nullable disable

using FluentAssertions;
using Reqnroll.VisualStudio.Configuration;
using Xunit;

namespace Reqnroll.VisualStudio.Tests.Configuration;

public class ReqnrollConfigDeserializerTests
{
    [Fact]
    public void Should_set_feature_language_from_reqnroll_json()
    {
        // Arrange
        var deserializer = new ReqnrollConfigDeserializer();
        var config = new DeveroomConfiguration();
        var json = """
        {
          "language": {
            "feature": "hu-HU"
          }
        }
        """;

        // Act
        deserializer.Populate(json, config);

        // Assert
        config.DefaultFeatureLanguage.Should().Be("hu-HU");
        config.ConfiguredBindingCulture.Should().BeNull();
        config.BindingCulture.Should().Be("hu-HU"); // Falls back to feature language
    }

    [Fact]
    public void Should_set_binding_culture_from_reqnroll_json()
    {
        // Arrange
        var deserializer = new ReqnrollConfigDeserializer();
        var config = new DeveroomConfiguration();
        var json = """
        {
          "language": {
            "binding": "fr-FR"
          }
        }
        """;

        // Act
        deserializer.Populate(json, config);

        // Assert
        config.DefaultFeatureLanguage.Should().Be("en-US"); // Should remain default
        config.ConfiguredBindingCulture.Should().Be("fr-FR");
        config.BindingCulture.Should().Be("fr-FR");
    }

    [Fact]
    public void Should_set_both_feature_and_binding_languages_from_reqnroll_json()
    {
        // Arrange
        var deserializer = new ReqnrollConfigDeserializer();
        var config = new DeveroomConfiguration();
        var json = """
        {
          "language": {
            "feature": "en-US",
            "binding": "fr-FR"
          }
        }
        """;

        // Act
        deserializer.Populate(json, config);

        // Assert
        config.DefaultFeatureLanguage.Should().Be("en-US");
        config.ConfiguredBindingCulture.Should().Be("fr-FR");
        config.BindingCulture.Should().Be("fr-FR");
    }

    [Fact]
    public void Should_keep_defaults_when_no_language_configuration()
    {
        // Arrange
        var deserializer = new ReqnrollConfigDeserializer();
        var config = new DeveroomConfiguration();
        var json = """
        {
          "trace": {
            "stepDefinitionSkeletonStyle": "CucumberExpressionAttribute"
          }
        }
        """;

        // Act
        deserializer.Populate(json, config);

        // Assert
        config.DefaultFeatureLanguage.Should().Be("en-US"); // Default
        config.ConfiguredBindingCulture.Should().BeNull(); // Default
        config.BindingCulture.Should().Be("en-US"); // Falls back to feature language
    }

    [Fact]
    public void Should_support_legacy_specflow_binding_culture_format()
    {
        // Arrange
        var deserializer = new ReqnrollConfigDeserializer();
        var config = new DeveroomConfiguration();
        var json = """
        {
          "bindingCulture": {
            "name": "de-DE"
          }
        }
        """;

        // Act
        deserializer.Populate(json, config);

        // Assert
        config.DefaultFeatureLanguage.Should().Be("en-US"); // Default
        config.ConfiguredBindingCulture.Should().Be("de-DE");
        config.BindingCulture.Should().Be("de-DE");
    }

    [Fact]
    public void Should_prioritize_language_binding_over_legacy_bindingCulture()
    {
        // Arrange
        var deserializer = new ReqnrollConfigDeserializer();
        var config = new DeveroomConfiguration();
        var json = """
        {
          "language": {
            "binding": "fr-FR"
          },
          "bindingCulture": {
            "name": "de-DE"
          }
        }
        """;

        // Act
        deserializer.Populate(json, config);

        // Assert
        config.DefaultFeatureLanguage.Should().Be("en-US"); // Default
        config.ConfiguredBindingCulture.Should().Be("fr-FR"); // language.binding takes priority
        config.BindingCulture.Should().Be("fr-FR");
    }
}