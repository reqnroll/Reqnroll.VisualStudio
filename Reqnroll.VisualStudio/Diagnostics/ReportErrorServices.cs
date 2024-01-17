namespace Reqnroll.VisualStudio.Diagnostics;

public static class ReportErrorServices
{
    private static readonly List<string> HiddenErrors = new();

    public static void ReportInitError(IIdeScope ideScope, Exception exception)
    {
        ideScope.Logger.LogException(ideScope.MonitoringService, exception, "Initialization error");
        ReportError(ideScope, ReportErrorDialogViewModel.INIT_ERROR, exception.ToString());
    }

    public static void ReportGeneralError(IIdeScope ideScope, string message, Exception exception)
    {
        ideScope.Logger.LogException(ideScope.MonitoringService, exception, message);
        var errorMessage = $"**{message}**{ReportErrorDialogViewModel.GENERAL_ERROR_SUFFIX}";
        ReportError(ideScope, errorMessage, exception.ToString());
    }

    private static void ReportError(this IIdeScope ideScope, string message, string errorDetails)
    {
        var hash = Math.Abs(errorDetails.GetHashCode()).ToString();
        if (HiddenErrors.Contains(hash))
        {
            ideScope.Logger.LogVerbose($"Error hidden: {hash}");
            return;
        }

        message = message +
                  ReportErrorDialogViewModel.ERROR_SUFFIX_TEMPLATE.Replace("{logFilePath}",
                      AsynchronousFileLogger.GetLogFile());

        var errorInfo = $"Error hash: {hash}{Environment.NewLine}{errorDetails}";

        var viewModel = new ReportErrorDialogViewModel
        {
            Message = message,
            ErrorInfo = errorInfo,
            CopyErrorToClipboardCommand = vm =>
            {
                ideScope.Logger.LogVerbose($"Copy to clipboard: {vm.ErrorInfo}");
                ideScope.Actions.SetClipboardText(vm.ErrorInfo);
            }
        };

        ideScope.WindowManager.ShowDialog(viewModel);

        if (viewModel.DoNotShowThisErrorAgain) HiddenErrors.Add(hash);
    }
}
