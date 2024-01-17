#nullable disable
using System;
using System.Linq;
using Microsoft.VisualStudio.Shell.Interop;
using Reqnroll.VisualStudio.UI.ViewModels.WizardDialogs;

namespace Reqnroll.VisualStudio.UI.Dialogs;

public partial class WelcomeDialog
{
    public WelcomeDialog()
    {
        InitializeComponent();
    }

    public WelcomeDialog(WizardViewModel viewModel, IVsUIShell vsUiShell = null) : base(vsUiShell)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public WizardViewModel ViewModel { get; }
}
