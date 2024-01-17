namespace SpecFlow.VisualStudio.SpecFlowConnector.Generation;

/// <summary>
///     Design time code generation is not supported after specflow version >= 3.0.0
/// </summary>
public abstract class SpecFlowVLatestGenerator : BaseGenerator
{
    protected override SpecFlowConfigurationHolder CreateConfigHolder(string configFilePath)
    {
        var extension = Path.GetExtension(configFilePath) ?? "";
        var configFileContent = File.ReadAllText(configFilePath);
        switch (extension.ToLowerInvariant())
        {
            case ".config":
            {
                return GetXmlConfigurationHolder(configFileContent);
            }
            case ".json":
            {
                if (!IsSpecFlowV2Json(configFileContent))
                    return new SpecFlowConfigurationHolder();

                return new SpecFlowConfigurationHolder(ConfigSource.Json, configFileContent);
            }
        }

        throw new ConfigurationErrorsException($"Invalid config type: {configFilePath}");
    }

    private bool IsSpecFlowV2Json(string configFileContent)
    {
        try
        {
            var configObject = JObject.Parse(configFileContent);
            return configObject["specFlow"] != null;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
