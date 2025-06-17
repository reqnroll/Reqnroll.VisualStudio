using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Reqnroll.VisualStudio.ProjectSystem.Actions;
using Reqnroll.VisualStudio.UI.Dialogs;
using Reqnroll.VisualStudio.UI.ViewModels;
using Reqnroll.VisualStudio.Wizards.Infrastructure;

namespace Reqnroll.VisualStudio.UI.Tester;

/// <summary>
///     Interaction logic for UiTesterWindow.xaml
/// </summary>
public partial class UiTesterWindow : Window
{
    private readonly ContextMenuManager _contextMenuManager = new(UiResourceProvider.Instance);

    public UiTesterWindow()
    {
        InitializeComponent();
    }

    private ContextMenu CreateContextMenu()
    {
        var command = new Action<ContextMenuItem>(item => MessageBox.Show($"hello {item.Label}", "test"));
        var contextMenu = _contextMenuManager.CreateContextMenu("Test context menu",
            new ContextMenuItem("Defined step [regex]", command, "StepDefinitionsDefined"),
            new ContextMenuItem("Invalid defined step [regex]", command, "StepDefinitionsDefinedInvalid"),
            new ContextMenuItem("Ambiguous step [regex]", command, "StepDefinitionsAmbiguous"),
            new ContextMenuItem("Undefined step", command, "StepDefinitionsUndefined"),
            new ContextMenuItem("Before hook", command, "BeforeHook"),
            new ContextMenuItem("After hook", command, "AfterHook")
            );
        return contextMenu;
    }

    private void Test_ContextMenu_DefaultLocation(object sender, RoutedEventArgs e)
    {
        var contextMenu = CreateContextMenu();
        _contextMenuManager.ShowContextMenu(contextMenu);
    }

    private void Test_ContextMenu_At100x100(object sender, RoutedEventArgs e)
    {
        var contextMenu = CreateContextMenu();
        _contextMenuManager.ShowContextMenu(contextMenu, new Point(100, 100));
    }

    private void Test_GenerateStepDefinitions(object sender, RoutedEventArgs e)
    {
        var viewModel = CreateStepDefinitionsDialogViewModel.DesignData;
        var dialog = new CreateStepDefinitionsDialog(viewModel);
        dialog.ShowDialog();

        string resultMessage =
            $"{viewModel.Result}: ClassName={viewModel.ClassName}, Snippets={string.Join(",", viewModel.Items.Select((item, i) => item.IsSelected ? i.ToString() : null).Where(i => i != null))}";

        MessageBox.Show(resultMessage);
    }

    private void Test_ReportError(object sender, RoutedEventArgs e)
    {
        var viewModel = ReportErrorDialogViewModel.DesignData;
        var dialog = new ReportErrorDialog(viewModel);
        dialog.ShowDialog();
    }

    private void Test_WelcomeDialog(object sender, RoutedEventArgs e)
    {
        var viewModel = WelcomeDialogViewModel.DesignData;
        var dialog = new WelcomeDialog(viewModel);
        dialog.ShowDialog();
    }

    private void Test_UpgradeDialog(object sender, RoutedEventArgs e)
    {
        var viewModel = UpgradeDialogViewModel.DesignData;
        var dialog = new WelcomeDialog(viewModel);
        dialog.ShowDialog();
    }

    private void Test_UpgradeDialogRealChangelog(object sender, RoutedEventArgs e)
    {
        var viewModel = UpgradeDialogViewModel.GetDesignDataWithRealChangeLog();
        var dialog = new WelcomeDialog(viewModel);
        dialog.ShowDialog();
    }

    private void Test_ProjectTemplateWizard(object sender, RoutedEventArgs e)
    {
        var viewModel = AddNewReqnrollProjectViewModel.DesignData;
        var dialog = new AddNewReqnrollProjectDialog(viewModel);
        var result = dialog.ShowDialog();
        if (result != true) return;

        string resultMessage =
            $"Chosen {viewModel.DotNetFramework.Tag} with {viewModel.UnitTestFramework.Tag}";

        MessageBox.Show(resultMessage);
    }

    private void Test_ProjectTemplateWizardWithMetadataProvider(object sender, RoutedEventArgs e)
    {
        var viewModel = new AddNewReqnrollProjectViewModel(new NewProjectMetaDataProvider(new HttpClientWrapper(), new EnvironmentWrapper()));
        var dialog = new AddNewReqnrollProjectDialog(viewModel);
        var result = dialog.ShowDialog();
        if (result != true) return;

        string resultMessage =
            $"Chosen {viewModel.DotNetFramework.Tag} with {viewModel.UnitTestFramework.Tag}";

        MessageBox.Show(resultMessage);
    }

    private void Test_RenameStep(object sender, RoutedEventArgs e)
    {
        var viewModel = RenameStepViewModel.DesignData;
        var dialog = new RenameStepDialog(viewModel);
        var result = dialog.ShowDialog();

        if (result == true)
        {
            string resultMessage =
                $"Renamed Step={viewModel.StepText}";

            MessageBox.Show(resultMessage);
        }
    }
}
