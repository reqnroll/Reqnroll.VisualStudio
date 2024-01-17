#nullable disable

namespace Reqnroll.VisualStudio.ProjectSystem.Actions;

public class ContextMenuItem
{
    public ContextMenuItem(string label, Action<ContextMenuItem> command = null, string icon = null)
    {
        Label = label;
        Command = command;
        Icon = icon;
    }

    public string Label { get; }
    public Action<ContextMenuItem> Command { get; }
    public string Icon { get; set; }
}
