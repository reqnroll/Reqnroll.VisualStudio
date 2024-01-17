using System;

namespace Reqnroll.VisualStudio.VsEvents;

public interface IVsSolutionEventListener
{
    event EventHandler<HostOpenedEventArgs> Opened;

    event EventHandler Closing;

    event EventHandler Closed;

    event EventHandler Loaded;

    event EventHandler<OpenProjectEventArgs> AfterOpenProject;

    event EventHandler<LoadProjectEventArgs> AfterLoadProject;

    event EventHandler<HierarchyEventArgs> ProjectRenamed;

    event EventHandler<CloseProjectEventArgs> BeforeCloseProject;
}
