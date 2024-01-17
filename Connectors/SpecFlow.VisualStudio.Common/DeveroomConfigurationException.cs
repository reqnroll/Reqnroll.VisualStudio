using System;

namespace SpecFlow.VisualStudio.Common;

public class DeveroomConfigurationException : Exception
{
    public DeveroomConfigurationException()
    {
    }

    public DeveroomConfigurationException(string message) : base(message)
    {
    }

    public DeveroomConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
