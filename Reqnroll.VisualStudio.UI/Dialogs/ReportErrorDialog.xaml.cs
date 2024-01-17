#nullable disable
using System;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.Shell.Interop;
using Reqnroll.VisualStudio.UI.ViewModels;

namespace Reqnroll.VisualStudio.UI.Dialogs;

public partial class ReportErrorDialog
{
    public ReportErrorDialog()
    {
        InitializeComponent();
    }

    public ReportErrorDialog(ReportErrorDialogViewModel viewModel, IVsUIShell vsUiShell = null) : base(vsUiShell)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public ReportErrorDialogViewModel ViewModel { get; }

    private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CopyErrorToClipboard();
    }
}
