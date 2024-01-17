namespace Reqnroll.VisualStudio.UI.ViewModels.WizardDialogs;

public class WizardViewModel : INotifyPropertyChanged
{
    public WizardViewModel(string finishButtonLabel, string dialogTitle, params WizardPageViewModel[] pages)
    {
        VisitedPages = new HashSet<WizardPageViewModel>();
        FinishButtonLabel = finishButtonLabel;
        DialogTitle = dialogTitle;
        NextCommand = new DelegateCommand(_ => MovePageBy(1), _ => CanMovePageBy(1), null);
        PreviousCommand = new DelegateCommand(_ => MovePageBy(-1), _ => CanMovePageBy(-1), null);
        Pages = new ObservableCollection<WizardPageViewModel>();
        if (pages != null)
            foreach (var page in pages)
                Pages.Add(page);
        if (Pages.Count > 0)
            MoveToPage(0);
    }

    public string DialogTitle { get; }
    public string FinishButtonLabel { get; }
    public ObservableCollection<WizardPageViewModel> Pages { get; }
    public HashSet<WizardPageViewModel> VisitedPages { get; }

    public ICommand PreviousCommand { get; }
    public ICommand NextCommand { get; }

    public WizardPageViewModel ActivePage => Pages.FirstOrDefault(p => p.IsActive);

    public int ActivePageIndex
    {
        get
        {
            var activePage = ActivePage;
            return activePage == null ? 0 : Pages.IndexOf(activePage);
        }
    }

    public bool IsOnLastPage => ActivePageIndex == Pages.Count - 1;

    [CanBeNull] public event PropertyChangedEventHandler PropertyChanged = null!;

    private bool CanMovePageBy(int step)
    {
        int activePageIndex = ActivePageIndex;
        int newPageIndex = activePageIndex + step;
        return newPageIndex >= 0 && newPageIndex < Pages.Count;
    }

    private void MovePageBy(int step)
    {
        int activePageIndex = ActivePageIndex;
        int newPageIndex = activePageIndex + step;
        MoveToPage(newPageIndex, activePageIndex);
    }

    private void MoveToPage(int newPageIndex, int activePageIndex = -1)
    {
        if (activePageIndex < 0)
            activePageIndex = ActivePageIndex;
        if (newPageIndex < 0 || newPageIndex >= Pages.Count)
            return;
        Pages[activePageIndex].IsActive = false;
        var newPage = Pages[newPageIndex];
        newPage.IsActive = true;
        VisitedPages.Add(newPage);

        OnPropertyChanged(nameof(ActivePage));
        OnPropertyChanged(nameof(ActivePageIndex));
        OnPropertyChanged(nameof(IsOnLastPage));
    }

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "?")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
