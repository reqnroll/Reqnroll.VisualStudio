#nullable disable
using System;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.Shell.Interop;
using Reqnroll.VisualStudio.UI.ViewModels;

namespace Reqnroll.VisualStudio.UI.Dialogs;

public partial class RenameStepDialog
{
    public RenameStepDialog()
    {
        InitializeComponent();
    }

    public RenameStepDialog(RenameStepViewModel viewModel, IVsUIShell vsUiShell = null) : base(vsUiShell)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public RenameStepViewModel ViewModel { get; }

    private void Rename_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
