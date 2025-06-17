using Reqnroll.VisualStudio.Wizards.Infrastructure;

namespace Reqnroll.VisualStudio.UI.ViewModels;

public class AddNewReqnrollProjectViewModel : INotifyPropertyChanged
{
#if DEBUG
    private static readonly List<DotNetFrameworkViewModel> DesignDataDotNetFrameworks = new()
    {
        new DotNetFrameworkViewModel("net471", ".NET Framework 4.7.1"),
        new DotNetFrameworkViewModel("net8.0",".NET 8.0"),
    };

    private static readonly List<UnitTestFrameworkViewModel> DesignDataUnitTestFrameworks = new()
    {
        new UnitTestFrameworkViewModel("NUnit", "NUnit", "Use Reqnroll with NUnit", "https://nunit.org"),
        new UnitTestFrameworkViewModel("MsTest","MsTest", "Use Reqnroll with MsTest", "https://github.com/microsoft/testfx?tab=readme-ov-file"),
        new UnitTestFrameworkViewModel("WithoutDetailsKey", "Without Details", null, null),
    };

    public static AddNewReqnrollProjectViewModel DesignData = new()
    {
        DotNetFrameworks = DesignDataDotNetFrameworks,
        DotNetFramework = DesignDataDotNetFrameworks[1],
        UnitTestFrameworks = DesignDataUnitTestFrameworks,
        UnitTestFramework = DesignDataUnitTestFrameworks[1],
    };
#endif

    public class DotNetFrameworkViewModel
    {
        public string Tag { get; set; }
        public string Label { get; set; }

        public DotNetFrameworkViewModel(string tag, string label)
        {
            Tag = tag;
            Label = label;
        }
    }

    public class UnitTestFrameworkViewModel
    {
        public string Tag { get; set; }
        public string Label { get; set; }
        public string? Description { get; set; }
        public string? Url { get; set; }

        public UnitTestFrameworkViewModel(string tag, string label, string? description, string? url)
        {
            Tag = tag;
            Label = label;
            Description = description;
            Url = url;
        }
    }

    public AddNewReqnrollProjectViewModel()
    {
        
    }

    public AddNewReqnrollProjectViewModel(INewProjectMetaDataProvider metaDataProvider)
    {
        _metaDataProvider = metaDataProvider;
        LoadMetadata(_metaDataProvider.GetFallbackMetadata());
    }

    public async Task InitializeAsync()
    {
        if (_metaDataProvider == null)
            return; // design time

        var metadata = await _metaDataProvider.RetrieveNewProjectMetaDataAsync();
        if (!metadata.IsFallback) // we already loaded the fallback
            LoadMetadata(metadata);
    }

    private void LoadMetadata(NewProjectMetaData metadata)
    {
        DotNetFrameworks = metadata.DotNetFrameworksMetadata
                                   .Select(fmd => new DotNetFrameworkViewModel(fmd.Tag, fmd.Label))
                                   .ToList();
        DotNetFramework = DotNetFrameworks.FirstOrDefault(f => f.Tag == metadata.DotNetFrameworkDefault)!;
        UnitTestFrameworks = metadata.TestFrameworkMetaData
                                     .Select(fmd => new UnitTestFrameworkViewModel(fmd.Key, fmd.Value.Label, fmd.Value.Description,fmd.Value.Url))
                                     .ToList();
        UnitTestFramework = UnitTestFrameworks.FirstOrDefault(f => f.Tag == metadata.TestFrameworkDefault)!;
    }

    private readonly INewProjectMetaDataProvider? _metaDataProvider;

    private DotNetFrameworkViewModel _dotNetFramework = new DotNetFrameworkViewModel("net8.0", ".NET 8.0");
    public DotNetFrameworkViewModel DotNetFramework
    {
        get => _dotNetFramework;
        set
        {
            if (value == _dotNetFramework)
            {
                return;
            }

            _dotNetFramework = value;
            OnPropertyChanged();
        }
    }

    private List<DotNetFrameworkViewModel> _dotNetFrameworks = new();
    public List<DotNetFrameworkViewModel> DotNetFrameworks
    {
        get => _dotNetFrameworks;
        set
        {
            if (Equals(value, _dotNetFrameworks))
            {
                return;
            }

            _dotNetFrameworks = value;
            OnPropertyChanged();
        }
    }

    private UnitTestFrameworkViewModel _unitTestFramework = new("MsTest", "MsTest", null, null);
    public UnitTestFrameworkViewModel UnitTestFramework
    {
        get => _unitTestFramework;
        set
        {
            if (Equals(value, _unitTestFramework))
            {
                return;
            }

            _unitTestFramework = value;
            OnPropertyChanged();
        }
    }

    private List<UnitTestFrameworkViewModel> _unitTestFrameworks = new();
    public List<UnitTestFrameworkViewModel> UnitTestFrameworks
    {
        get => _unitTestFrameworks;
        set
        {
            if (Equals(value, _unitTestFrameworks))
            {
                return;
            }

            _unitTestFrameworks = value;
            OnPropertyChanged();
        }
    }

    // FluentAssertions suggestion is temporarily hidden from the UI as it is not free for commercial use anymore. 
    // See https://xceed.com/fluent-assertions-faq/
    // Maybe we could consider suggesting https://github.com/shouldly/shouldly instead.
    public bool FluentAssertionsIncluded { get; set; } = false;

    #region INotifyPropertyChanged implementation
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion
}