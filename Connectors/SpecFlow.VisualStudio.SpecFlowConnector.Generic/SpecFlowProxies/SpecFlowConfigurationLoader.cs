using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Configuration.AppConfig;
using TechTalk.SpecFlow.Configuration.JsonConfig;
using TechTalk.SpecFlow.Tracing;

namespace SpecFlowConnector.SpecFlowProxies;

public class SpecFlowConfigurationLoader : IConfigurationLoader
{
    private readonly Option<FileDetails> _configFile;

    public SpecFlowConfigurationLoader(Option<FileDetails> configFile)
    {
        _configFile = configFile;
    }

    public SpecFlowConfiguration Load(SpecFlowConfiguration? specFlowConfiguration)
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
                    var specFlowNode = configDocument.SelectSingleNode("/configuration/specFlow");
                    if (specFlowNode == null)
                        return LoadDefaultConfiguration(specFlowConfiguration);

                    var configSection = ConfigurationSectionHandler.CreateFromXml(specFlowNode);
                    var loader = new AppConfigConfigurationLoader();
                    return loader.LoadAppConfig(specFlowConfiguration, configSection);
                }
                case ".json":
                {
                    configFileContent = ConvertToJsonSpecFlow3Style(configFileContent);

                    var loader = new JsonConfigurationLoader();
                    return loader.LoadJson(specFlowConfiguration, configFileContent);
                }
                default: throw new ConfigurationErrorsException($"Invalid config type: {_configFile}");
            }
        }).Reduce(() => LoadDefaultConfiguration(specFlowConfiguration));
    }

    public void TraceConfigSource(ITraceListener traceListener, SpecFlowConfiguration specFlowConfiguration)
    {
        traceListener.WriteToolOutput($"Using config from: {_configFile}");
    }

    public SpecFlowConfiguration Load(SpecFlowConfiguration specFlowConfiguration,
        ISpecFlowConfigurationHolder specFlowConfigurationHolder) => throw new NotSupportedException();

    public SpecFlowConfiguration Update(SpecFlowConfiguration specFlowConfiguration,
        ConfigurationSectionHandler specFlowConfigSection) => throw new NotSupportedException();

    protected virtual string ConvertToJsonSpecFlow3Style(string configFileContent) => configFileContent;

    private static SpecFlowConfiguration LoadDefaultConfiguration(SpecFlowConfiguration? specFlowConfiguration) =>
        specFlowConfiguration ?? ConfigurationLoader.GetDefault();
}
