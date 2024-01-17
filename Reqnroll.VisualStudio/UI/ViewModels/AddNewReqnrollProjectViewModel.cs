#nullable disable
namespace Reqnroll.VisualStudio.UI.ViewModels;

public class AddNewReqnrollProjectViewModel : INotifyPropertyChanged
{
    private const string Runner = "Reqnroll + Runner";
    private const string Net6 = "net6.0";

#if DEBUG
    public static AddNewReqnrollProjectViewModel DesignData = new()
    {
        DotNetFramework = Net6,
        UnitTestFramework = Runner,
        FluentAssertionsIncluded = true
    };
#endif
    private string _dotNetFramework = Net6;

    public string DotNetFramework
    {
        get => _dotNetFramework;
        set
        {
            _dotNetFramework = value;
            if (_dotNetFramework == Net6 && TestFrameworks.Contains(Runner))
            {
                TestFrameworks.Remove(Runner);
                UnitTestFramework = TestFrameworks[0];
                OnPropertyChanged(nameof(UnitTestFramework));
            }

            if (_dotNetFramework != Net6 && !TestFrameworks.Contains(Runner)) TestFrameworks.Add(Runner);
            OnPropertyChanged(nameof(TestFrameworks));
        }
    }

    public string UnitTestFramework { get; set; } = Runner;
    public bool FluentAssertionsIncluded { get; set; } = true;
    public ObservableCollection<string> TestFrameworks { get; } = new(new List<string> {"NUnit", "xUnit", "MSTest"});

    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
