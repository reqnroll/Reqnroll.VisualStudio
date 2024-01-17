using System.Text.Json;

namespace ReqnrollConnector.ReqnrollProxies;

public class SpecFlow21ConfigurationLoader : ReqnrollConfigurationLoader
{
    public SpecFlow21ConfigurationLoader(Option<FileDetails> configFile) : base(configFile)
    {
    }

    protected override string ConvertToJsonSpecFlow3Style(string configFileContent)
    {
        var content = JsonDocument.Parse(configFileContent);

        if (!content.RootElement.TryGetProperty("reqnroll", out var reqnrollObject))
            return configFileContent;

        var configObject = reqnrollObject.EnumerateArray().First();

        var modifiedContent = JsonSerializer.Serialize(configObject, new JsonSerializerOptions {WriteIndented = true});
        return modifiedContent;
    }
}
