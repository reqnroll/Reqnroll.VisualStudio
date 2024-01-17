#nullable enable
namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery;

public class DynamicRuntimePluginLoaderFactory
{
    public Type Create()
    {
        Type interfaceType = typeof(IRuntimePluginLoader_3_0_220);
        Type baseType = typeof(LoadContextPluginLoader);
        MethodInfo baseMethod = baseType.GetMethod(nameof(LoadContextPluginLoader.LoadPlugin))!;

        var typeBuilder = BuildTypeBuilder(baseType, interfaceType);
        EmitConstructor(baseType, typeBuilder);

        EmitLoadPluginMethod(typeBuilder, baseMethod, interfaceType);

        return typeBuilder.CreateType()!;
    }

    /// <summary>
    ///     Create a Type Builder that generates a type directly into the current AppDomain.
    /// </summary>
    private static TypeBuilder BuildTypeBuilder(Type baseType, Type interfaceType)
    {
        var moduleBuilder = BuildModuleBuilder();
        var typeBuilder = moduleBuilder.DefineType("DynamicRuntimePluginLoader",
            TypeAttributes.Class | TypeAttributes.Public, baseType, new[] {interfaceType});
        return typeBuilder;
    }

    private static ModuleBuilder BuildModuleBuilder()
    {
        var assemblyName = new AssemblyName("SpecFlowConnectorDynamicAssembly");
        var assemblyBuilder = BuildAssemblyBuilder(assemblyName);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        return moduleBuilder;
    }

    private static AssemblyBuilder BuildAssemblyBuilder(AssemblyName assemblyName)
    {
#if NETFRAMEWORK
        var appDomain = AppDomain.CurrentDomain;
        var assemblyBuilder = appDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
#else
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            assemblyName,
            AssemblyBuilderAccess.Run);
#endif
        return assemblyBuilder;
    }

    private static void EmitConstructor(Type baseType, TypeBuilder typeBuilder)
    {
        var parameterTypes = new[] {typeof(AssemblyLoadContext)};
        ConstructorInfo baseConstructor = baseType
            .GetConstructor(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance, null,
                parameterTypes, null)!;
        ConstructorBuilder constructor = typeBuilder
            .DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameterTypes);

        ILGenerator ilGenerator = constructor.GetILGenerator();

        // Generate constructor code
        ilGenerator.Emit(OpCodes.Ldarg_0); // push "this" onto stack.
        ilGenerator.Emit(OpCodes.Ldarg_1); // push the AssemblyLoadContext parameter
        ilGenerator.Emit(OpCodes.Call, baseConstructor); // call base constructor

        ilGenerator.Emit(OpCodes.Nop); // C# compiler add 2 NOPS, so
        ilGenerator.Emit(OpCodes.Nop); // we'll add them, too.

        ilGenerator.Emit(OpCodes.Ret); // Return
    }

    private static void EmitLoadPluginMethod(TypeBuilder typeBuilder, MethodInfo baseMethod, Type interfaceType)
    {
        MethodBuilder methodBuilder =
            typeBuilder.DefineMethod(nameof(IRuntimePluginLoader_3_0_220.LoadPlugin),
                MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(IRuntimePlugin),
                new[] {typeof(string), typeof(ITraceListener)});
        ILGenerator methodIl = methodBuilder.GetILGenerator();
        methodIl.Emit(OpCodes.Ldarg_0); // push "this" onto stack.
        methodIl.Emit(OpCodes.Ldarg_1); // push the string parameter
        methodIl.Emit(OpCodes.Ldarg_2); // push the ITraceListener parameter
        methodIl.Emit(OpCodes.Ldc_I4_0); // push false as 3rd parameter
        methodIl.EmitCall(OpCodes.Call, baseMethod, null);
        methodIl.Emit(OpCodes.Ret);

        MethodInfo loadPluginMethodInfo = interfaceType.GetMethod(nameof(IRuntimePluginLoader_3_0_220.LoadPlugin))!;
        typeBuilder.DefineMethodOverride(methodBuilder, loadPluginMethodInfo);
    }
}
