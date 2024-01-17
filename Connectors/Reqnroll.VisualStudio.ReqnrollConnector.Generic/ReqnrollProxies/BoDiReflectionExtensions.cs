namespace ReqnrollConnector.ReqnrollProxies;

public static class BoDiReflectionExtensions
{
    //public void RegisterInstanceAs(object instance, Type interfaceType, string name = null, bool dispose = false)
    public static void ReflectionRegisterInstanceAs<T>(this object container, T instance)
    {
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        container.ReflectionCallMethod(
            nameof(IObjectContainer.RegisterInstanceAs),
            new[] { typeof(T), typeof(Type), typeof(string), typeof(bool) },
            instance, typeof(T), null!, false);
    }

    public static T ReflectionResolve<T>(this object container)
    {
        return container.ReflectionCallMethod<T>(nameof(IObjectContainer.Resolve),
            new[] {typeof(Type), typeof(string)},
            typeof(T), null!);
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
            implementationType, interfaceType, null!);
    }
}
