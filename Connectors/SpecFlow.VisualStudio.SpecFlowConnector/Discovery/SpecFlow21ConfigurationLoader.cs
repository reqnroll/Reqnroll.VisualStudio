#nullable disable
namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery;

public class SpecFlow21ConfigurationLoader : SpecFlowConfigurationLoader
{
    public SpecFlow21ConfigurationLoader(string configFilePath) : base(configFilePath)
    {
    }

    protected override string ConvertToJsonSpecFlow3Style(string configFileContent)
    {
        var content = JsonConvert.DeserializeObject<JObject>(configFileContent);

        if (!content.TryGetValue("specFlow", out var specFlowObject))
            return configFileContent;

        var configObject = new JObject(specFlowObject.First);

        var modifiedContent = JsonConvert.SerializeObject(configObject, Formatting.Indented);
        return modifiedContent;
    }
}
