using System;

namespace Reqnroll.VisualStudio.Specs.Support;

/// <summary>
///     Simple implementation of a busy-waiting strategy that waits for an assertion to succeed for a certain time.
/// </summary>
public static class Wait
{
    private const int ACTIVE_WAIT_TIMEOUT_MSEC = 5000;
    private const int ACTIVE_WAIT_POLL_PERIOD_MSEC = 100;

    public static void For(Action action, int waitTimeoutMsec = ACTIVE_WAIT_TIMEOUT_MSEC,
        int pollPeriodMsec = ACTIVE_WAIT_POLL_PERIOD_MSEC)
    {
        if (Debugger.IsAttached)
            waitTimeoutMsec = 60000;
        var waitUntil = DateTime.Now + TimeSpan.FromMilliseconds(waitTimeoutMsec);
        while (true)
        {
            try
            {
                action();
                return;
            }
            catch (Exception)
            {
                if (DateTime.Now >= waitUntil)
                    throw;
            }

            Thread.Sleep(pollPeriodMsec);
        }
    }
}
