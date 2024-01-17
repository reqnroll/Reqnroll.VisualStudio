namespace SpecFlow.VisualStudio.SpecFlowConnector.Generation;

public class SpecFlowV19Generator : SpecFlowV22Generator
{
    protected override SpecFlowConfigurationHolder CreateConfigHolder(string configFilePath)
    {
        var configFileContent = File.ReadAllText(configFilePath);
        return GetXmlConfigurationHolder(configFileContent);
    }
}
