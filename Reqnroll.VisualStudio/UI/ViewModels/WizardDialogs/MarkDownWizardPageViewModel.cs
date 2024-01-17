#nullable disable
using System;
using System.Linq;

namespace Reqnroll.VisualStudio.UI.ViewModels.WizardDialogs;

public class MarkDownWizardPageViewModel : WizardPageViewModel
{
    public MarkDownWizardPageViewModel(string name) : base(name)
    {
    }

    public string Text { get; set; }
}
