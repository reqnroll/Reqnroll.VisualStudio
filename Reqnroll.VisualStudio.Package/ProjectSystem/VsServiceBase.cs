using System;

namespace Reqnroll.VisualStudio.ProjectSystem;

public abstract class VsServiceBase
{
    private readonly Dispatcher _dispatcher;
    protected readonly IVsIdeScope _vsIdeScope;

    protected VsServiceBase(IVsIdeScope vsIdeScope)
    {
        _vsIdeScope = vsIdeScope;
        _dispatcher = Dispatcher.CurrentDispatcher;
    }

    protected void RunOnUiThread(Action action)
    {
        if (ThreadHelper.CheckAccess())
        {
            action();
        }
        else
        {
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
            _dispatcher.BeginInvoke(action, DispatcherPriority.ContextIdle);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs
        }
    }
}
