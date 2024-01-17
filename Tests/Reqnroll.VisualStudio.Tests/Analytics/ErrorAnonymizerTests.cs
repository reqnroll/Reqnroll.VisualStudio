#nullable disable
using System;
using System.Linq;

namespace Reqnroll.VisualStudio.Tests.Analytics;

public class ErrorAnonymizerTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ErrorAnonymizerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }


    [Fact]
    public void SimplifyStackTrace_removes_namespace_and_params()
    {
        const string stackTrace = @"
   at Reqnroll.VisualStudio.ReqnrollConnector.ReflectionExtensions.ReflectionCallMethod[T](Object obj, String methodName, Type[] parameterTypes, Object[] args)
";

        var result = ErrorAnonymizer.SimplifyStackTrace(stackTrace, false);

        result.Should().Be("ReflectionExtensions.ReflectionCallMethod[T](,,,)");
    }

    [Fact]
    public void SimplifyStackTrace_includes_line_number()
    {
        const string stackTrace = @"
   at Reqnroll.VisualStudio.ReqnrollConnector.ReflectionExtensions.ReflectionCallMethod[T](Object obj, String methodName, Type[] parameterTypes, Object[] args) in W:\SpecF\Reqnroll.VisualStudio\Reqnroll.VisualStudio.ReqnrollConnector.V2\ReflectionExtensions.cs:line 17
";

        var result = ErrorAnonymizer.SimplifyStackTrace(stackTrace, false);

        result.Should().Be("ReflectionExtensions.ReflectionCallMethod[T](,,,)L17");
    }

    [Fact]
    public void SimplifyStackTrace_keeps_own_stack_trace_entries()
    {
        const string stackTrace = @"
   at System.Reflection.RuntimeMethodInfo.InvokeAndWaitAnalyticsEvent(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
   at Reqnroll.VisualStudio.ReqnrollConnector.ReflectionExtensions.ReflectionCallMethod[T](Object obj, String methodName, Type[] parameterTypes, Object[] args)
   at Reqnroll.VisualStudio.ReqnrollConnector.Discovery.ReflectionReqnrollDiscoverer.Discover(Assembly testAssembly, String testAssemblyPath, String configurationFilePath)
";

        var result = ErrorAnonymizer.SimplifyStackTrace(stackTrace, false);

        result.Should()
            .Be("ReflectionExtensions.ReflectionCallMethod[T](,,,)-ReflectionReqnrollDiscoverer.Discover(,,)");
    }

    [Fact]
    public void SimplifyStackTrace_minimizes_stack_trace()
    {
        const string stackTrace = @"
   at System.Reflection.RuntimeMethodInfo.InvokeAndWaitAnalyticsEvent(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
   at Reqnroll.VisualStudio.ReqnrollConnector.ReflectionExtensions.ReflectionCallMethod[T](Object obj, String methodName, Type[] parameterTypes, Object[] args) in W:\SpecF\Reqnroll.VisualStudio\Reqnroll.VisualStudio.ReqnrollConnector.V2\ReflectionExtensions.cs:line 17
   at Reqnroll.VisualStudio.ReqnrollConnector.Discovery.ReflectionReqnrollDiscoverer.Discover(Assembly testAssembly, String testAssemblyPath, String configurationFilePath) in W:\SpecF\Reqnroll.VisualStudio\Reqnroll.VisualStudio.ReqnrollConnector.V2\Discovery\ReflectionReqnrollDiscoverer.cs:line 25
";

        var result = ErrorAnonymizer.SimplifyStackTrace(stackTrace);

        result.Should().Be("RE.RCM[T](,,,)L17-RSFD.D(,,)L25");
    }

    [Fact]
    public void SimplifyStackTrace_keeps_first_4_Reqnroll_stack_trace_entries()
    {
        const string stackTrace = @"
   at System.Text.RegularExpressions.Regex..ctor(String pattern, RegexOptions options) 
   at Reqnroll.Bindings.RegexFactory.Create(String regexString) 
   at Reqnroll.Bindings.BindingFactory.CreateStepBinding(StepDefinitionType type, String regexString, IBindingMethod bindingMethod, BindingScope bindingScope) 
   at Reqnroll.Bindings.Discovery.BindingSourceProcessor.ProcessStepDefinitionAttribute(BindingSourceMethod bindingSourceMethod, BindingSourceAttribute stepDefinitionAttribute, BindingScope scope) 
   at Reqnroll.Bindings.Discovery.BindingSourceProcessor.ProcessStepDefinitions(BindingSourceMethod bindingSourceMethod, BindingScope[] methodScopes) 
   at Reqnroll.Bindings.Discovery.BindingSourceProcessor.ProcessMethod(BindingSourceMethod bindingSourceMethod) 
   at Reqnroll.Bindings.Discovery.RuntimeBindingRegistryBuilder.BuildBindingsFromType(Type type) 
   at System.Reflection.RuntimeMethodInfo.InvokeAndWaitAnalyticsEvent(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
   at Reqnroll.VisualStudio.ReqnrollConnector.ReflectionExtensions.ReflectionCallMethod[T](Object obj, String methodName, Type[] parameterTypes, Object[] args)
";

        var result = ErrorAnonymizer.SimplifyStackTrace(stackTrace, false);

        result.Should()
            .Be(
                "SF.RegexFactory.Create(_)-SF.BindingFactory.CreateStepBinding(,,,)-SF.BindingSourceProcessor.ProcessStepDefinitionAttribute(,,)-SF.BindingSourceProcessor.ProcessStepDefinitions(,)-ReflectionExtensions.ReflectionCallMethod[T](,,,)");
    }

    [Fact]
    public void SimplifyStackTrace_simplifies_real_stack_trace()
    {
        const string stackTrace = @"
Server stack trace:  
   at System.Text.RegularExpressions.RegexParser.ScanRegex() 
   at System.Text.RegularExpressions.RegexParser.Parse(String re, RegexOptions op) 
   at System.Text.RegularExpressions.Regex..ctor(String pattern, RegexOptions options, TimeSpan matchTimeout, Boolean useCache) 
   at System.Text.RegularExpressions.Regex..ctor(String pattern, RegexOptions options) 
   at Reqnroll.Bindings.RegexFactory.Create(String regexString) 
   at Reqnroll.Bindings.BindingFactory.CreateStepBinding(StepDefinitionType type, String regexString, IBindingMethod bindingMethod, BindingScope bindingScope) 
   at Reqnroll.Bindings.Discovery.BindingSourceProcessor.ProcessStepDefinitionAttribute(BindingSourceMethod bindingSourceMethod, BindingSourceAttribute stepDefinitionAttribute, BindingScope scope) 
   at Reqnroll.Bindings.Discovery.BindingSourceProcessor.ProcessStepDefinitions(BindingSourceMethod bindingSourceMethod, BindingScope[] methodScopes) 
   at Reqnroll.Bindings.Discovery.BindingSourceProcessor.ProcessMethod(BindingSourceMethod bindingSourceMethod) 
   at Reqnroll.Bindings.Discovery.RuntimeBindingRegistryBuilder.BuildBindingsFromType(Type type) 
   at Reqnroll.Bindings.Discovery.RuntimeBindingRegistryBuilder.BuildBindingsFromAssembly(Assembly assembly) 
   at Reqnroll.TestRunnerManager.BuildBindingRegistry(IEnumerable`1 bindingAssemblies) 
   at Reqnroll.TestRunnerManager.InitializeBindingRegistry(ITestRunner testRunner) 
   at Reqnroll.TestRunnerManager.CreateTestRunner(Int32 threadId) 
   at Reqnroll.VisualStudio.ReqnrollConnector.Discovery.V2020.ReqnrollV2020Discoverer.GetBindingRegistry(Assembly testAssembly, String configurationFilePath) 
   at Reqnroll.VisualStudio.ReqnrollConnector.Discovery.BaseDiscoverer.DiscoverInternal(String testAssemblyPath, String configurationFilePath) 
   at Reqnroll.VisualStudio.ReqnrollConnector.Discovery.BaseDiscoverer.Discover(String testAssemblyPath, String configurationFilePath) 
   at System.Runtime.Remoting.Messaging.StackBuilderSink._PrivateProcessMessage(IntPtr md, Object[] args, Object server, Object[]& outArgs) 
   at System.Runtime.Remoting.Messaging.StackBuilderSink.SyncProcessMessage(IMessage msg) 
 
Exception rethrown at [0]:  
   at System.Runtime.Remoting.Proxies.RealProxy.HandleReturnMessage(IMessage reqMsg, IMessage retMsg) 
   at System.Runtime.Remoting.Proxies.RealProxy.PrivateInvoke(MessageData& msgData, Int32 type) 
   at Reqnroll.VisualStudio.ReqnrollConnector.Discovery.IReqnrollDiscoverer.Discover(String testAssembly, String configurationFilePath) 
   at Reqnroll.VisualStudio.ReqnrollConnector.Discovery.DiscoveryProcessor.Process() 
   at Reqnroll.VisualStudio.ReqnrollConnector.ConsoleRunner.EntryPoint(String[] args) 
";

        var result = ErrorAnonymizer.SimplifyStackTrace(stackTrace);

        result.Should()
            .Be(
                "SF.RF.C(_)-SF.BF.CSB(,,,)-SF.BSP.PSDA(,,)-SF.BSP.PSD(,)-SFV2020D.GBR(,)-BD.DI(,)-BD.D(,)-ISFD.D(,)-DP.P()-CR.EP(_)");
    }

    [Fact]
    public void SimplifyExceptions_removes_namespaces_and_simplifies_names()
    {
        var exceptions = new[]
        {
            "SomeNameSpace.Deeper.SomeException",
            "OtherNamespace.SomeOtherException"
        };

        var result = ErrorAnonymizer.SimplifyExceptions(exceptions);

        result.Should().Be("SomEx-SomOthEx");
    }

    [Fact]
    public void SimplifyExceptions_removes_known_wrapper_exceptions()
    {
        var exceptions = new[]
        {
            "Microsoft.VisualStudio.Composition.CompositionFailedException",
            typeof(TypeInitializationException).FullName,
            typeof(AggregateException).FullName,
            typeof(TargetInvocationException).FullName,
            "SomeNameSpace.Deeper.SomeException",
            "OtherNamespace.FooException"
        };

        var result = ErrorAnonymizer.SimplifyExceptions(exceptions);

        result.Should().Be("SomEx-FooEx");
    }

    [Fact]
    public void SimplifyErrorMessage_removes_punctuation_and_minmizes_words()
    {
        //const string errorMessage = "Could not load file or assembly 'Microsoft.AspNetCore.Hosting.Abstractions, Version=2.1.1.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'. The system cannot find the file specified.";
        const string errorMessage =
            "Could not load file or assembly 'Microsoft.AspNetCore.Hosting.Abstractions, Version=2.1.1.0'. The system cannot find the file specified.";

        var result = ErrorAnonymizer.SimplifyErrorMessage(errorMessage);

        result.Should().Be("CouNotLoaFilOrAssMicAspHosAbsVer2110TheSysCanFinTheFilSpe");
    }

    [Fact]
    public void SimplifyErrorMessage_removes_culture_and_public_key_token()
    {
        const string errorMessage =
            "S Microsoft.AspNetCore.Hosting.Abstractions, Version=2.1.1.0, Culture=neutral, PublicKeyToken=adb9793829ddae60 E";

        var result = ErrorAnonymizer.SimplifyErrorMessage(errorMessage);

        result.Should().Be("SMicAspHosAbsVer2110E");
    }

    [Theory]
    [InlineData(@"C:\Foo\Bar\boz.txt")]
    [InlineData(@"'C:\Foo\Bar Bar\boz.txt'")]
    [InlineData(@"""C:\Foo\Bar Bar\boz.txt""")]
    public void SimplifyErrorMessage_removes_path(string path)
    {
        var errorMessage = $"S {path} E";

        var result = ErrorAnonymizer.SimplifyErrorMessage(errorMessage);

        result.Should().Be("SE");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Method2()
    {
        Method1();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Method1()
    {
        throw new NotImplementedException(@"test error for path C:\Foo\Bar");
    }

    private void Method3()
    {
        try
        {
            Method2();
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("wrapper exception", e);
        }
    }

    [Fact]
    public void AnonymizeException_anonymizes_exception()
    {
        Exception exception = null;
        try
        {
            Method3();
        }
        catch (Exception e)
        {
            exception = e;
        }

        exception.Should().NotBeNull();

        _testOutputHelper.WriteLine(exception.ToString());
        var result = ErrorAnonymizer.AnonymizeException(exception);
        _testOutputHelper.WriteLine("Result: " + result);
        result = Regex.Replace(result, @"L\d+", "");
        result.Should().Be("InvOpeEx-NotImpEx:TesErrForPat:EAT.M1()-EAT.M2()-EAT.M3()-EAT.M3()-EAT.AE__()");
    }
}
