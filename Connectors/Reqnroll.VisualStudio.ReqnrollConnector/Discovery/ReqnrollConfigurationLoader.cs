using System;
using System.Xml;
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
            case ".config": // for SpecFlow compatibility
            {
                var configDocument = new XmlDocument();
                configDocument.LoadXml(configFileContent);
                var specFlowNode = configDocument.SelectSingleNode("/configuration/specFlow");
                if (specFlowNode == null)
                    return LoadDefaultConfiguration(reqnrollConfiguration);

                return LegacyAppConfigLoader.LoadConfiguration(specFlowNode, LoadDefaultConfiguration(reqnrollConfiguration));
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

    protected virtual string ConvertToJsonSpecFlow3Style(string configFileContent) => configFileContent;

    private static ReqnrollConfiguration LoadDefaultConfiguration(ReqnrollConfiguration reqnrollConfiguration) =>
        reqnrollConfiguration ?? ConfigurationLoader.GetDefault();
}
