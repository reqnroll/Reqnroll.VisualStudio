namespace Reqnroll.VisualStudio.Tests.Discovery;

public class BindingNavigationTests
{
    private const string Source = @"
namespace First
{
    public class Steps
    {
        public void Bind() {}
        public void Bind(int value) {}
        public void Bind(string value) {}
        public void Bind(ref int value, string[][] values) {}
        public void Generic<T>(T value) {}
        public void Unsupported(int[,] value) {}
    }
}
namespace Second { public class Steps { public void Bind(int value) {} } }";

    private static (Compilation compilation, Assembly assembly) CreateReferencedBindingAssembly()
    {
        var bindingCompilation = CSharpCompilation.Create("Bindings" + Guid.NewGuid().ToString("N"),
            new[] { CSharpSyntaxTree.ParseText(Source) },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = bindingCompilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        var bytes = stream.ToArray();
        var compilation = CSharpCompilation.Create("FeatureProject", references: new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromImage(bytes)
        });
        return (compilation, Assembly.Load(bytes));
    }

    [Fact]
    public void Resolves_exact_assembly_namespace_overload_and_byref_array_signature()
    {
        var (compilation, assembly) = CreateReferencedBindingAssembly();
        foreach (var method in assembly.GetType("First.Steps")!.GetMethods(BindingFlags.Public | BindingFlags.Instance |
                                                                        BindingFlags.DeclaredOnly)
                     .Where(m => m.Name == "Bind"))
        {
            var target = BindingNavigationTarget.FromMethod(method);
            target.Should().NotBeNull();
            var symbol = BindingNavigationSymbolResolver.Resolve(compilation, target!);
            symbol.Should().NotBeNull();
            symbol!.GetDocumentationCommentId().Should().Be(target!.DocumentationCommentId);
            symbol.ContainingNamespace.ToDisplayString().Should().Be("First");
            symbol.ContainingAssembly.Identity.ToString().Should().Be(target.AssemblyIdentity);
        }
    }

    [Theory]
    [InlineData("Generic")]
    [InlineData("Unsupported")]
    public void Unsupported_signatures_fail_closed_instead_of_selecting_a_name_match(string name)
    {
        var (_, assembly) = CreateReferencedBindingAssembly();
        BindingNavigationTarget.FromMethod(assembly.GetType("First.Steps")!.GetMethod(name)!).Should().BeNull();
    }

    [Theory]
    [InlineData("", "M:First.Steps.Bind(System.Int32)")]
    [InlineData("Different, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "M:First.Steps.Bind(System.Int32)")]
    [InlineData(null, "M:First.Steps.Bind(System.Int32)")]
    [InlineData("", "https://example.test/source.cs")]
    public void Rejects_wrong_or_missing_assembly_and_non_symbol_input(string? identity, string id)
    {
        var (compilation, _) = CreateReferencedBindingAssembly();
        BindingNavigationSymbolResolver.Resolve(compilation, new BindingNavigationTarget
        {
            AssemblyIdentity = identity!,
            DocumentationCommentId = id
        }).Should().BeNull();
    }

    [Fact]
    public void Rejects_changed_assembly_version_and_unknown_overload()
    {
        var (compilation, assembly) = CreateReferencedBindingAssembly();
        var target = BindingNavigationTarget.FromMethod(assembly.GetType("First.Steps")!.GetMethod("Bind", Type.EmptyTypes)!)!;
        target.AssemblyIdentity = target.AssemblyIdentity.Replace("Version=0.0.0.0", "Version=9.0.0.0");
        BindingNavigationSymbolResolver.Resolve(compilation, target).Should().BeNull();
        target.AssemblyIdentity = assembly.FullName!;
        target.DocumentationCommentId = "M:First.Steps.Bind(System.Boolean)";
        BindingNavigationSymbolResolver.Resolve(compilation, target).Should().BeNull();
    }

    [Fact]
    public void Imports_distinct_exact_targets_even_when_the_legacy_display_name_collides()
    {
        var importer = new BindingImporter(new Dictionary<string, string>(), new Dictionary<string, string>(),
            new StubLogger());
        var first = new StepDefinition
        {
            Type = "Given", Method = "Steps.Bind(Int32)", Regex = "first",
            NavigationTarget = new BindingNavigationTarget
            {
                AssemblyIdentity = "Bindings", DocumentationCommentId = "M:First.Steps.Bind(System.Int32)"
            }
        };
        var second = new StepDefinition
        {
            Type = "Given", Method = first.Method, Regex = "second",
            NavigationTarget = new BindingNavigationTarget
            {
                AssemblyIdentity = "Bindings", DocumentationCommentId = "M:Second.Steps.Bind(System.Int32)"
            }
        };
        var firstBinding = importer.ImportStepDefinition(first);
        var secondBinding = importer.ImportStepDefinition(second);
        firstBinding.Implementation.Should().NotBeSameAs(secondBinding.Implementation);
        secondBinding.Implementation.NavigationTarget.Should().Be(second.NavigationTarget);
    }

    [Fact]
    public void Hook_and_step_protocol_roundtrip_preserves_navigation_identity()
    {
        var target = new BindingNavigationTarget
        {
            AssemblyIdentity = "Bindings, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            DocumentationCommentId = "M:First.Steps.Bind(System.Int32)"
        };
        var step = new StepDefinition { Type = "Given", Method = "Steps.Bind", NavigationTarget = target };
        var hook = new Hook { Type = "BeforeScenario", Method = "Steps.Before", NavigationTarget = target };
        var importer = new BindingImporter(new Dictionary<string, string>(), new Dictionary<string, string>(),
            new StubLogger());
        var stepCopy = Newtonsoft.Json.JsonConvert.DeserializeObject<StepDefinition>(
            Newtonsoft.Json.JsonConvert.SerializeObject(step))!;
        var hookCopy = Newtonsoft.Json.JsonConvert.DeserializeObject<Hook>(
            Newtonsoft.Json.JsonConvert.SerializeObject(hook))!;
        importer.ImportStepDefinition(stepCopy).Implementation.NavigationTarget.Should().Be(target);
        importer.ImportHook(hookCopy).Implementation.NavigationTarget.Should().Be(target);
        stepCopy.Should().Be(step);
        hookCopy.Should().Be(hook);
    }
}
