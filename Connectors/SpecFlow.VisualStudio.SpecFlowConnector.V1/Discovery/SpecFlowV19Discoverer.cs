#nullable disable
namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery;

public class SpecFlowV19Discoverer : SpecFlowV20Discoverer
{
    protected override object CreateGlobalContainer(Assembly testAssembly, string configFilePath)
    {
        var testRunContainerBuilderType = typeof(ITestRunner).Assembly
            .GetType("TechTalk.SpecFlow.Infrastructure.TestRunContainerBuilder", true);
        var container = testRunContainerBuilderType
            .ReflectionCallStaticMethod<object>("CreateContainer",
                new[] {typeof(IRuntimeConfigurationProvider)}, new object[] {null});
        return container;
    }

    protected override IBindingRegistry ResolveBindingRegistry(Assembly testAssembly, object globalContainer)
    {
        ITestRunner testRunner = Resolve<ITestRunnerFactory>(globalContainer).Create(testAssembly);
        var testExecutionEngine = testRunner.ReflectionGetField<ITestExecutionEngine>("executionEngine");
        return testExecutionEngine.ReflectionGetField<IBindingRegistry>("bindingRegistry");
    }
}
