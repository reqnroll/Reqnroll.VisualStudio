using System;

namespace Reqnroll.VisualStudio.ProjectSystem.Configuration;

public interface IDeveroomConfigurationProvider
{
    event EventHandler<EventArgs> WeakConfigurationChanged;
    DeveroomConfiguration GetConfiguration();
}
