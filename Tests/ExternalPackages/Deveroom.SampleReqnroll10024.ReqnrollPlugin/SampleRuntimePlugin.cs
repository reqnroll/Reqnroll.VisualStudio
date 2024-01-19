using Deveroom.SampleReqnroll10024.ReqnrollPlugin;
using System.Reflection;
using Reqnroll.Bindings;
using Reqnroll.Bindings.Discovery;
using Reqnroll.Bindings.Reflection;
using Reqnroll.Plugins;
using Reqnroll.UnitTestProvider;

[assembly: RuntimePlugin(typeof(SampleRuntimePlugin))]

namespace Deveroom.SampleReqnroll10024.ReqnrollPlugin;

public class SampleRuntimePlugin : IRuntimePlugin
{
    public static bool Initialized = false;

    public void Initialize(RuntimePluginEvents runtimePluginEvents, RuntimePluginParameters runtimePluginParameters,
        UnitTestProviderConfiguration unitTestProviderConfiguration)
    {
        Initialized = true;
        runtimePluginEvents.CustomizeGlobalDependencies +=
            (_, args) =>
            {
                args.ObjectContainer.RegisterInstanceAs(this);
                args.ObjectContainer.RegisterTypeAs<CustomBindingRegistryBuilder, IRuntimeBindingRegistryBuilder>();
            };
    }

    public class CustomBindingRegistryBuilder : IRuntimeBindingRegistryBuilder
    {
        private readonly RuntimeBindingRegistryBuilder _innerBuilder;
        private readonly IBindingRegistry _bindingRegistry;

        public CustomBindingRegistryBuilder(RuntimeBindingRegistryBuilder innerBuilder, IBindingRegistry bindingRegistry)
        {
            _innerBuilder = innerBuilder;
            _bindingRegistry = bindingRegistry;
        }

        public void BuildBindingsFromAssembly(Assembly assembly)
        {
            _innerBuilder.BuildBindingsFromAssembly(assembly);
        }

        public void BuildingCompleted()
        {
            var bindingMethod = new RuntimeBindingMethod(typeof(PluginSteps).GetMethod(nameof(PluginSteps.ThenThereShouldBeAStepFromAPlugin)));
            var builder = new RegexStepDefinitionBindingBuilder(StepDefinitionType.Then, bindingMethod, null, "there should be a step from a plugin");
            var stepDefinitionBinding = builder.BuildSingle();            
            _bindingRegistry.RegisterStepDefinitionBinding(stepDefinitionBinding);
            _innerBuilder.BuildingCompleted();
        }
    }
}

public class PluginSteps
{
    public void ThenThereShouldBeAStepFromAPlugin()
    {
        Console.WriteLine("Hello from plugin!");
    }
}
