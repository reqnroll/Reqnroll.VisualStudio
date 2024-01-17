#nullable disable
// Decompiled with JetBrains decompiler
// Type: Microsoft.VisualStudio.TestWindow.VsHost.BuildCommandIntercepter
// Assembly: Microsoft.VisualStudio.TestWindow.Core, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// MVID: 87C699D5-75FE-4153-9738-59513196DBB5
// Assembly location: C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TestWindow\Microsoft.VisualStudio.TestWindow.Core.dll


using Microsoft.VisualStudio.OLE.Interop;
using IServiceProvider = System.IServiceProvider;

namespace Reqnroll.VisualStudio.VsEvents;

internal sealed class BuildCommandIntercepter : IDisposable, IOleCommandTarget
{
    private static BuildCommandIntercepter instance;
    private readonly Guid programmaticBuildCmdGrp = new("95A56312-F89A-4838-B533-9F0064C4AA63");
    private readonly uint programmaticBuildCmdId = 1;
    private readonly IVsRegisterPriorityCommandTarget registerCommandTarget;
    private uint cookie;
    private EventHandler<BuildCommandEventArgs> userInitiatedBuild;

    private BuildCommandIntercepter(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
        registerCommandTarget =
            serviceProvider.GetService<IVsRegisterPriorityCommandTarget>(typeof(SVsRegisterPriorityCommandTarget));
        ErrorHandler.ThrowOnFailure(registerCommandTarget.RegisterPriorityCommandTarget(0U, this, out cookie));
    }

    public void Dispose()
    {
        if (cookie == 0U || registerCommandTarget == null)
            return;
        ErrorHandler.Succeeded(registerCommandTarget.UnregisterPriorityCommandTarget(cookie));
        cookie = 0U;
    }

    int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
    {
        if (pvaOut == IntPtr.Zero &&
            (IsBuildCommand(pguidCmdGroup, nCmdID) || IsProgrammaticBuildCmd(pguidCmdGroup, nCmdID)))
        {
            EventHandler<BuildCommandEventArgs> userInitiatedBuild = this.userInitiatedBuild;
            if (userInitiatedBuild != null)
            {
                BuildCommandEventArgs e = new BuildCommandEventArgs(IsBuildClean(pguidCmdGroup, nCmdID));
                userInitiatedBuild(this, e);
            }
        }

        return -2147221248;
    }

    int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) =>
        -2147221248;

    public static BuildCommandIntercepter InitializeCommandInterceptor(IServiceProvider serviceProvider)
    {
        if (instance == null)
            instance = new BuildCommandIntercepter(serviceProvider);
        return instance;
    }

    internal event EventHandler<BuildCommandEventArgs> UserInitiatedBuild
    {
        add => userInitiatedBuild += value;
        remove => userInitiatedBuild -= value;
    }

    private static bool IsBuildCommand(Guid commandGroup, uint commandID)
    {
        if (commandGroup == VSConstants.GUID_VSStandardCommandSet97)
            switch (commandID)
            {
                case 882:
                case 883:
                case 885:
                case 886:
                case 887:
                case 889:
                case 892:
                case 893:
                case 895:
                    return true;
            }

        return false;
    }

    public bool IsBuildClean(Guid commandGroup, uint commandID) =>
        commandGroup == VSConstants.GUID_VSStandardCommandSet97 &&
        (commandID == 885U || commandID == 889U || commandID == 895U);

    private bool IsProgrammaticBuildCmd(Guid pguidCmdGroup, uint nCmdID) =>
        pguidCmdGroup.Equals(programmaticBuildCmdGrp) && (int) nCmdID == (int) programmaticBuildCmdId;
}
