namespace SpecFlow.VisualStudio.SpecFlowConnector.SourceDiscovery.Com;

// Wrapper for ICLRRuntimeInfo.  Represents information about a CLR install instance.
internal class ClrRuntimeInfo
{
    private readonly ICLRRuntimeInfo _runtimeInfo;

    public ClrRuntimeInfo(object clrRuntimeInfo)
    {
        _runtimeInfo = (ICLRRuntimeInfo) clrRuntimeInfo;
    }

    public TInterface GetInterface<TInterface>(Guid clsId)
    {
        Guid ifaceId = typeof(TInterface).GUID;
        return (TInterface) _runtimeInfo.GetInterface(ref clsId, ref ifaceId);
    }
}

// Details about this interface are in metahost.idl.
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("BD39D1D2-BA2F-486A-89B0-B4B0CB466891")]
internal interface ICLRRuntimeInfo
{
    // Marshalling pcchBuffer as int even though it's unsigned. Max version string is 24 characters, so we should not need to go over 2 billion soon.
    void GetVersionString([Out] [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwzBuffer,
        [In] [Out] [MarshalAs(UnmanagedType.U4)]
        ref int pcchBuffer);

    // Marshalling pcchBuffer as int even though it's unsigned. MAX_PATH is 260, unicode paths are 65535, so we should not need to go over 2 billion soon.
    [PreserveSig]
    int GetRuntimeDirectory([Out] [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwzBuffer,
        [In] [Out] [MarshalAs(UnmanagedType.U4)]
        ref int pcchBuffer);

    int IsLoaded([In] IntPtr hndProcess);

    // Marshal pcchBuffer as int even though it's unsigned. Error strings approaching 2 billion characters are currently unheard-of.
    [LCIDConversion(3)]
    void LoadErrorString([In] [MarshalAs(UnmanagedType.U4)] int iResourceId,
        [Out] [MarshalAs(UnmanagedType.LPWStr)]
        StringBuilder pwzBuffer,
        [In] [Out] [MarshalAs(UnmanagedType.U4)]
        ref int pcchBuffer,
        [In] int iLocaleId);

    IntPtr LoadLibrary([In] [MarshalAs(UnmanagedType.LPWStr)] string pwzDllName);

    IntPtr GetProcAddress([In] [MarshalAs(UnmanagedType.LPStr)] string pszProcName);

    [return: MarshalAs(UnmanagedType.IUnknown)]
    object GetInterface([In] ref Guid rclsid, [In] ref Guid riid);
}
