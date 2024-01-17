using System;
using System.Linq;

namespace Reqnroll.VisualStudio.ProjectSystem;

public interface IDeveroomWindowManager
{
    bool? ShowDialog<TViewModel>(TViewModel viewModel);
}
