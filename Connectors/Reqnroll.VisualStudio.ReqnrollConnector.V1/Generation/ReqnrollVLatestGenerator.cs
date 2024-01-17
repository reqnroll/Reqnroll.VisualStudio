namespace Reqnroll.VisualStudio.ReqnrollConnector.Generation;

/// <summary>
///     Design time code generation is not supported after reqnroll version >= 3.0.0
/// </summary>
public abstract class ReqnrollVLatestGenerator : BaseGenerator
{
    protected override ReqnrollConfigurationHolder CreateConfigHolder(string configFilePath)
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
                if (!IsReqnrollV2Json(configFileContent))
                    return new ReqnrollConfigurationHolder();

                return new ReqnrollConfigurationHolder(ConfigSource.Json, configFileContent);
            }
        }

        throw new ConfigurationErrorsException($"Invalid config type: {configFilePath}");
    }

    private bool IsReqnrollV2Json(string configFileContent)
    {
        try
        {
            var configObject = JObject.Parse(configFileContent);
            return configObject["reqnroll"] != null;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
