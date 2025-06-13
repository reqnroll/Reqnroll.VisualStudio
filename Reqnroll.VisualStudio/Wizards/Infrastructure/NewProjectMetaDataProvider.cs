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
    private NewProjectMetaRecord _retrievedData;
    private readonly IHttpClient _httpClient;

    [ImportingConstructor]
    public NewProjectMetaDataProvider(IHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void RetrieveNewProjectMetaData(Action<NewProjectMetaData> onRetrievedAction)
    {
        _retrievedData = FetchDescriptorsFromReqnrollWebsite(_httpClient);
        var md = new NewProjectMetaData(_retrievedData);
        onRetrievedAction(md);
    }

    private NewProjectMetaRecord FetchDescriptorsFromReqnrollWebsite(IHttpClient httpClient)
    {
        try
        {
            using (var cts = new DebuggableCancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                var overrideUrl = Environment.GetEnvironmentVariable(environmentVariableOverrideOfMetaDataEndpointURL);
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
        var dependencies = _retrievedData.TestFrameworks.First(tf => tf.Label == testFramework).Dependencies;
        return dependencies;
    }

    internal virtual NewProjectMetaRecord CreateFallBackMetaData()
    {
        // read static metadata from a resource file, deserialize the resulting json, storing it in the _fallbackTestFrameworkDescriptors field
        var resourceName = "Reqnroll.VisualStudio.Resources.TestFrameworkDescriptors.json";
        using (var stream = typeof(NewProjectMetaDataProvider).Assembly.GetManifestResourceStream(resourceName))
        using (var reader = new StreamReader(stream))
        {
            var json = reader.ReadToEnd();
            var data = JsonSerialization.DeserializeObject<NewProjectMetaRecord>(json);
            return data;
        }
    }

}
