namespace Reqnroll.VisualStudio.UI.ViewModels;

public class ReportErrorDialogViewModel
{
    internal const string ERROR_SUFFIX_TEMPLATE = @"

            Please use the '*Copy error to clipboard*' button to copy the following error details to the clipboard. 
            The error has been also saved to the log file at [%LOCALAPPDATA%\Reqnroll](file://{logFilePath}).
        ";

    internal const string GENERAL_ERROR_SUFFIX = @"
            
            This issue causes instability or blocks important features such as navigation or auto-complete.
            
            *Please help us and other Reqnroll users* by reporting this issue in our issue tracker at 
            https://github.com/reqnroll/Reqnroll.VS/issues.
        ";

    internal const string INIT_ERROR = @"
            Reqnroll Visual Studio Extension detected an issue during initialization. Please try updating your Visual Studio to the latest
            version. (The version of your Viusal Studio can be found in the '*Help / About*' dialog.) 

            If the problem persists even after updating Visual Studio, please report the error above in our issue tracker at 
            https://github.com/reqnroll/Reqnroll.VS/issues.
        ";

#if DEBUG
    public static ReportErrorDialogViewModel DesignData = new()
    {
        Message = INIT_ERROR + ERROR_SUFFIX_TEMPLATE.Replace("{logFilePath}", AsynchronousFileLogger.GetLogFile()),
        ErrorInfo = @"Error hash: {554A5919-12BC-4AAC-AE3B-E1C77DD98540}
A MEF Component threw an exception at runtime: Microsoft.VisualStudio.Composition.CompositionFailedException: An exception was thrown while initializing part ""Reqnroll.VisualStudio.IdeScope.VsProjectSystem"". 
---> System.TypeInitializationException: The type initializer for 'Reqnroll.VisualStudio.EventTracking.GoogleAnalyticsApi' threw an exception. 
---> System.IO.FileNotFoundException: Could not load file or assembly 'System.Net.Http, Version=4.2.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' or one of its dependencies. The system cannot find the file specified. 
at Reqnroll.VisualStudio.EventTracking.GoogleAnalyticsApi..cctor() 
--- End of inner exception stack trace --- 
at Reqnroll.VisualStudio.EventTracking.MonitoringService.TrackEvent(String category, String action, String label, Nullable`1 value) 
at Reqnroll.VisualStudio.EventTracking.MonitoringService.TrackOpenProjectSystem(String vsVersion) 
at Reqnroll.VisualStudio.IdeScope.VsProjectSystem..ctor(IServiceProvider serviceProvider, IVsPackageInstallerServices vsPackageInstallerServices, IVsSolutionEventListener solutionEventListener) in W:\SpecF\Reqnroll.VisualStudio\Reqnroll.VisualStudio.Package\IdeScope\VsProjectSystem.cs:line 62 
--- End of inner exception stack trace --- 
at Microsoft.VisualStudio.Composition.RuntimeExportProviderFactory.RuntimeExportProvider.RuntimePartLifecycleTracker.CreateValue() 
at Microsoft.VisualStudio.Composition.ExportProvider.PartLifecycleTracker.Create() 
at Microsoft.VisualStudio.Composition.ExportProvider.PartLifecycleTracker.MoveNext(PartLifecycleState nextState) 
at Microsoft.VisualStudio.Composition.ExportProvider.PartLifecycleTracker.MoveToState(PartLifecycleState requiredState) 
at Microsoft.VisualStudio.Composition.ExportProvider.PartLifecycleTracker.GetValueReadyToExpose() 
at Microsoft.VisualStudio.Composition.RuntimeExportProviderFactory.RuntimeExportProvider.<>c__DisplayClass15_0.<GetExportedValueHelper>b__0()"
    };
#endif

    public string Message { get; set; } = string.Empty;
    public string ErrorInfo { get; set; } = string.Empty;
    public bool DoNotShowThisErrorAgain { get; set; }
    public Action<ReportErrorDialogViewModel> CopyErrorToClipboardCommand { get; set; } = _ => { };

    public void CopyErrorToClipboard()
    {
        CopyErrorToClipboardCommand?.Invoke(this);
    }
}
