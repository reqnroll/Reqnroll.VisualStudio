#nullable disable
namespace Reqnroll.VisualStudio.UI.ViewModels.WizardDialogs;

public class WizardPageViewModel : INotifyPropertyChanged
{
    private bool _isActive;

    public WizardPageViewModel(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (value == _isActive) return;
            _isActive = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
