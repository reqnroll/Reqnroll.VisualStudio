namespace Reqnroll.VisualStudio.Wizards.Infrastructure;

public interface INewProjectMetaDataProvider
{
    IEnumerable<string> TestFrameworks { get; }
    string DependenciesOf(string testFramework);
}
public record TestFrameworkInfoModel(string description, string dependencies);

[Export(typeof(INewProjectMetaDataProvider))]
public class NewProjectMetaDataProvider : INewProjectMetaDataProvider
{
    private Dictionary<string, TestFrameworkInfoModel> _testFrameworkDescriptors = new();
    private Task<Dictionary<string, TestFrameworkInfoModel>> _httpTask;
    private Lazy<Dictionary<string, TestFrameworkInfoModel>> _resolvedTestFrameworkDescriptors;

    [ImportingConstructor]
    public NewProjectMetaDataProvider()
    {
        // read static metadata from a resource file, deserialize the resulting json, storing it in the _testFrameworkDescriptors field
        var resourceName = "Reqnroll.VisualStudio.Resources.TestFrameworkDescriptors.json";
        using (var stream = typeof(NewProjectMetaDataProvider).Assembly.GetManifestResourceStream(resourceName))
        using (var reader = new StreamReader(stream))
        {
            var json = reader.ReadToEnd();
            var data = JsonSerialization.DeserializeObject<Dictionary<string, TestFrameworkInfoModel>>(json);
            if (data != null)
                _testFrameworkDescriptors = data;
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

    public string DependenciesOf(string testFramework)
    {
        var descriptors = _resolvedTestFrameworkDescriptors.Value;
        return descriptors[testFramework].dependencies.Replace("\\n", Environment.NewLine);
    }

    private Dictionary<string, TestFrameworkInfoModel> FinishRetrievalOfTestFrameworkDescriptors()
    {
        // Wait for the HTTP task to complete
        if (_httpTask == null)
            return _testFrameworkDescriptors;

        try
        {
            var result = _httpTask.GetAwaiter().GetResult();
            if (result != null)
            {
                _testFrameworkDescriptors = result;
            }
        }
        catch
        {
            // Ignore exceptions, keep existing _testFrameworkDescriptors
        }
        return _testFrameworkDescriptors;
    }
}
