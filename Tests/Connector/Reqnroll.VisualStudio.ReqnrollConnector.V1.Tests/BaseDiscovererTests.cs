#nullable disable
using System.Reflection;
using System.Text.RegularExpressions;
using CucumberExpressions;
using FluentAssertions;
using Reqnroll.VisualStudio.ReqnrollConnector.Discovery;
using Reqnroll.VisualStudio.ReqnrollConnector.Models;
using Reqnroll.VisualStudio.ReqnrollConnector.SourceDiscovery;
using Reqnroll;
using Reqnroll.Bindings;
using Reqnroll.Bindings.Reflection;
using Xunit;

namespace Reqnroll.VisualStudio.ReqnrollConnector.V1.Tests;

public class BaseDiscovererTests
{
    private readonly BindingRegistry _bindingRegistry = new();

    private StubDiscoverer CreateSut()
    {
        var stubDiscoverer = new StubDiscoverer();
        stubDiscoverer.BindingRegistry = _bindingRegistry;
        return stubDiscoverer;
    }

    private string GetTestAssemblyPath() => Assembly.GetExecutingAssembly().Location;

    private void RegisterStepDefinitionBinding(string regex = "I press add",
        StepDefinitionType type = StepDefinitionType.When, string method = nameof(StubBindingClass.WhenIPressAdd),
        BindingScope scope = null)
    {
        var methodInfo = GetMethodInfo(method);
        _bindingRegistry.RegisterStepDefinitionBinding(new StepDefinitionBinding(type, 
            new RuntimeBindingMethod(methodInfo), scope, StepDefinitionExpressionTypes.RegularExpression, regex, new RegularExpression(new Regex(regex))));
    }

    private void RegisterStepDefinitionBindingWithSourceAndError(string regex = "I press add",
        StepDefinitionType type = StepDefinitionType.When, string method = nameof(StubBindingClass.WhenIPressAdd),
        BindingScope scope = null, string sourceExpression = null, string error = null)
    {
        var methodInfo = GetMethodInfo(method);
        _bindingRegistry.RegisterStepDefinitionBinding(
            new StepDefinitionBindingWithSourceAndError(type, regex, new RuntimeBindingMethod(methodInfo), scope)
            {
                SourceExpression = sourceExpression,
                ErrorMessage = error
            });
    }

    private static MethodInfo GetMethodInfo(string method)
    {
        var methodInfo =
            typeof(StubBindingClass).GetMethod(method,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
        methodInfo.Should().NotBeNull();
        return methodInfo;
    }

    [Fact]
    public void Discovers_step_definitions()
    {
        RegisterStepDefinitionBinding(method: nameof(StubBindingClass.WhenIPressAdd));
        RegisterStepDefinitionBinding(method: nameof(StubBindingClass.WhenIPressMultiply));

        var sut = CreateSut();

        var result = sut.DiscoverInternal(GetTestAssemblyPath(), null);

        result.StepDefinitions.Should().NotBeNullOrEmpty();
        result.StepDefinitions.Should().HaveCount(2);
    }

    [Fact]
    public void Discovers_step_definition_type()
    {
        RegisterStepDefinitionBinding(type: StepDefinitionType.Given,
            method: nameof(StubBindingClass.GivenACalculator));
        RegisterStepDefinitionBinding(type: StepDefinitionType.When, method: nameof(StubBindingClass.WhenIPressAdd));
        RegisterStepDefinitionBinding(type: StepDefinitionType.Then,
            method: nameof(StubBindingClass.ThenTheReasultShouldBeCalculated));

        var sut = CreateSut();

        var result = sut.DiscoverInternal(GetTestAssemblyPath(), null);

        result.StepDefinitions.Should().HaveCount(3);
        result.StepDefinitions.Should().ContainSingle(sd => sd.Type == "Given");
        result.StepDefinitions.Should().ContainSingle(sd => sd.Type == "When");
        result.StepDefinitions.Should().ContainSingle(sd => sd.Type == "Then");
    }

    [Fact]
    public void Discovers_regex()
    {
        RegisterStepDefinitionBinding("this is? a (.*) regex");

        var sut = CreateSut();

        var result = sut.DiscoverInternal(GetTestAssemblyPath(), null);

        result.StepDefinitions.Should().HaveCount(1);
        result.StepDefinitions[0].Regex.Should().Be("^this is? a (.*) regex$");
    }

    [Fact]
    public void Discovers_tag_scope()
    {
        RegisterStepDefinitionBinding(scope: new BindingScope("foo", null, null));

        var sut = CreateSut();

        var result = sut.DiscoverInternal(GetTestAssemblyPath(), null);

        result.StepDefinitions.Should().HaveCount(1);
        result.StepDefinitions[0].Scope.Tag.Should().Be("@foo");
    }

    [Fact]
    public void Discovers_feature_scope()
    {
        RegisterStepDefinitionBinding(scope: new BindingScope(null, "foo", null));

        var sut = CreateSut();

        var result = sut.DiscoverInternal(GetTestAssemblyPath(), null);

        result.StepDefinitions.Should().HaveCount(1);
        result.StepDefinitions[0].Scope.FeatureTitle.Should().Be("foo");
    }

    [Fact]
    public void Discovers_scenario_scope()
    {
        RegisterStepDefinitionBinding(scope: new BindingScope(null, null, "foo"));

        var sut = CreateSut();

        var result = sut.DiscoverInternal(GetTestAssemblyPath(), null);

        result.StepDefinitions.Should().HaveCount(1);
        result.StepDefinitions[0].Scope.ScenarioTitle.Should().Be("foo");
    }

    [Fact]
    public void Scope_is_null_for_non_scoped()
    {
        RegisterStepDefinitionBinding();

        var sut = CreateSut();

        var result = sut.DiscoverInternal(GetTestAssemblyPath(), null);

        result.StepDefinitions.Should().HaveCount(1);
        result.StepDefinitions[0].Scope.Should().BeNull();
    }

    [Fact]
    public void Discovers_source_location()
    {
        RegisterStepDefinitionBinding();
        StubSymbolReader.SetNextSymbol(@"C:\Temp\MyFile.cs", 12, 3);

        var sut = CreateSut();

        var result = sut.DiscoverInternal(GetTestAssemblyPath(), null);

        result.StepDefinitions.Should().HaveCount(1);
        result.StepDefinitions[0].SourceLocation.Should().NotBeNullOrEmpty();
        result.StepDefinitions[0].SourceLocation.Should().Contain("|12|3");
    }

    [Fact]
    public void Collects_source_files()
    {
        RegisterStepDefinitionBinding();
        StubSymbolReader.SetNextSymbol(@"C:\Temp\MyFile.cs", 12, 3);
        RegisterStepDefinitionBinding();
        StubSymbolReader.SetNextSymbol(@"C:\Temp\MyFile.cs", 16, 5);
        RegisterStepDefinitionBinding();
        StubSymbolReader.SetNextSymbol(@"C:\Temp\OtherFile.cs", 12, 3);

        var sut = CreateSut();

        var result = sut.DiscoverInternal(GetTestAssemblyPath(), null);

        result.StepDefinitions.Should().HaveCount(3);
        result.StepDefinitions.Should()
            .OnlyContain(sd => sd.SourceLocation.StartsWith("#0|") || sd.SourceLocation.StartsWith("#1|"));

        result.SourceFiles.Should().ContainKeys("0", "1");
        result.SourceFiles.Should().ContainValues(@"C:\Temp\MyFile.cs", @"C:\Temp\OtherFile.cs");
    }

    [Fact]
    public void Discovers_param_types()
    {
        RegisterStepDefinitionBinding(method: nameof(StubBindingClass.WithParams));

        var sut = CreateSut();

        var result = sut.DiscoverInternal(GetTestAssemblyPath(), null);

        result.StepDefinitions.Should().HaveCount(1);
        result.StepDefinitions[0].ParamTypes.Should().NotBeNullOrEmpty();
        result.StepDefinitions[0].ParamTypes.Should().Contain("|");
    }

    [Fact]
    public void ParamTypes_is_null_for_stepdef_without_param()
    {
        RegisterStepDefinitionBinding(method: nameof(StubBindingClass.WhenIPressAdd));

        var sut = CreateSut();

        var result = sut.DiscoverInternal(GetTestAssemblyPath(), null);

        result.StepDefinitions.Should().HaveCount(1);
        result.StepDefinitions[0].ParamTypes.Should().BeNull();
    }

    [Fact]
    public void Discovers_custom_param_types_with_type_names()
    {
        RegisterStepDefinitionBinding(method: nameof(StubBindingClass.WithCustomParam));

        var sut = CreateSut();

        var result = sut.DiscoverInternal(GetTestAssemblyPath(), null);

        result.StepDefinitions.Should().HaveCount(1);
        result.StepDefinitions[0].ParamTypes.Should().Be("#0");
        result.TypeNames.Should().ContainValue(typeof(CustomParamType).FullName);
    }

    [Fact]
    public void Discovers_usual_param_types_with_shortcuts()
    {
        RegisterStepDefinitionBinding(method: nameof(StubBindingClass.WithStdParams));

        var sut = CreateSut();

        var result = sut.DiscoverInternal(GetTestAssemblyPath(), null);

        result.StepDefinitions.Should().HaveCount(1);
        result.StepDefinitions[0].ParamTypes.Should().Be("s|s|i|st");
        result.TypeNames.Should().HaveCount(0);
    }

    [Fact]
    public void Discovers_source_expression()
    {
        RegisterStepDefinitionBindingWithSourceAndError(sourceExpression: "this is source {num}");

        var sut = CreateSut();

        var result = sut.DiscoverInternal(GetTestAssemblyPath(), null);

        result.StepDefinitions.Should().HaveCount(1);
        result.StepDefinitions[0].Expression.Should().Be("this is source {num}");
    }

    [Fact]
    public void Discovers_error()
    {
        RegisterStepDefinitionBindingWithSourceAndError(error: "this an error");

        var sut = CreateSut();

        var result = sut.DiscoverInternal(GetTestAssemblyPath(), null);

        result.StepDefinitions.Should().HaveCount(1);
        result.StepDefinitions[0].Error.Should().Be("this an error");
    }


    [Fact]
    public void Does_not_discover_source_expression_and_error_from_old_stepdefs()
    {
        RegisterStepDefinitionBinding();

        var sut = CreateSut();

        var result = sut.DiscoverInternal(GetTestAssemblyPath(), null);

        result.StepDefinitions.Should().HaveCount(1);
        result.StepDefinitions[0].Expression.Should().BeNull();
        result.StepDefinitions[0].Error.Should().BeNull();
    }

    [Fact]
    public void Should_detect_regex_error()
    {
        RegisterStepDefinitionBinding();

        var sut = CreateSut();
        try
        {
            new Regex("invalid (regex");
        }
        catch (Exception ex)
        {
            sut.GetBindingRegistryError = new TargetInvocationException(ex);
        }

        var result = sut.DiscoverInternal(GetTestAssemblyPath(), null);

        result.StepDefinitions.Should().HaveCount(1);
        result.StepDefinitions[0].Error.Should().NotBeNullOrEmpty();
    }

    private class StubDiscoverer : RemotingBaseDiscoverer
    {
        public Exception GetBindingRegistryError { get; set; }
        public IBindingRegistry BindingRegistry { get; set; }

        protected override IBindingRegistry GetBindingRegistry(Assembly testAssembly, string configFilePath)
        {
            if (GetBindingRegistryError != null)
                throw GetBindingRegistryError;
            return BindingRegistry;
        }

        protected override IEnumerable<IStepDefinitionBinding> GetStepDefinitions(IBindingRegistry bindingRegistry) =>
            bindingRegistry.GetStepDefinitions();

        protected override IDeveroomSymbolReader CreateSymbolReader(string assemblyFilePath,
            WarningCollector warningCollector) => new StubSymbolReader();

        public DiscoveryResult DiscoverInternal(string testAssemblyPath, string configFilePath)
        {
            var testAssembly = Assembly.LoadFrom(testAssemblyPath);
            return DiscoverInternal(testAssembly, testAssemblyPath, configFilePath);
        }

        protected override object CreateGlobalContainer(Assembly testAssembly, string configFilePath) =>
            throw new NotImplementedException();

        protected override void RegisterTypeAs<TType, TInterface>(object globalContainer)
        {
            throw new NotImplementedException();
        }

        protected override T Resolve<T>(object globalContainer) => throw new NotImplementedException();

        protected override IBindingRegistry ResolveBindingRegistry(Assembly testAssembly, object globalContainer) =>
            throw new NotImplementedException();
    }

    private class StubSymbolReader : IDeveroomSymbolReader
    {
        public static readonly List<MethodSymbol> NextSymbols = new();

        public MethodSymbol ReadMethodSymbol(int token)
        {
            var symbol = NextSymbols.FirstOrDefault();
            if (NextSymbols.Any())
                NextSymbols.RemoveAt(0);
            return symbol;
        }

        public void Dispose()
        {
        }

        public static void SetNextSymbol(string fileName, int line, int column)
        {
            NextSymbols.Add(
                new MethodSymbol(1, new[] {new SequencePoint(0, fileName, line, line, column, column)}));
        }
    }

    private class CustomParamType
    {
    }

    private class StubBindingClass
    {
        public void GivenACalculator()
        {
        }

        public void WhenIPressAdd()
        {
        }

        public void WhenIPressMultiply()
        {
        }

        public void ThenTheReasultShouldBeCalculated()
        {
        }

        public void WithCustomParam(CustomParamType p1)
        {
        }

        public void WithParams(string p0, CustomParamType p1, int p2, Table p3)
        {
        }

        public void WithStdParams(string p0, string p1, int p2, Table p3)
        {
        }
    }

    public class StepDefinitionBindingWithSourceAndError : StepDefinitionBinding
    {
        public StepDefinitionBindingWithSourceAndError(StepDefinitionType stepDefinitionType, string regexString,
            IBindingMethod bindingMethod, BindingScope bindingScope) : base(stepDefinitionType, bindingMethod, bindingScope,
            StepDefinitionExpressionTypes.RegularExpression, regexString, new RegularExpression(new Regex(regexString)))
        {
        }

        public string SourceExpression { get; set; }
        public string ErrorMessage { get; set; }
    }
}
