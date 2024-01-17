#nullable disable

namespace Reqnroll.VisualStudio.UI;

public class AsyncContextMenu : IAsyncContextMenu
{
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ContextMenu _contextMenu;
    private readonly ContextMenuManager _contextMenuManager;
    private readonly MenuItem _headerMenuItem;
    private readonly IIdeScope _ideScope;

    private bool _isRedirectCommandAdded;
    private bool _isRedirectedToOutputPane;

    public AsyncContextMenu(ContextMenu contextMenu, IIdeScope ideScope, ContextMenuManager contextMenuManager)
    {
        _contextMenu = contextMenu;
        _ideScope = ideScope;
        _contextMenuManager = contextMenuManager;
        _cancellationTokenSource = new CancellationTokenSource();
        _contextMenu.Closed += OnMenuClosed;
        _headerMenuItem = contextMenu.Items[0] as MenuItem;
        Debug.Assert(_headerMenuItem != null);
        _contextMenuManager.SetMenuItemIcon(_headerMenuItem, "Hourglass");
    }

    public CancellationToken CancellationToken => _cancellationTokenSource.Token;
    public bool IsComplete { get; private set; }

    public void AddItems(params ContextMenuItem[] items)
    {
        RunOnUiThread(() => AddItemsOnUiThread(items));
    }

    public void Complete()
    {
        IsComplete = true;
        _cancellationTokenSource.Dispose();
        RunOnUiThread(() => _contextMenuManager.SetMenuItemIcon(_headerMenuItem, "Reqnroll"));
    }

    private void OnMenuClosed(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_isRedirectedToOutputPane && !IsComplete)
                _cancellationTokenSource.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void RunOnUiThread(Action action)
    {
        if (ThreadHelper.CheckAccess())
            action();
        else
            ThreadHelper.Generic.BeginInvoke(action);
    }

    private void AddItemsOnUiThread(ContextMenuItem[] items)
    {
        foreach (var contextMenuItem in items)
            if (_isRedirectedToOutputPane)
            {
                AddToOutputWindowPane(contextMenuItem);
            }
            else
            {
                if (!_isRedirectCommandAdded && contextMenuItem is SourceLocationContextMenuItem)
                {
                    _isRedirectCommandAdded = true;
                    var redirectMenuItem = new ContextMenuItem("Show results on output window",
                        ShowResultsOnOutputWindow, "OutputPane");
                    _contextMenuManager.AddMenuItem(_contextMenu, redirectMenuItem, 0);
                    _contextMenuManager.AddSeparator(_contextMenu, 1);
                }

                _contextMenuManager.AddMenuItem(_contextMenu, contextMenuItem);
            }
    }

    private void ShowResultsOnOutputWindow(ContextMenuItem _)
    {
        _ideScope.DeveroomOutputPaneServices.WriteLine($"Results for {_headerMenuItem.Header}:");
        foreach (var contextMenuItem in _contextMenuManager.GetContextMenuItems(_contextMenu)
                     .OfType<SourceLocationContextMenuItem>())
            _ideScope.DeveroomOutputPaneServices.WriteLine(contextMenuItem.GetSearchResultLabel());
        _isRedirectedToOutputPane = true;
        _ideScope.DeveroomOutputPaneServices.Activate();
    }

    private void AddToOutputWindowPane(ContextMenuItem contextMenuItem)
    {
        if (contextMenuItem is SourceLocationContextMenuItem sourceLocationContextMenuItem)
            _ideScope.DeveroomOutputPaneServices.WriteLine(sourceLocationContextMenuItem.GetSearchResultLabel());
        else
            _ideScope.DeveroomOutputPaneServices.WriteLine(contextMenuItem.Label);
    }
}
