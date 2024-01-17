namespace SpecFlowConnector;

public static class ReflectionExtensions
{
    public static T ReflectionCallMethod<T>(this object obj, string methodName, params object[]? args)
    {
        return ReflectionCallMethod<T>(obj, methodName, args?.Select(a => a.GetType()).ToArray() ?? Type.EmptyTypes,
            args);
    }

    public static void ReflectionCallMethod(this object obj, string methodName, Type[] parameterTypes,
        params object[]? args)
    {
        ReflectionCallMethod<object>(obj, methodName, parameterTypes, args);
    }

    public static T ReflectionCallMethod<T>(this object obj, string methodName, Type[] parameterTypes,
        params object[]? args)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        var objType = obj.GetType();
        var methodInfo = objType.GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, parameterTypes, null);
        if (methodInfo == null)
            throw new ArgumentException($"Cannot find method {methodName} on type {objType.FullName}");
        var invoke = methodInfo.Invoke(obj, args);

        if (invoke is T result) return result;

        if (methodInfo.ReturnType != typeof(void))
            throw new InvalidCastException($"'{invoke?.GetType()}' is not {typeof(T)}");

        return default!;
    }

    public static T ReflectionCallStaticMethod<T>(this Type type, string methodName, Type[] parameterTypes,
        params object[] args)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        var methodInfo = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null, parameterTypes, null);
        if (methodInfo == null)
            throw new ArgumentException($"Cannot find method {methodName} on type {type.FullName}");

        object result = methodInfo.Invoke(null, args) 
                        ?? throw new InvalidOperationException($"Cannot invoke {methodName} on type {type.FullName}");

        return ((T)result) 
               ?? throw new InvalidOperationException($"{result!.GetType()} is not assignable from {typeof(T)}");
    }

    public static bool ReflectionHasProperty(this object obj, string propertyName)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        var propertyInfo = obj.GetType().GetProperty(propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return propertyInfo != null;
    }

    public static T ReflectionGetProperty<T>(this object obj, string propertyName)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        var type = obj.GetType();
        var propertyInfo = type.GetProperty(propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (propertyInfo == null)
            throw new ArgumentException($"Cannot find property {propertyName} on type {type.FullName}");
        object? result = propertyInfo.GetValue(obj);
        return (T)result!;
    }

    public static T ReflectionGetField<T>(this object obj, string fieldName)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        var type = obj.GetType();
        var fieldInfo = type
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (fieldInfo == null)
            throw new ArgumentException($"Cannot find field {fieldName} on type {type.FullName}");
        object? result = fieldInfo.GetValue(obj);
        return (T)result!;
    }
}
