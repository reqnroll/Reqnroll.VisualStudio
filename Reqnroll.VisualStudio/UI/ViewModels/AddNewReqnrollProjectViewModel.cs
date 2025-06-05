#nullable disable
namespace Reqnroll.VisualStudio.UI.ViewModels;

public class AddNewReqnrollProjectViewModel : INotifyPropertyChanged
{
    private const string MsTest = "MsTest";
    private const string Net8 = "net8.0";

    public AddNewReqnrollProjectViewModel() { }
    public AddNewReqnrollProjectViewModel(IEnumerable<string> testFrameworkNames)
    {
        TestFrameworks = new (testFrameworkNames);
    }
#if DEBUG
    public static AddNewReqnrollProjectViewModel DesignData = new()
    {
        DotNetFramework = Net8,
        UnitTestFramework = MsTest,
        FluentAssertionsIncluded = false
    };
#endif
    private string _dotNetFramework = Net8;

    public string DotNetFramework
    {
        get => _dotNetFramework;
        set
        {
            _dotNetFramework = value;
            OnPropertyChanged(nameof(TestFrameworks));
        }
    }

    public string UnitTestFramework { get; set; } = MsTest;
    // FluentAssertions suggestion is temporarily hidden from the UI as it is not free for commercial use anymore. 
    // See https://xceed.com/fluent-assertions-faq/
    // Maybe we could consider suggesting https://github.com/shouldly/shouldly instead.
    public bool FluentAssertionsIncluded { get; set; } = false;
    public ObservableCollection<string> TestFrameworks { get; } = new(new List<string> { "MSTest", "NUnit", "xUnit" });

    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
