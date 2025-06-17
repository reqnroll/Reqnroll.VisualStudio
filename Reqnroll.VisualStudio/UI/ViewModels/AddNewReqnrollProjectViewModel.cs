#nullable disable
using Newtonsoft.Json.Linq;
using Reqnroll.VisualStudio.Wizards.Infrastructure;

namespace Reqnroll.VisualStudio.UI.ViewModels;

public class AddNewReqnrollProjectViewModel : INotifyPropertyChanged
{
    private const string MsTest = "MsTest";
    private const string Net8Tag = "net8.0";
    private const string Net8Label = ".NET 8.0";
    private IDictionary<string, string> DotNetFrameworkLabelToTagMap = new Dictionary<string, string>();
    private IDictionary<string, FrameworkInfo> TestFrameworkMetaData = new Dictionary<string, FrameworkInfo>();

    public AddNewReqnrollProjectViewModel() { }
    public AddNewReqnrollProjectViewModel(INewProjectMetaDataProvider metaDataProvider)
    {
        metaDataProvider.RetrieveNewProjectMetaData(
            (NewProjectMetaData md) =>
            {
                DotNetFrameworkLabelToTagMap = md.DotNetFrameworkNameToTagMap;
                DotNetFrameworks = new(md.DotNetFrameworks);
                DotNetFramework = md.DotNetFrameworkDefault;
                TestFrameworks = new(md.TestFrameworks);
                UnitTestFramework = md.TestFrameworkDefault;
                TestFrameworkMetaData = md.TestFrameworkMetaData;
            });
    }
#if DEBUG
    public static AddNewReqnrollProjectViewModel DesignData = new()
    {
        DotNetFramework = Net8Label,
        UnitTestFramework = MsTest,
        FluentAssertionsIncluded = false,
        TestFrameworkMetaData = new Dictionary<string, FrameworkInfo>()
        {
            {
                MsTest, new FrameworkInfo()
                {
                    Label = "MsTest Label",
                    Description = "MsTest description",
                    Url = "https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-csharp-with-mstest",
                    Dependencies = new List<NugetPackageDescriptor>()
                    {
                        new("Reqnroll.MsTest", "2.4.1"),
                        new("MSTest.TestFramework", "3.9.2"),
                    }
                }
            },
            {
                "NUnit", new FrameworkInfo()
                {
                    Label = "NUnit Label",
                    Description = "NUnit description",
                    Url = "https://nunit.org",
                    Dependencies = new List<NugetPackageDescriptor>()
                    {
                        new("Reqnroll.NUnit", "2.4.1"),
                        new("nunit", "3.14.0"),
                    }
                }
            }
        },
        TestFrameworks = new ObservableCollection<string>()
        {
            MsTest, "NUnit"
        },
        DotNetFrameworkLabelToTagMap = new Dictionary<string, string>()
        {
            { Net8Label, Net8Tag}
        }
    };
#endif


    // The tag value of the currently selected .NET framework (ie, "net8.0")
    public string DotNetFrameworkTag
    {
        get
        {
            return DotNetFrameworkLabelToTagMap.Count > 0 ? DotNetFrameworkLabelToTagMap[DotNetFramework] : Net8Tag;
        }
    }
    private string _dotNetFramework = Net8Label;
    private string _unitTestFramework = MsTest;

    #region XAML Bound Properties

    // The currently selected DotNetFramework label (ie, ".NET 8.0")
    public string DotNetFramework
    {
        get => _dotNetFramework;
        set
        {
            _dotNetFramework = value;
            OnPropertyChanged(nameof(TestFrameworks));
        }
    }
    // The list of .NET Frameworks that appear in the combobox for selection by the user
    public ObservableCollection<string> DotNetFrameworks { get; set; } = new(new List<string> { ".NET Framework 4.8.1", ".NET 8.0" });

    // The currently selected Unit Test Framework (ie, "xUnit")
    public string UnitTestFramework
    {
        get => _unitTestFramework;
        set
        {
            _unitTestFramework = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(UnitTestFrameworkDescription));
            OnPropertyChanged(nameof(UnitTestFrameworkUrl));
        }
    }

    // The list of available Unit Test Framework that appear in a combobox for selection by the user
    public ObservableCollection<string> TestFrameworks { get; set; } = new(new List<string> { "MSTest", "NUnit", "xUnit" });

    // A line of text describing the currently selected Unit Test Framework (bound to a Textbox)
    public string UnitTestFrameworkDescription
    {
        get
        {
            if (TestFrameworkMetaData.TryGetValue(_unitTestFramework, out var frameworkInfo))
                return frameworkInfo.Description;
            return "";
        }
    }

    // URL to the home page of the currently selected Unit Test Framework (bound to a TextBox)
    public string UnitTestFrameworkUrl
    {
        get
        {
            if (TestFrameworkMetaData.TryGetValue(_unitTestFramework, out var frameworkInfo))
                return frameworkInfo.Url;
            return "";
        }
    }

    // FluentAssertions suggestion is temporarily hidden from the UI as it is not free for commercial use anymore. 
    // See https://xceed.com/fluent-assertions-faq/
    // Maybe we could consider suggesting https://github.com/shouldly/shouldly instead.
    public bool FluentAssertionsIncluded { get; set; } = false;

    #endregion

    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
