#nullable disable
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Reqnroll.VisualStudio.Monitoring;
using Reqnroll.VisualStudio.ProjectSystem;
using Reqnroll.VisualStudio.UI.Dialogs;
using Reqnroll.VisualStudio.UI.ViewModels;

namespace Reqnroll.VisualStudio.UI;

//https://stackoverflow.com/questions/40608094/wpf-modal-window-in-visual-studio-extension-blocking-input
//https://stackoverflow.com/questions/38057256/wpf-dialog-blocking-input-after-closing?rq=1
[Export(typeof(IDeveroomWindowManager))]
public class DeveroomWindowManager : IDeveroomWindowManager
{
    protected readonly IMonitoringService _monitoringService;
    protected readonly IVsUIShell _vsUiShell;

    [ImportingConstructor]
    public DeveroomWindowManager([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
        IMonitoringService monitoringService)
    {
        _vsUiShell = (IVsUIShell) serviceProvider.GetService(typeof(SVsUIShell));
        _monitoringService = monitoringService;
    }

    public bool? ShowDialog<TViewModel>(TViewModel viewModel)
    {
        var window = CreateWindow(viewModel);
        return window.ShowModal();
    }

    protected virtual DialogWindow CreateWindow<TViewModel>(TViewModel viewModel)
    {
        var dialogWindow = CreateWindowInternal(viewModel);
        dialogWindow.LinkClicked += OnLinkClicked;
        return dialogWindow;
    }

    private void OnLinkClicked(object sender, RequestNavigateEventArgs e)
    {
        var uriString = e.Uri.ToString();
        if (!uriString.StartsWith("file")) _monitoringService.MonitorLinkClicked(GetViewModelName(sender), uriString);
    }

    private string GetViewModelName(object sender)
    {
        if (sender is Window window)
        {
            var viewModel = "ViewModel";
            var name = window.DataContext.GetType().Name; //"UpgradeDialogViewModel"
            var index = name.IndexOf(viewModel, StringComparison.InvariantCultureIgnoreCase);
            if (index > -1)
            {
                var val = name.Substring(0, index);
                return val;
            }

            return name;
        }

        return null;
    }

    private DialogWindow CreateWindowInternal<TViewModel>(TViewModel viewModel)
    {
        if ((object) viewModel is CreateStepDefinitionsDialogViewModel createStepDefinitionsDialogViewModel)
            return new CreateStepDefinitionsDialog(createStepDefinitionsDialogViewModel, _vsUiShell);

        if ((object) viewModel is ReportErrorDialogViewModel reportErrorDialogViewModel)
            return new ReportErrorDialog(reportErrorDialogViewModel, _vsUiShell);

        if ((object) viewModel is WelcomeDialogViewModel welcomeDialogViewModel)
            return new WelcomeDialog(welcomeDialogViewModel, _vsUiShell);

        if ((object) viewModel is UpgradeDialogViewModel upgradeDialogViewModel)
            return new WelcomeDialog(upgradeDialogViewModel, _vsUiShell);

        if ((object) viewModel is AddNewReqnrollProjectViewModel addNewReqnrollProjectViewModel)
            return new AddNewReqnrollProjectDialog(addNewReqnrollProjectViewModel, _vsUiShell);

        if ((object) viewModel is RenameStepViewModel renameStepViewModel)
            return new RenameStepDialog(renameStepViewModel, _vsUiShell);

        throw new NotSupportedException(typeof(TViewModel).ToString());
    }
}
