namespace Reqnroll.VisualStudio.ReqnrollConnector.SourceDiscovery.Com;

// Wrapper for ICLRMetaHost.  Used to find information about runtimes.
internal class ClrMetaHost
{
    private static readonly Guid ClsidClrMetaHost = new("9280188D-0E8E-4867-B30C-7FA83884E8DE");
    private readonly ICLRMetaHost _metaHost;

    public ClrMetaHost()
    {
        Guid ifaceId = typeof(ICLRMetaHost).GUID;
        Guid clsid = ClsidClrMetaHost;
        NativeMethods.CLRCreateInstance(ref clsid, ref ifaceId, out var o);
        _metaHost = (ICLRMetaHost) o;
    }

    public ClrRuntimeInfo GetRuntime(string version)
    {
        Guid ifaceId = typeof(ICLRRuntimeInfo).GUID;
        return new ClrRuntimeInfo(_metaHost.GetRuntime(version, ref ifaceId));
    }

    private static class NativeMethods
    {
        [DllImport("mscoree.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void CLRCreateInstance(ref Guid clsid, ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out object metahostInterface);
    }
}

// You're expected to get this interface from mscoree!GetCLRMetaHost.
// Details for APIs are in metahost.idl.
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("D332DB9E-B9B3-4125-8207-A14884F53216")]
internal interface ICLRMetaHost
{
    [return: MarshalAs(UnmanagedType.Interface)]
    object GetRuntime(
        [In] [MarshalAs(UnmanagedType.LPWStr)] string pwzVersion,
        [In] ref Guid riid /*must use typeof(ICLRRuntimeInfo).GUID*/);
}
