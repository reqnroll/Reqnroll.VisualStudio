using System;
using System.Linq;

namespace Reqnroll.VisualStudio;

public class SafeDispatcherTimer
{
    private readonly Func<bool> _action;
    private readonly DispatcherTimer _dispatcherTimer;
    private readonly IDeveroomLogger _logger;
    private readonly IMonitoringService _monitoringService;

    private SafeDispatcherTimer(int intervalSeconds, IDeveroomLogger logger, IMonitoringService monitoringService,
        [NotNull] Func<bool> action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _logger = logger;
        _monitoringService = monitoringService;
        _dispatcherTimer = new DispatcherTimer(
            TimeSpan.FromSeconds(intervalSeconds),
            DispatcherPriority.ContextIdle,
            DispatcherTick,
            Dispatcher.CurrentDispatcher);
    }

    public static SafeDispatcherTimer CreateOneTime(int intervalSeconds, IDeveroomLogger logger,
        IMonitoringService monitoringService, [NotNull] Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        return new SafeDispatcherTimer(intervalSeconds, logger, monitoringService, () =>
        {
            action();
            return false;
        });
    }

    public static SafeDispatcherTimer CreateContinuing(int intervalSeconds, IDeveroomLogger logger,
        IMonitoringService monitoringService, [NotNull] Func<bool> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        return new SafeDispatcherTimer(intervalSeconds, logger, monitoringService, action);
    }

    public void Start()
    {
        _dispatcherTimer.Start();
    }

    private void DispatcherTick(object sender, EventArgs e)
    {
        try
        {
            _dispatcherTimer.Stop();
            bool doContinue = _action();
            if (doContinue)
                _dispatcherTimer.Start();
        }
        catch (Exception ex)
        {
            _logger?.LogException(_monitoringService, ex);
            if (_logger == null)
                MessageBox.Show("Unhandled exception: " + ex, "Reqnroll error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
        }
    }
}
