namespace Reqnroll.VisualStudio.Wizards.Infrastructure;

public interface INewProjectMetaDataProvider
{
    IEnumerable<NugetPackageDescriptor> DependenciesOf(string testFramework);
    Task<NewProjectMetaData> RetrieveNewProjectMetaDataAsync();
    NewProjectMetaData GetFallbackMetadata();
}

[Export(typeof(INewProjectMetaDataProvider))]
public class NewProjectMetaDataProvider : INewProjectMetaDataProvider
{
    private const string EnvironmentVariableOverrideOfMetaDataEndpointUrl = "REQNROLL_VISUALSTUDIOEXTENSION_NPW_FRAMEWORKMETADATAENDPOINTURL";
    private const string MetaDataEndpointUrl = "https://assets.reqnroll.net/testframeworkmetadata/testframeworks.json";
    private NewProjectMetaData _metadata;
    private readonly IHttpClient _httpClient;
    private readonly IEnvironmentWrapper _environmentWrapper;

    [ImportingConstructor]
    public NewProjectMetaDataProvider(IHttpClient httpClient, IEnvironmentWrapper environmentWrapper)
    {
        _httpClient = httpClient;
        _environmentWrapper = environmentWrapper;
        _metadata = GetFallbackMetadata();
    }

    public async Task<NewProjectMetaData> RetrieveNewProjectMetaDataAsync()
    {
        try
        {
            using var cts = new DebuggableCancellationTokenSource(TimeSpan.FromSeconds(10));

            var overrideUrl = _environmentWrapper.GetEnvironmentVariable(EnvironmentVariableOverrideOfMetaDataEndpointUrl);
            var url = overrideUrl ?? MetaDataEndpointUrl;
            var httpJson = await _httpClient.GetStringAsync(url, cts);
            var httpData = JsonSerialization.DeserializeObject<NewProjectMetaRecord?>(httpJson);
            if (httpData != null)
                _metadata = new NewProjectMetaData(httpData);
            return _metadata;
        }
        catch
        {
            return _metadata;
        }
    }

    public NewProjectMetaData GetFallbackMetadata() => new(CreateFallBackMetaDataRecord(), isFallback: true);

    public IEnumerable<NugetPackageDescriptor> DependenciesOf(string testFramework)
    {
        IEnumerable<NugetPackageDescriptor> dependencies = Enumerable.Empty<NugetPackageDescriptor>();
        if (_metadata.TestFrameworkMetaData.TryGetValue(testFramework, out var framework))
        {
            dependencies = framework.Dependencies;
        }
        return dependencies;
    }

    internal virtual NewProjectMetaRecord CreateFallBackMetaDataRecord()
    {
        NewProjectMetaRecord CreateEmpty() =>
            new()
            {
                DotNetFrameworks = new(),
                TestFrameworks = new(),
                ValidationFrameworks = new()
            };

        try
        {
            // read static metadata from a resource file, deserialize the resulting json
            var resourceName = "Reqnroll.VisualStudio.Resources.TestFrameworkDescriptors.json";
            var assembly = typeof(NewProjectMetaDataProvider).Assembly;
            using var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                return CreateEmpty(); // Resource not found
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var data = JsonSerialization.DeserializeObject<NewProjectMetaRecord>(json);
            return data ?? CreateEmpty(); // Could be null if deserialization fails
        }
        catch
        {
            // Any exception during resource reading or deserialization
            return CreateEmpty();
        }
    }
}
