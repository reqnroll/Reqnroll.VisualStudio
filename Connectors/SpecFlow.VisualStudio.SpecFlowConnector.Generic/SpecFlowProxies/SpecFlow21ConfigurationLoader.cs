using System.Text.Json;

namespace SpecFlowConnector.SpecFlowProxies;

public class SpecFlow21ConfigurationLoader : SpecFlowConfigurationLoader
{
    public SpecFlow21ConfigurationLoader(Option<FileDetails> configFile) : base(configFile)
    {
    }

    protected override string ConvertToJsonSpecFlow3Style(string configFileContent)
    {
        var content = JsonDocument.Parse(configFileContent);

        if (!content.RootElement.TryGetProperty("specFlow", out var specFlowObject))
            return configFileContent;

        var configObject = specFlowObject.EnumerateArray().First();

        var modifiedContent = JsonSerializer.Serialize(configObject, new JsonSerializerOptions {WriteIndented = true});
        return modifiedContent;
    }
}
