using System;
using System.Xml;
using Reqnroll.Configuration.AppConfig;
using Reqnroll.Configuration.JsonConfig;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Discovery;

public class ReqnrollConfigurationLoader : IConfigurationLoader
{
    private readonly string _configFilePath;

    public ReqnrollConfigurationLoader(string configFilePath)
    {
        _configFilePath = configFilePath;
    }

    public ReqnrollConfiguration Load(ReqnrollConfiguration reqnrollConfiguration)
    {
        if (_configFilePath == null)
            return LoadDefaultConfiguration(reqnrollConfiguration);

        var extension = Path.GetExtension(_configFilePath);
        var configFileContent = File.ReadAllText(_configFilePath);
        switch (extension.ToLowerInvariant())
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
        }

        throw new ConfigurationErrorsException($"Invalid config type: {_configFilePath}");
    }

    public void TraceConfigSource(ITraceListener traceListener, ReqnrollConfiguration reqnrollConfiguration)
    {
        traceListener.WriteToolOutput($"Using config from: {_configFilePath ?? "<default>"}");
    }

    public ReqnrollConfiguration Load(ReqnrollConfiguration reqnrollConfiguration,
        IReqnrollConfigurationHolder reqnrollConfigurationHolder) => throw new NotSupportedException();

    public ReqnrollConfiguration Update(ReqnrollConfiguration reqnrollConfiguration,
        ConfigurationSectionHandler reqnrollConfigSection) => throw new NotSupportedException();

    protected virtual string ConvertToJsonSpecFlow3Style(string configFileContent) => configFileContent;

    private static ReqnrollConfiguration LoadDefaultConfiguration(ReqnrollConfiguration reqnrollConfiguration) =>
        reqnrollConfiguration ?? ConfigurationLoader.GetDefault();
}
