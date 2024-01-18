#nullable disable
namespace Reqnroll.VisualStudio.UI.ViewModels;

public class AddNewReqnrollProjectViewModel : INotifyPropertyChanged
{
    private const string MsTest = "MsTest";
    private const string Net8 = "net8.0";

#if DEBUG
    public static AddNewReqnrollProjectViewModel DesignData = new()
    {
        DotNetFramework = Net8,
        UnitTestFramework = MsTest,
        FluentAssertionsIncluded = true
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
    public bool FluentAssertionsIncluded { get; set; } = true;
    public ObservableCollection<string> TestFrameworks { get; } = new(new List<string> { "MSTest", "NUnit", "xUnit" });

    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
