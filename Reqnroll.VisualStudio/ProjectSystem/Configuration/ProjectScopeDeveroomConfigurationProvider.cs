#nullable disable
using System;
using System.Xml.XPath;
using System.Linq;
using System.Xml.Linq;

namespace Reqnroll.VisualStudio.ProjectSystem.Configuration;

public class ProjectScopeDeveroomConfigurationProvider : IDeveroomConfigurationProvider, IDisposable
{
    public const string ReqnrollJsonConfigFileName = "reqnroll.json";
    public const string ReqnrollAppConfigFileName = "App.config";
    public const string SpecSyncJsonConfigFileName = "specsync.json";
    public const string DeveroomConfigFileName = "deveroom.json";

    private readonly IProjectScope _projectScope;
    private ConfigCache _configCache;

    public ProjectScopeDeveroomConfigurationProvider([NotNull] IProjectScope projectScope)
    {
        _projectScope = projectScope ?? throw new ArgumentNullException(nameof(projectScope));
        InitializeConfiguration();

        _projectScope.IdeScope.WeakProjectsBuilt += ProjectSystemOnProjectsBuilt;
    }

    private IDeveroomLogger Logger => _projectScope.IdeScope.Logger;
    private IMonitoringService MonitoringService => _projectScope.IdeScope.MonitoringService;
    private IFileSystem FileSystem => _projectScope.IdeScope.FileSystem;

    public event EventHandler<EventArgs> WeakConfigurationChanged
    {
        add => WeakEventManager<ProjectScopeDeveroomConfigurationProvider, EventArgs>.AddHandler(this,
            nameof(ConfigurationChanged), value);
        remove => WeakEventManager<ProjectScopeDeveroomConfigurationProvider, EventArgs>.RemoveHandler(this,
            nameof(ConfigurationChanged), value);
    }

    public DeveroomConfiguration GetConfiguration() => _configCache.Configuration;

    public void Dispose()
    {
        _projectScope.IdeScope.WeakProjectsBuilt -= ProjectSystemOnProjectsBuilt;
    }

    public event EventHandler<EventArgs> ConfigurationChanged;

    private void InitializeConfiguration()
    {
        var configSources = _projectScope is VoidProjectScope
            ? Array.Empty<ConfigSource>()
            : GetConfigSources().ToArray();
        _configCache = LoadConfiguration(configSources);
    }

    private void ProjectSystemOnProjectsBuilt(object sender, EventArgs e)
    {
        CheckConfiguration(true);
    }

    private void CheckConfiguration(bool triggerChanged)
    {
        Logger.LogVerbose("Checking configuration...");

        var configSources = GetConfigSources().ToArray();
        if (_configCache.ConfigSources.SequenceEqual(configSources))
            return; // no source changed

        var oldConfiguration = _configCache.Configuration;
        _configCache = LoadConfiguration(configSources);
        Logger.LogVerbose("Configuration loaded");

        if (triggerChanged && !oldConfiguration.Equals(_configCache.Configuration))
        {
            Logger.LogInfo("Configuration changed");
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private DeveroomConfiguration GetDefaultConfiguration() => new();

    private IEnumerable<ConfigSource> GetConfigSources()
    {
        var jsonSource = GetProjectConfigFilePath(ReqnrollJsonConfigFileName);
        if (jsonSource != null)
        {
            yield return jsonSource;
        }
        else
        {
            var appConfigSource = GetProjectConfigFilePath(ReqnrollAppConfigFileName);
            if (appConfigSource != null)
                yield return appConfigSource;
        }

        var specSyncConfigSource = GetProjectConfigFilePath(SpecSyncJsonConfigFileName);
        if (specSyncConfigSource != null)
            yield return specSyncConfigSource;

        var deveroomConfigSource = GetProjectConfigFilePath(DeveroomConfigFileName);
        if (deveroomConfigSource != null)
            yield return deveroomConfigSource;
    }

    private ConfigSource GetProjectConfigFilePath(string fileName)
    {
        try
        {
            var projectFolder = _projectScope.ProjectFolder;
            var fileSystem = _projectScope.IdeScope.FileSystem;
            var configFilePath = fileSystem.GetFilePathIfExists(Path.Combine(projectFolder, fileName));

            if (fileName.Equals(ReqnrollAppConfigFileName)) configFilePath ??= GetAppConfigPathFromProject();
            if (configFilePath == null)
                return null;

            return ConfigSource.CreateValid(configFilePath, FileSystem.File.GetLastWriteTimeUtc(configFilePath));
        }
        catch (Exception ex)
        {
            Logger.LogDebugException(ex);
            return null;
        }
    }

    private string GetAppConfigPathFromProject()
    {
        var projectFilePath = _projectScope.ProjectFullName;
        using var configFile = FileSystem.File.OpenRead(_projectScope.ProjectFullName);
        XElement csProjXElement = XElement.Load(configFile);

        string appConfigPath = csProjXElement
            .Element("PropertyGroup")?
            .Element("AppConfig")?
            .Value;
        if (!string.IsNullOrEmpty(appConfigPath) && !Path.IsPathRooted(appConfigPath))
            appConfigPath = Path.Combine(Path.GetDirectoryName(projectFilePath)!, appConfigPath);

        return appConfigPath;
    }

    private ConfigCache LoadConfiguration(ConfigSource[] configSources)
    {
        var loadedSources = new List<ConfigSource>();
        var configuration = GetDefaultConfiguration();

        foreach (var configSource in configSources)
            try
            {
                var fileName = Path.GetFileName(configSource.FilePath);
                if (ReqnrollAppConfigFileName.Equals(fileName, StringComparison.InvariantCultureIgnoreCase))
                    LoadFromReqnrollXmlConfig(configSource.FilePath, configuration);

                if (ReqnrollJsonConfigFileName.Equals(fileName, StringComparison.InvariantCultureIgnoreCase))
                    LoadFromReqnrollJsonConfig(configSource.FilePath, configuration);

                if (SpecSyncJsonConfigFileName.Equals(fileName, StringComparison.InvariantCultureIgnoreCase))
                    LoadFromSpecSyncJsonConfig(configSource.FilePath, configuration);

                if (DeveroomConfigFileName.Equals(fileName, StringComparison.InvariantCultureIgnoreCase))
                    LoadFromDeveroomConfig(configSource.FilePath, configuration);

                configuration.CheckConfiguration();

                loadedSources.Add(configSource);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Unable to load configuration from '{configSource.FilePath}': {ex.Message}");
                Logger.LogVerboseException(MonitoringService, ex, "Unable to load configuration");
            }

        if (loadedSources.Any())
            configuration.ConfigurationChangeTime = loadedSources.Max(cs => cs.LastChangeTime);

        try
        {
            configuration.CheckConfiguration();
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Invalid Reqnroll Visual Studio configuration: {ex.Message}");
            Logger.LogVerboseException(MonitoringService, ex, "Configuration error, using default config");
            configuration = new DeveroomConfiguration();
        }

        return new ConfigCache(configuration, loadedSources.ToArray());
    }

    private void LoadFromDeveroomConfig(string configSourceFilePath, DeveroomConfiguration configuration)
    {
        Logger.LogVerbose($"Loading Deveroom config from '{configSourceFilePath}'");
        var loader = DeveroomConfigurationLoader.CreateDeveroomJsonConfigurationLoader(FileSystem);
        loader.Update(configuration, configSourceFilePath);
    }

    private string XPathEvaluateAttribute(XDocument doc, string xpath) => (doc.XPathEvaluate(xpath) as IEnumerable)
        ?.OfType<XAttribute>().FirstOrDefault()?.Value;

    private void LoadFromReqnrollXmlConfig(string configSourceFilePath, DeveroomConfiguration configuration)
    {
        var fileContent = FileSystem.File.ReadAllText(configSourceFilePath);
        var configDoc = XDocument.Parse(fileContent);
        var featureLang = XPathEvaluateAttribute(configDoc, "/configuration/reqnroll/language/@feature");
        if (featureLang != null)
            configuration.DefaultFeatureLanguage = featureLang;
        var bindingCulture = XPathEvaluateAttribute(configDoc, "/configuration/reqnroll/bindingCulture/@name");
        if (bindingCulture != null)
            configuration.ConfiguredBindingCulture = bindingCulture;
    }

    private void LoadFromReqnrollJsonConfig(string configSourceFilePath, DeveroomConfiguration configuration)
    {
        Logger.LogVerbose($"Loading configuration from '{configSourceFilePath}'");
        var configLoader = DeveroomConfigurationLoader.CreateReqnrollJsonConfigurationLoader(FileSystem);
        configLoader.Update(configuration, configSourceFilePath);
    }

    private void LoadFromSpecSyncJsonConfig(string configSourceFilePath, DeveroomConfiguration configuration)
    {
        var fileContent = FileSystem.File.ReadAllText(configSourceFilePath);
        UpdateFromSpecSyncJsonConfig(configuration, fileContent);
    }

    internal static void UpdateFromSpecSyncJsonConfig(DeveroomConfiguration configuration, string fileContent)
    {
        var configDoc = JObject.Parse(fileContent);

        var projectUrl = ((string) configDoc["remote"]?["projectUrl"])?.TrimEnd('/');
        if (string.IsNullOrEmpty(projectUrl))
            return;

        var testCaseTagPrefix = (string) configDoc["synchronization"]?["testCaseTagPrefix"] ?? "tc";

        var tagLinks = new List<TagLinkConfiguration>(configuration.Traceability.TagLinks);
        AddSpecSyncTagLinkConfiguration(tagLinks, testCaseTagPrefix, projectUrl);

        var linksArray = configDoc["synchronization"]?["links"] as JArray;
        if (linksArray != null)
            foreach (var link in linksArray)
            {
                var tagPrefix = (string) link["tagPrefix"];
                if (string.IsNullOrEmpty(tagPrefix))
                    continue;
                AddSpecSyncTagLinkConfiguration(tagLinks, tagPrefix, projectUrl);
            }

        configuration.Traceability.TagLinks = tagLinks.ToArray();
    }

    private static void AddSpecSyncTagLinkConfiguration(List<TagLinkConfiguration> tagLinks, string tagPrefix,
        string projectUrl)
    {
        tagLinks.Add(new TagLinkConfiguration
        {
            TagPattern = $@"{tagPrefix}\:(?<id>\d+)",
            UrlTemplate = projectUrl + "/_workitems/edit/{id}"
        });
    }

    public override string ToString() =>
        $"{nameof(ProjectScopeDeveroomConfigurationProvider)}({string.Join(",", _configCache.ConfigSources.Select(cs => cs.ToString()))})";
}
