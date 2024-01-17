namespace Reqnroll.VisualStudio.Common;

public class DebuggableCancellationTokenSource : CancellationTokenSource
{
    /// <summary>
    ///     Do not forget to Dispose!
    /// </summary>
    /// <param name="nonDebuggerTimeout"></param>
    public DebuggableCancellationTokenSource(TimeSpan nonDebuggerTimeout)
        : base(GetDebuggerTimeout(nonDebuggerTimeout))
    {
    }

    public static TimeSpan GetDebuggerTimeout(TimeSpan nonDebuggerTimeout)
        => Debugger.IsAttached ? TimeSpan.FromMinutes(1) : nonDebuggerTimeout;
}
