namespace Reqnroll.VisualStudio.Wizards.Infrastructure;

public interface INewProjectMetaDataProvider
{
    IEnumerable<string> TestFrameworks { get; }
    IEnumerable<NugetPackageDescriptor> DependenciesOf(string testFramework);
}
public record NugetPackageDescriptor(string name, string version);
public record TestFrameworkInfoModel(string description, IEnumerable<NugetPackageDescriptor> dependencies);

[Export(typeof(INewProjectMetaDataProvider))]
public class NewProjectMetaDataProvider : INewProjectMetaDataProvider
{
    private Dictionary<string, TestFrameworkInfoModel> _fallbackTestFrameworkDescriptors = new();
    private Task<Dictionary<string, TestFrameworkInfoModel>> _httpTask;
    private Lazy<Dictionary<string, TestFrameworkInfoModel>> _resolvedTestFrameworkDescriptors;

    [ImportingConstructor]
    public NewProjectMetaDataProvider()
    {
        // read static metadata from a resource file, deserialize the resulting json, storing it in the _fallbackTestFrameworkDescriptors field
        var resourceName = "Reqnroll.VisualStudio.Resources.TestFrameworkDescriptors.json";
        using (var stream = typeof(NewProjectMetaDataProvider).Assembly.GetManifestResourceStream(resourceName))
        using (var reader = new StreamReader(stream))
        {
            var json = reader.ReadToEnd();
            var data = JsonSerialization.DeserializeObject<Dictionary<string, TestFrameworkInfoModel>>(json);
            if (data != null)
                _fallbackTestFrameworkDescriptors = data;
        }

        // launch a task to read metadata from http resource and deserialize the resulting json, save the Task to a field
        _httpTask = Task.Run(async () =>
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var httpJson = await httpClient.GetStringAsync("https://example.com/testframeworks.json").ConfigureAwait(false);
                    var httpData = JsonSerialization.DeserializeObject<Dictionary<string, TestFrameworkInfoModel>>(httpJson);
                    return httpData;
                }
            }
            catch
            {
                return null;
            }
        });

        _resolvedTestFrameworkDescriptors = new Lazy<Dictionary<string, TestFrameworkInfoModel>>(FinishRetrievalOfTestFrameworkDescriptors);
    }

    public IEnumerable<string> TestFrameworks
    {
        get
        {
            var descriptors = _resolvedTestFrameworkDescriptors.Value;
            return descriptors.Keys;
        }
    }

    public IEnumerable<NugetPackageDescriptor> DependenciesOf(string testFramework)
    {
        var descriptors = _resolvedTestFrameworkDescriptors.Value;
        return descriptors[testFramework].dependencies;
    }

    private Dictionary<string, TestFrameworkInfoModel> FinishRetrievalOfTestFrameworkDescriptors()
    {
        // Wait for the HTTP task to complete
        if (_httpTask == null)
            return _fallbackTestFrameworkDescriptors;

        try
        {
            var result = _httpTask.GetAwaiter().GetResult();
            if (result != null)
            {
                return result;
            }
        }
        catch
        {
            // Ignore exceptions, keep existing _fallbackTestFrameworkDescriptors
        }
        // If we've gotten to this point, the http end point returned no result or failed. So, we fall back.
        return _fallbackTestFrameworkDescriptors;
    }
}
