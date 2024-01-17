// ReSharper disable once CheckNamespace

namespace System;

public static class SystemExtensions
{
    public static Exception WithStackTrace(this Exception exception)
    {
        try
        {
            throw exception;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
