#nullable enable

namespace Reqnroll.VisualStudio.ProjectSystem.Settings;

public interface IProjectSettingsProvider
{
    event EventHandler<EventArgs> WeakSettingsInitialized;
    event EventHandler<EventArgs> SettingsInitialized;

    ProjectSettings GetProjectSettings();
    ProjectSettings CheckProjectSettings();
}
