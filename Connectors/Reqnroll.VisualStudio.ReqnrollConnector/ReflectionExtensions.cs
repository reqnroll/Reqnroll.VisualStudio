#nullable disable
namespace Reqnroll.VisualStudio.ReqnrollConnector;

public static class ReflectionExtensions
{
    public static T ReflectionCallMethod<T>(this object obj, string methodName, params object[] args)
    {
        return ReflectionCallMethod<T>(obj, methodName, args?.Select(a => a.GetType()).ToArray() ?? new Type[0], args);
    }

    public static void ReflectionCallMethod(this object obj, string methodName, params object[] args)
    {
        ReflectionCallMethod<object>(obj, methodName, args);
    }

    public static void ReflectionCallMethod(this object obj, string methodName, Type[] parameterTypes,
        params object[] args)
    {
        ReflectionCallMethod<object>(obj, methodName, parameterTypes, args);
    }

    public static T ReflectionCallMethod<T>(this object obj, string methodName, Type[] parameterTypes,
        params object[] args)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        var objType = obj.GetType();
        var methodInfo = objType.GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, parameterTypes, null);
        if (methodInfo == null)
            throw new ArgumentException($"Cannot find method {methodName} on type {objType.FullName}");
        return (T) methodInfo.Invoke(obj, args);
    }

    public static T ReflectionCallStaticMethod<T>(this Type type, string methodName, Type[] parameterTypes,
        params object[] args)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        var methodInfo = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null, parameterTypes, null);
        if (methodInfo == null)
            throw new ArgumentException($"Cannot find method {methodName} on type {type.FullName}");
        return (T) methodInfo.Invoke(null, args);
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
        var propertyInfo = obj.GetType().GetProperty(propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (propertyInfo == null)
            throw new ArgumentException($"Cannot find property {propertyName} on type {obj.GetType().FullName}");
        return (T) propertyInfo.GetValue(obj);
    }

    public static T ReflectionGetField<T>(this object obj, string fieldName)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        var fieldInfo = obj.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (fieldInfo == null)
            throw new ArgumentException($"Cannot find field {fieldName} on type {obj.GetType().FullName}");
        return (T) fieldInfo.GetValue(obj);
    }

    public static void ReflectionSetField<T>(this object obj, string fieldName, T value)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        var fieldInfo = obj.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (fieldInfo == null)
            throw new ArgumentException($"Cannot find field {fieldName} on type {obj.GetType().FullName}");
        fieldInfo.SetValue(obj, value);
    }

    public static T ReflectionGetStaticField<T>(this Type type, string fieldName)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        var fieldInfo = type.GetField(fieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (fieldInfo == null)
            throw new ArgumentException($"Cannot find field {fieldName} on type {type.FullName}");
        return (T) fieldInfo.GetValue(null);
    }

    public static T ReflectionGetStaticProperty<T>(this Type type, string propertyName)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        var propertyInfo =
            type.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (propertyInfo == null)
            throw new ArgumentException($"Cannot find property {propertyName} on type {type.FullName}");
        return (T) propertyInfo.GetValue(null);
    }

    public static T ReflectionCreateInstance<T>(this Type type, Type[] parameterTypes, params object[] args)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        var constructorInfo = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, parameterTypes, null);
        if (constructorInfo == null)
            throw new ArgumentException($"Cannot find constructor on type {type.FullName}");
        return (T) constructorInfo.Invoke(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance, null,
            args, null);
    }

    //BoDi
    public static bool ReflectionIsRegistered<T>(this object container, string name)
    {
        return container.ReflectionCallMethod<bool>(nameof(IObjectContainer.IsRegistered),
            new[] {typeof(string)}, name);
    }

    public static T ReflectionResolve<T>(this object container)
    {
        return container.ReflectionCallMethod<T>(nameof(IObjectContainer.Resolve),
            new[] {typeof(Type), typeof(string)},
            typeof(T), null);
    }

    public static void ReflectionRegisterTypeAs<TType, TInterface>(this object container)
        where TType : class, TInterface
    {
        ReflectionRegisterTypeAs(container, typeof(TType), typeof(TInterface));
    }

    public static void ReflectionRegisterTypeAs(this object container, Type implementationType, Type interfaceType)
    {
        container.ReflectionCallMethod(nameof(IObjectContainer.RegisterTypeAs),
            new[] {typeof(Type), typeof(Type), typeof(string)},
            implementationType, interfaceType, null);
    }
}
