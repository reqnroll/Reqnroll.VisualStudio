using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable once CheckNamespace
namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery;

internal class FakeTestContext : TestContext
{
    public override IDictionary Properties => throw new NotImplementedException();

    public override DataRow DataRow => throw new NotImplementedException();

    public override DbConnection DataConnection => throw new NotImplementedException();

    public override void AddResultFile(string fileName)
    {
        throw new NotImplementedException();
    }

    public override void Write(string? message)
    {
        throw new NotImplementedException();
    }

    public override void Write(string format, params object?[] args)
    {
        throw new NotImplementedException();
    }

    public override void WriteLine(string? message)
    {
        throw new NotImplementedException();
    }

    public override void WriteLine(string format, params object?[] args)
    {
        throw new NotImplementedException();
    }
}
