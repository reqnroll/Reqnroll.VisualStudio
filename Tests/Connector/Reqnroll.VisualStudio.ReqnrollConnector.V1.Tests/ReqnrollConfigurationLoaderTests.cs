#nullable disable
using FluentAssertions;
using Reqnroll.VisualStudio.ReqnrollConnector.Discovery;
using Reqnroll.Configuration;
using Xunit;

namespace Reqnroll.VisualStudio.ReqnrollConnector.V1.Tests;

public class ReqnrollConfigurationLoaderTests
{
    private readonly ReqnrollConfiguration _defaultConfig = ConfigurationLoader.GetDefault();

    [Fact]
    public void Loads_config_from_AppConfig()
    {
        var configFileContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <configSections>
    <section name=""reqnroll"" type=""Reqnroll.Configuration.ConfigurationSectionHandler, Reqnroll"" />
  </configSections>
  <specFlow>
    <language feature=""de-AT"" />
  </specFlow>
</configuration>";
        var filePath = Path.GetTempPath() + ".config";
        File.WriteAllText(filePath, configFileContent);
        var sut = new ReqnrollConfigurationLoader(filePath);

        var config = sut.Load(_defaultConfig);

        config.Should().NotBeNull();
        config.FeatureLanguage.ToString().Should().Be("de-AT");
    }

    [Fact]
    public void Loads_config_from_JSON_SpecFlow2_Style()
    {
        var configFileContent = @"
{
    ""reqnroll"": {
        ""language"": {
            ""feature"": ""de-AT""
        }
    }
}";
        var filePath = Path.GetTempPath() + ".json";
        File.WriteAllText(filePath, configFileContent);
        var sut = new SpecFlow21ConfigurationLoader(filePath);

        var config = sut.Load(_defaultConfig);

        config.Should().NotBeNull();
        config.FeatureLanguage.ToString().Should().Be("de-AT");
    }

    [Fact]
    public void Loads_config_from_JSON_SpecFlow3_Style()
    {
        var configFileContent = @"
{
    ""language"": {
        ""feature"": ""de-AT""
    }
}";
        var filePath = Path.GetTempPath() + ".json";
        File.WriteAllText(filePath, configFileContent);
        var sut = new ReqnrollConfigurationLoader(filePath);

        var config = sut.Load(_defaultConfig);

        config.Should().NotBeNull();
        config.FeatureLanguage.ToString().Should().Be("de-AT");
    }

    [Fact]
    public void Loads_input_config_for_null_config_file()
    {
        var sut = new ReqnrollConfigurationLoader(null);

        var config = sut.Load(_defaultConfig);

        config.Should().BeSameAs(_defaultConfig);
    }

    [Fact]
    public void Loads_default_config_for_null_config_file_and_null_input()
    {
        var sut = new ReqnrollConfigurationLoader(null);

        var config = sut.Load(null);

        config.Should().NotBeNull();
        config.FeatureLanguage.ToString().Should().Be("en-US");
    }


    [Fact]
    public void Loads_input_config_for_AppConfig_file_without_config_node()
    {
        var configFileContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
</configuration>";
        var filePath = Path.GetTempPath() + ".config";
        File.WriteAllText(filePath, configFileContent);
        var sut = new ReqnrollConfigurationLoader(filePath);

        var config = sut.Load(_defaultConfig);

        config.Should().BeSameAs(_defaultConfig);
    }
}
