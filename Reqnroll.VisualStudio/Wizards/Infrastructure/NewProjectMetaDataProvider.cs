namespace Reqnroll.VisualStudio.Wizards.Infrastructure;

public interface INewProjectMetaDataProvider
{
    IEnumerable<NugetPackageDescriptor> DependenciesOf(string testFramework);
    void RetrieveNewProjectMetaData(Action<NewProjectMetaData> onRetrieved);
}

[Export(typeof(INewProjectMetaDataProvider))]
public class NewProjectMetaDataProvider : INewProjectMetaDataProvider
{
    private const string environmentVariableOverrideOfMetaDataEndpointURL = "REQNROLL_VISUALSTUDIOEXTENSION_NPW_FRAMEWORKMETADATAENDPOINTURL";
    private const string _metaDataEndpointUrl = "https://assets.reqnroll.net/testframeworkmetadata/testframeworks.json";
    private NewProjectMetaData? _metadata;
    private readonly IHttpClient _httpClient;
    private readonly IEnvironmentWrapper _environmentWrapper;

    [ImportingConstructor]
    public NewProjectMetaDataProvider(IHttpClient httpClient, IEnvironmentWrapper environmentWrapper)
    {
        _httpClient = httpClient;
        _environmentWrapper = environmentWrapper;
    }

    public void RetrieveNewProjectMetaData(Action<NewProjectMetaData> onRetrievedAction)
    {
        var retrievedData = FetchDescriptorsFromReqnrollWebsite(_httpClient);
        _metadata = new NewProjectMetaData(retrievedData);
        onRetrievedAction(_metadata);
    }

    internal NewProjectMetaRecord FetchDescriptorsFromReqnrollWebsite(IHttpClient httpClient)
    {
        try
        {
            using (var cts = new DebuggableCancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                var overrideUrl = _environmentWrapper.GetEnvironmentVariable(environmentVariableOverrideOfMetaDataEndpointURL);
                var url = overrideUrl ?? _metaDataEndpointUrl;
                var httpJson = Task.Run(() => httpClient.GetStringAsync(url, cts)).Result;
                var httpData = JsonSerialization.DeserializeObject<NewProjectMetaRecord>(httpJson);
                return httpData ?? CreateFallBackMetaData();
            }
        }
        catch
        {
            return CreateFallBackMetaData(); 
        }
    }

    public IEnumerable<NugetPackageDescriptor> DependenciesOf(string testFramework)
    {
        IEnumerable<NugetPackageDescriptor> dependencies = Enumerable.Empty<NugetPackageDescriptor>();
        if (_metadata != null && _metadata.TestFrameworkMetaData.TryGetValue(testFramework, out var framework))
        {
            dependencies = framework.Dependencies;
        }
        return dependencies;
    }

    internal virtual NewProjectMetaRecord CreateFallBackMetaData()
    {
        try
        {
            // read static metadata from a resource file, deserialize the resulting json
            var resourceName = "Reqnroll.VisualStudio.Resources.TestFrameworkDescriptors.json";
            var assembly = typeof(NewProjectMetaDataProvider).Assembly;
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return null; // Resource not found
                }
                
                using (var reader = new StreamReader(stream))
                {
                    var json = reader.ReadToEnd();
                    var data = JsonSerialization.DeserializeObject<NewProjectMetaRecord>(json);
                    return data; // Could be null if deserialization fails
                }
            }
        }
        catch
        {
            // Any exception during resource reading or deserialization
            return null;
        }
    }
}
