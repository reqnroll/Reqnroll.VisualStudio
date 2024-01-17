#nullable disable
using System;
using System.Linq;
using System.Reflection;

namespace Reqnroll.VisualStudio.VsxStubs;

public class VsxStubObjects
{
    public static object GuardedOperations { get; private set; }
    public static IBufferGraphFactoryService BufferGraphFactoryService { get; private set; }
    public static ITextBufferFactoryService BufferFactoryService { get; private set; }

    public static ITextBuffer CreateTextBuffer(string content, string contentType)
    {
        Initialize();

        var stringRebuilder = CallStaticMethod(
            "Microsoft.VisualStudio.Text.Implementation.StringRebuilder, Microsoft.VisualStudio.Platform.VSEditor",
            "Create", content);
        var contentTypeImpl = CreateInstance(
            "Microsoft.VisualStudio.Utilities.Implementation.ContentTypeImpl, Microsoft.VisualStudio.Platform.VSEditor",
            contentType, null, null);
        var defaultTextDifferencingService = CreateInstance(
            "Microsoft.VisualStudio.Text.Differencing.Implementation.DefaultTextDifferencingService, Microsoft.VisualStudio.Platform.VSEditor");
        var textBuffer = CreateInstance(
            "Microsoft.VisualStudio.Text.Implementation.TextBuffer, Microsoft.VisualStudio.Platform.VSEditor",
            contentTypeImpl, stringRebuilder, defaultTextDifferencingService, GuardedOperations, BufferFactoryService,
            null, null);
        return (ITextBuffer) textBuffer;
    }

    private static object CallStaticMethod(string typeName, string methodName, params object[] args)
    {
        var type = Type.GetType(typeName, true);
        return CallStaticMethod(type, methodName, args);
    }

    private static object CallStaticMethod(Type type, string methodName, params object[] args)
    {
        return CallStaticMethod(type, methodName, args.Select(a => a.GetType()).ToArray(), args);
    }

    private static object CallStaticMethod(Type type, string methodName, Type[] parameTypes, params object[] args)
    {
        var method = type.GetMethod(methodName,
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
            null, parameTypes, null);
        method.Should().NotBeNull($"Type '{type.FullName}' should have a method '{methodName}'");
        return method.Invoke(null, args);
    }

    private static object CreateInstance(string typeName, params object[] args)
    {
        var type = Type.GetType(typeName, true);
        return CreateInstance(type, args);
    }

    private static object CreateInstance(Type type, params object[] args) => Activator.CreateInstance(type,
        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, args, null);

    public static T CreateObject<T>(string typeName, params object[] args)
    {
        var type = Type.GetType(typeName, true);
        return (T) Activator.CreateInstance(type,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, args, null);
    }

    private static void SetField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        field.SetValue(obj, value);
    }

    public static void Initialize()
    {
        GuardedOperations =
            CreateObject<object>(
                "Microsoft.VisualStudio.Text.Utilities.GuardedOperations, Microsoft.VisualStudio.Platform.VSEditor");
        BufferGraphFactoryService = CreateObject<IBufferGraphFactoryService>(
            "Microsoft.VisualStudio.Text.Projection.Implementation.BufferGraphFactoryService, Microsoft.VisualStudio.Platform.VSEditor");
        SetField(BufferGraphFactoryService, nameof(GuardedOperations), GuardedOperations);

        BufferFactoryService = CreateObject<ITextBufferFactoryService>(
            "Microsoft.VisualStudio.Text.Implementation.BufferFactoryService, Microsoft.VisualStudio.Platform.VSEditor");
    }
}
