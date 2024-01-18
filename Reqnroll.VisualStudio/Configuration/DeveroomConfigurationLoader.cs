#nullable disable
using System.Linq.Expressions;

namespace Reqnroll.VisualStudio.Configuration;

public class DeveroomConfigurationLoader
{
    private readonly IConfigDeserializer<DeveroomConfiguration> _configDeserializer;
    private readonly IFileSystem _fileSystem;

    private DeveroomConfigurationLoader(
        IConfigDeserializer<DeveroomConfiguration> configDeserializer,
        IFileSystem fileSystem)
    {
        _configDeserializer = configDeserializer;
        _fileSystem = fileSystem;
    }

    public static DeveroomConfigurationLoader CreateReqnrollJsonConfigurationLoader(IFileSystem fileSystem) =>
        new(new ReqnrollConfigDeserializer(), fileSystem);

    public static DeveroomConfigurationLoader CreateDeveroomJsonConfigurationLoader(IFileSystem fileSystem) =>
        new(new JsonNetConfigDeserializer<DeveroomConfiguration>(), fileSystem);

    public DeveroomConfiguration Load(string configFilePath)
    {
        var config = new DeveroomConfiguration();
        Update(config, configFilePath);
        return config;
    }

    public void Update(DeveroomConfiguration config, string configFilePath)
    {
        if (!_fileSystem.File.Exists(configFilePath))
            throw new DeveroomConfigurationException($"The specified config file '{configFilePath}' does not exist.");
        var configFolder = Path.GetDirectoryName(configFilePath) ??
                           throw new DeveroomConfigurationException(
                               $"The specified config file '{configFilePath}' does not contain a folder.");

        var jsonString = _fileSystem.File.ReadAllText(configFilePath);
        Update(config, jsonString, configFolder);
    }

    private void Update(DeveroomConfiguration config, string configFileContent, string configFolder)
    {
        _configDeserializer.Populate(configFileContent, config);

        config.ConfigurationBaseFolder = configFolder;

        config.Reqnroll.ConfigFilePath = EnsureFullPath(config, c => c.Reqnroll.ConfigFilePath);
        config.SpecFlow.ConfigFilePath = EnsureFullPath(config, c => c.SpecFlow.ConfigFilePath);
        config.SpecFlow.GeneratorFolder = EnsureFullPath(config, c => c.SpecFlow.GeneratorFolder, true);
    }

    private string ExpandEnvironmentVariables(string value)
    {
        if (value == null)
            return null;
        return Environment.ExpandEnvironmentVariables(value);
    }

    private string EnsureFullPath(DeveroomConfiguration config, string filePath, string label, bool isFolder = false)
    {
        if (filePath == null)
            return null;
        filePath = ExpandEnvironmentVariables(filePath);
        var fullPath = Path.GetFullPath(Path.Combine(config.ConfigurationBaseFolder, filePath));
        if (!isFolder && !File.Exists(fullPath))
            throw new DeveroomConfigurationException(
                $"Unable to access file '{fullPath}'. Please make sure you specify a path for an existing file for the {label} option.");
        if (isFolder && !Directory.Exists(fullPath))
            throw new DeveroomConfigurationException(
                $"Unable to access directory '{fullPath}'. Please make sure you specify a path for an existing directory for the {label} option.");
        return fullPath;
    }

    private string EnsureFullPath(DeveroomConfiguration config,
        Expression<Func<DeveroomConfiguration, string>> configAccessor, bool isFolder = false)
    {
        var filePath = configAccessor.Compile().Invoke(config);
        return EnsureFullPath(config, filePath, configAccessor.ToString(), isFolder);
    }
}
