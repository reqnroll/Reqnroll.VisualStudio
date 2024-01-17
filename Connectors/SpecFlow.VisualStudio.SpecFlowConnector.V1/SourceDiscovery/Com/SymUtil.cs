#nullable disable
using System.Diagnostics.SymbolStore;

namespace SpecFlow.VisualStudio.SpecFlowConnector.SourceDiscovery.Com;

// Encapsulate a set of helper classes to get a symbol reader from a file.
// The symbol interfaces require an unmanaged metadata interface.
internal static class SymUtil
{
    public static ISymbolReader GetSymbolReaderForFile(string pathModule, string searchPath = null) =>
        GetSymbolReaderForFile(new SymBinder(), pathModule, searchPath);

    public static ISymbolReader GetSymbolReaderForFile(ISymbolBinder1 binder, string pathModule, string searchPath)
    {
        // Guids for imported metadata interfaces.
        Guid dispenserClassID = new Guid(0xe5cb7a31, 0x7512, 0x11d2, 0x89,
            0xce, 0x00, 0x80, 0xc7, 0x92, 0xe5, 0xd8); // CLSID_CorMetaDataDispenser
        Guid importerIID = new Guid(0x7dac8207, 0xd3ae, 0x4c75, 0x9b, 0x67,
            0x92, 0x80, 0x1a, 0x49, 0x7d, 0x44); // IID_IMetaDataImport

        // First create the Metadata dispenser.
        var metaHost = new ClrMetaHost();
        var runtime = metaHost.GetRuntime("v4.0.30319");

        var dispenser = runtime.GetInterface<IMetaDataDispenser>(dispenserClassID);
        dispenser.OpenScope(pathModule, 0, ref importerIID, out var objImporter);

        IntPtr importerPtr = IntPtr.Zero;
        ISymbolReader reader;
        try
        {
            // This will manually AddRef the underlying object, so we need to 
            // be very careful to Release it.
            importerPtr = Marshal.GetComInterfaceForObject(objImporter, typeof(IMetadataImport));
            reader = binder.GetReader(importerPtr, pathModule, searchPath);
        }
        finally
        {
            if (importerPtr != IntPtr.Zero) Marshal.Release(importerPtr);
        }

        return reader;
    }
}

// We can use reflection-only load context to use reflection to query for 
// metadata information rather
// than painfully import the com-classic metadata interfaces.
[Guid("809c652e-7396-11d2-9771-00a0c9b4d50c")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[ComVisible(true)]
internal interface IMetaDataDispenser
{
    // We need to be able to call OpenScope, which is the 2nd vtable slot.
    // Thus we need this one placeholder here to occupy the first slot..
    void DefineScope_Placeholder();

    //STDMETHOD(OpenScope)(                   // Return code.
    //LPCWSTR     szScope,                // [in] The scope to open.
    //  DWORD       dwOpenFlags,            // [in] Open mode flags.
    //  REFIID      riid,                   // [in] The interface desired.
    //  IUnknown    **ppIUnk) PURE;         // [out] Return interface on success.
    void OpenScope([In] [MarshalAs(UnmanagedType.LPWStr)] string szScope,
        [In] int dwOpenFlags, [In] ref Guid riid,
        [Out] [MarshalAs(UnmanagedType.IUnknown)]
        out object punk);

    // Don't need any other methods.
}

// Since we're just blindly passing this interface through managed code to the Symbinder, 
// we don't care about actually importing the specific methods.
// This needs to be public so that we can call Marshal.GetComInterfaceForObject() on 
// it to get the underlying metadata pointer.
[Guid("7DAC8207-D3AE-4c75-9B67-92801A497D44")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[ComVisible(true)]
public interface IMetadataImport
{
    // Just need a single placeholder method so that it doesn't complain
    // about an empty interface.
    void Placeholder();
}
