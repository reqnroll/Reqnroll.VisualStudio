using System;
using System.Linq;
using Microsoft.VisualStudio.Platform.VSEditor;
using Microsoft.VisualStudio.Text.Internal.Language;

namespace Reqnroll.VisualStudio.VsxStubs;

public class RuntimeDependencyLock
{
    internal class MicrosoftVisualStudioPlatformVsEditor
    {
        private static void Dummy()
        {
            Noop(typeof(SnapshotSpanEventArgsHelper));
        }

        private static void Noop(Type _)
        {
        }
    }

    internal class MicrosoftVisualStudioTextInternal
    {
        private static void Dummy()
        {
            Noop(typeof(CompletionPresenterStylePrivate));
        }

        private static void Noop(Type _)
        {
        }
    }
}
