using System;
using System.Linq;

namespace Reqnroll.VisualStudio;

public interface IVersionProvider
{
    string GetVsVersion();
    string GetExtensionVersion();
}
