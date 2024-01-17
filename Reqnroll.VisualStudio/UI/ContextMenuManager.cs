namespace Reqnroll.VisualStudio.UI;

public class ContextMenuManager
{
    private readonly IUiResourceProvider _uiResourceProvider;

    public ContextMenuManager(IUiResourceProvider uiResourceProvider)
    {
        _uiResourceProvider = uiResourceProvider;
    }

    public ContextMenu CreateContextMenu(string header, params ContextMenuItem[] contextMenuItems)
    {
        var contextMenu = new ContextMenu();
        var headerMenuItem = new MenuItem
        {
            Header = header,
            FontWeight = FontWeights.Bold,
            IsEnabled = false
        };
        SetMenuItemIcon(headerMenuItem, "Reqnroll");
        contextMenu.Items.Add(headerMenuItem);
        AddSeparator(contextMenu);

        foreach (var contextMenuItem in contextMenuItems) AddMenuItem(contextMenu, contextMenuItem);
        return contextMenu;
    }

    public void SetMenuItemIcon(MenuItem menuItem, string iconName)
    {
        menuItem.Icon = iconName == null
            ? null
            : _uiResourceProvider.GetIcon(iconName);
    }

    public void AddSeparator(ContextMenu contextMenu, int? position = null)
    {
        var separator = new Separator();
        if (position == null)
            contextMenu.Items.Add(separator);
        else
            contextMenu.Items.Insert(position.Value + 2, separator);
    }

    public void AddMenuItem(ContextMenu contextMenu, ContextMenuItem contextMenuItem, int? position = null)
    {
        var menuItem = new MenuItem
        {
            Header = contextMenuItem.Label,
            Command = contextMenuItem.Command == null
                ? null
                : new DelegateCommand(() => contextMenuItem.Command(contextMenuItem), () => true, null),
            IsEnabled = contextMenuItem.Command != null,
            FontStyle = contextMenuItem.Command != null ? FontStyles.Normal : FontStyles.Italic,
            Tag = contextMenuItem
        };
        SetMenuItemIcon(menuItem, contextMenuItem.Icon);
        if (position == null)
            contextMenu.Items.Add(menuItem);
        else
            contextMenu.Items.Insert(position.Value + 2, menuItem);
    }

    public IEnumerable<ContextMenuItem> GetContextMenuItems(ContextMenu contextMenu)
    {
        return contextMenu.Items.OfType<MenuItem>().Select(mi => mi.Tag).OfType<ContextMenuItem>();
    }

    public void ShowContextMenu(ContextMenu contextMenu)
    {
        contextMenu.IsOpen = true;
    }

    public void ShowContextMenu(ContextMenu contextMenu, Point position)
    {
        contextMenu.Placement = PlacementMode.Absolute;
        contextMenu.HorizontalOffset = position.X;
        contextMenu.VerticalOffset = position.Y;
        contextMenu.IsOpen = true;
    }
}
