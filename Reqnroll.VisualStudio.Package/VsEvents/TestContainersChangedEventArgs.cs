using System;

namespace Reqnroll.VisualStudio.VsEvents;

public class TestContainersChangedEventArgs : EventArgs
{
    public TestContainersChangedEventArgs(bool testContainerUpdateSucceeded, bool testContainerUpdateCanceled)
    {
        Succeeded = testContainerUpdateSucceeded;
        Canceled = testContainerUpdateCanceled;
    }

    public bool Succeeded { get; }

    public bool Canceled { get; }
}
