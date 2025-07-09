using System.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;
using Reqnroll.VisualStudio.UI.ViewModels;
using System.Windows;
using System.Windows.Navigation;

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

    public AddNewReqnrollProjectDialog(AddNewReqnrollProjectViewModel viewModel, IVsUIShell? vsUiShell = null) :
        base(vsUiShell)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += AddNewReqnrollProjectDialog_LoadedAsync;
    }

    public AddNewReqnrollProjectViewModel? ViewModel { get; }

#pragma warning disable VSTHRD100
    private async void AddNewReqnrollProjectDialog_LoadedAsync(object sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100
    {
        try
        {
            if (ViewModel != null)
                await ViewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex, "Error during AddNewReqnrollProjectDialog_LoadedAsync");
        }
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        OnLinkClicked(sender, e);
    }
}
