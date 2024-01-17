#nullable disable


namespace Reqnroll.VisualStudio.VsxStubs.ProjectSystem;

public class StubWindowManager : IDeveroomWindowManager
{
    private readonly List<Tuple<Type, Delegate>> _registeredActions = new();
    private readonly Dictionary<Type, object> _showDialogViewModels = new();

    public bool? ShowDialog<TViewModel>(TViewModel viewModel)
    {
        _showDialogViewModels[typeof(TViewModel)] = viewModel;
        foreach (var action in _registeredActions.Where(a => a.Item1 == typeof(TViewModel)).Select(a => a.Item2))
            action.DynamicInvoke(viewModel);
        return true;
    }

    public TViewModel GetShowDialogViewModel<TViewModel>() where TViewModel : class
    {
        if (!_showDialogViewModels.TryGetValue(typeof(TViewModel), out var viewModel))
            return null;

        return (TViewModel) viewModel;
    }

    public void RegisterWindowAction<TViewModel>(Action<TViewModel> action)
    {
        _registeredActions.Add(new Tuple<Type, Delegate>(typeof(TViewModel), action));
    }
}
