using System.Reflection;

namespace ReqnrollConnector.Utils;

public static class ReflectionExtensions
{
    public static T ReflectionCallStaticMethod<T>(this Type type, string methodName, Type[] parameterTypes,
        params object?[] args) where T : class
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        var methodInfo = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null, parameterTypes, null);
        if (methodInfo == null)
            throw new ArgumentException($"Cannot find method {methodName} on type {type.FullName}");

        object result = methodInfo.Invoke(null, args) 
                        ?? throw new InvalidOperationException($"Cannot invoke {methodName} on type {type.FullName}");

        return result as T
               ?? throw new InvalidOperationException($"{result.GetType()} is not assignable from {typeof(T)}");
    }
}
