#nullable disable
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell.Interop;
using Reqnroll.VisualStudio.UI.ViewModels;

namespace Reqnroll.VisualStudio.UI.Dialogs;

/// <summary>
///     Interaction logic for AddNewReqnrollProjectDialog.xaml
/// </summary>
public partial class AddNewReqnrollProjectDialog
{
    public AddNewReqnrollProjectDialog()
    {
        InitializeComponent();
    }

    public AddNewReqnrollProjectDialog(AddNewReqnrollProjectViewModel viewModel, IVsUIShell vsUiShell = null) :
        base(vsUiShell)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public AddNewReqnrollProjectViewModel ViewModel { get; }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void TestFramework_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0) return;
        ViewModel.UnitTestFramework = e.AddedItems[0].ToString();
        e.Handled = true;
    }
}
