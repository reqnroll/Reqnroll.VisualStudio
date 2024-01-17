using System;
using System.Xml;
using TechTalk.SpecFlow.Configuration.AppConfig;
using TechTalk.SpecFlow.Configuration.JsonConfig;

namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery;

public class SpecFlowConfigurationLoader : IConfigurationLoader
{
    private readonly string _configFilePath;

    public SpecFlowConfigurationLoader(string configFilePath)
    {
        _configFilePath = configFilePath;
    }

    public SpecFlowConfiguration Load(SpecFlowConfiguration specFlowConfiguration)
    {
        if (_configFilePath == null)
            return LoadDefaultConfiguration(specFlowConfiguration);

        var extension = Path.GetExtension(_configFilePath);
        var configFileContent = File.ReadAllText(_configFilePath);
        switch (extension.ToLowerInvariant())
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
        }

        throw new ConfigurationErrorsException($"Invalid config type: {_configFilePath}");
    }

    public void TraceConfigSource(ITraceListener traceListener, SpecFlowConfiguration specFlowConfiguration)
    {
        traceListener.WriteToolOutput($"Using config from: {_configFilePath ?? "<default>"}");
    }

    public SpecFlowConfiguration Load(SpecFlowConfiguration specFlowConfiguration,
        ISpecFlowConfigurationHolder specFlowConfigurationHolder) => throw new NotSupportedException();

    public SpecFlowConfiguration Update(SpecFlowConfiguration specFlowConfiguration,
        ConfigurationSectionHandler specFlowConfigSection) => throw new NotSupportedException();

    protected virtual string ConvertToJsonSpecFlow3Style(string configFileContent) => configFileContent;

    private static SpecFlowConfiguration LoadDefaultConfiguration(SpecFlowConfiguration specFlowConfiguration) =>
        specFlowConfiguration ?? ConfigurationLoader.GetDefault();
}
