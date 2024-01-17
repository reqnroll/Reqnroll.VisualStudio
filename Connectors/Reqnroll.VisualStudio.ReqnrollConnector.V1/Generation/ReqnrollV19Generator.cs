namespace Reqnroll.VisualStudio.ReqnrollConnector.Generation;

public class ReqnrollV19Generator : ReqnrollV22Generator
{
    protected override ReqnrollConfigurationHolder CreateConfigHolder(string configFilePath)
    {
        var configFileContent = File.ReadAllText(configFilePath);
        return GetXmlConfigurationHolder(configFileContent);
    }
}
