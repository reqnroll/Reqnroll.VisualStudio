#nullable disable
namespace Reqnroll.VisualStudio.ReqnrollConnector.Discovery;

public class SpecFlow21ConfigurationLoader : ReqnrollConfigurationLoader
{
    public SpecFlow21ConfigurationLoader(string configFilePath) : base(configFilePath)
    {
    }

    protected override string ConvertToJsonSpecFlow3Style(string configFileContent)
    {
        var content = JsonConvert.DeserializeObject<JObject>(configFileContent);

        if (!content.TryGetValue("reqnroll", out var reqnrollObject))
            return configFileContent;

        var configObject = new JObject(reqnrollObject.First);

        var modifiedContent = JsonConvert.SerializeObject(configObject, Formatting.Indented);
        return modifiedContent;
    }
}
