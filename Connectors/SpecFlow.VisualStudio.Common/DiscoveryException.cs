using System;

namespace SpecFlow.VisualStudio.Common;

public class DiscoveryException : Exception
{
    public DiscoveryException()
    {
    }

    public DiscoveryException(string message) : base(message)
    {
    }

    public DiscoveryException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
