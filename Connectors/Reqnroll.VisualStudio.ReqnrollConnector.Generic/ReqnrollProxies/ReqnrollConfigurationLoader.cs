using Reqnroll.Configuration;
using Reqnroll.Configuration.AppConfig;
using Reqnroll.Configuration.JsonConfig;
using Reqnroll.Tracing;

namespace ReqnrollConnector.ReqnrollProxies;

public class ReqnrollConfigurationLoader : IConfigurationLoader
{
    private readonly Option<FileDetails> _configFile;

    public ReqnrollConfigurationLoader(Option<FileDetails> configFile)
    {
        _configFile = configFile;
    }

    public ReqnrollConfiguration Load(ReqnrollConfiguration? reqnrollConfiguration)
    {
        return _configFile.Map(configFile =>
        {
            var extension = configFile.Extension.ToLowerInvariant();
            var configFileContent = File.ReadAllText(configFile);
            switch (extension)
            {
                case ".config":
                {
                    var configDocument = new XmlDocument();
                    configDocument.LoadXml(configFileContent);
                    var reqnrollNode = configDocument.SelectSingleNode("/configuration/reqnroll");
                    if (reqnrollNode == null)
                        return LoadDefaultConfiguration(reqnrollConfiguration);

                    var configSection = ConfigurationSectionHandler.CreateFromXml(reqnrollNode);
                    var loader = new AppConfigConfigurationLoader();
                    return loader.LoadAppConfig(reqnrollConfiguration, configSection);
                }
                case ".json":
                {
                    configFileContent = ConvertToJsonSpecFlow3Style(configFileContent);

                    var loader = new JsonConfigurationLoader();
                    return loader.LoadJson(reqnrollConfiguration, configFileContent);
                }
                default: throw new ConfigurationErrorsException($"Invalid config type: {_configFile}");
            }
        }).Reduce(() => LoadDefaultConfiguration(reqnrollConfiguration));
    }

    public void TraceConfigSource(ITraceListener traceListener, ReqnrollConfiguration reqnrollConfiguration)
    {
        traceListener.WriteToolOutput($"Using config from: {_configFile}");
    }

    public ReqnrollConfiguration Load(ReqnrollConfiguration reqnrollConfiguration,
        IReqnrollConfigurationHolder reqnrollConfigurationHolder) => throw new NotSupportedException();

    public ReqnrollConfiguration Update(ReqnrollConfiguration reqnrollConfiguration,
        ConfigurationSectionHandler reqnrollConfigSection) => throw new NotSupportedException();

    protected virtual string ConvertToJsonSpecFlow3Style(string configFileContent) => configFileContent;

    private static ReqnrollConfiguration LoadDefaultConfiguration(ReqnrollConfiguration? reqnrollConfiguration) =>
        reqnrollConfiguration ?? ConfigurationLoader.GetDefault();
}
