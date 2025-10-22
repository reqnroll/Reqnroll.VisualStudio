#nullable disable
namespace Reqnroll.VisualStudio.UI.ViewModels;

public class AddNewReqnrollProjectViewModel : INotifyPropertyChanged
{
    private const string MsTest = "MsTest";
    private const string Net8 = "net8.0";
    private const string Net9 = "net9.0";
    private const string Net10 = "net10.0";

#if DEBUG
    public static AddNewReqnrollProjectViewModel DesignData = new()
    {
        DotNetFramework = Net9,
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
    public ObservableCollection<string> TestFrameworks { get; } = new(new List<string> { "MSTest", "NUnit", "xUnit", "xUnit.v3", "TUnit" });

    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
