#nullable disable
using System.Security;

namespace Reqnroll.VisualStudio.ReqnrollConnector;

public class RemoteContextObject : MarshalByRefObject
{
    [SecurityCritical]
    public sealed override object InitializeLifetimeService() => null;
}
