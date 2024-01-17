using System;
using System.Linq;

namespace Reqnroll.VisualStudio.Discovery;

public struct MatchedStepTextParameter
{
    public int Index;
    public int Length;

    public MatchedStepTextParameter(int index, int length)
    {
        Index = index;
        Length = length;
    }
}
