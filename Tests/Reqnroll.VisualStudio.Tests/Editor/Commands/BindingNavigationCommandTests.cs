using Reqnroll.VisualStudio.ProjectSystem.Actions;
using Reqnroll.VisualStudio.Tests.Editor.Services;

namespace Reqnroll.VisualStudio.Tests.Editor.Commands;

public class BindingNavigationCommandTests
{
    private readonly StubIdeScope _ideScope;
    private readonly IIdeActions _actions = Substitute.For<IIdeActions, IBindingNavigation>();
    private readonly IProjectScope _project = Substitute.For<IProjectScope>();
    private readonly ITestOutputHelper _output;

    public BindingNavigationCommandTests(ITestOutputHelper output)
    {
        _output = output;
        _ideScope = new StubIdeScope(output) { Actions = _actions };
    }

    private sealed class JumpCommand : DeveroomEditorCommandBase
    {
        public JumpCommand(IIdeScope scope) : base(scope, null!, null!) { }

        public void Jump(ProjectBindingImplementation implementation, IProjectScope project, Action<bool>? afterJump)
            => PerformJump("binding", true, implementation, afterJump, project);
    }

    private static ProjectBindingImplementation Implementation(string? path, bool identity = true) => new(
        "Steps.Bind", null, path == null ? null! : new SourceLocation(path, 1, 1),
        identity ? new BindingNavigationTarget
        {
            AssemblyIdentity = "Bindings, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            DocumentationCommentId = "M:Steps.Bind"
        } : null);

    [Theory]
    [InlineData(@"C:\missing\Steps.cs")]
    [InlineData(@"/_/repo/Steps.cs")]
    [InlineData(@"..\..\Steps.cs")]
    [InlineData("https://example.test/Steps.cs")]
    [InlineData(null)]
    public async Task Missing_source_falls_back_to_exact_symbol_navigation(string? path)
    {
        var implementation = Implementation(path);
        var navigation = (IBindingNavigation)_actions;
        navigation.NavigateToBindingAsync(implementation, _project).Returns(Task.FromResult(true));

        new JumpCommand(_ideScope).Jump(implementation, _project, null);

        await navigation.Received(1).NavigateToBindingAsync(implementation, _project);
        if (path == null)
            _actions.DidNotReceive().NavigateTo(Arg.Any<SourceLocation>());
        else
            _actions.Received(1).NavigateTo(implementation.SourceLocation!);
        _actions.DidNotReceive().ShowProblem(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Native_navigation_never_runs_a_rename_continuation()
    {
        var implementation = Implementation(@"C:\missing\Steps.cs");
        var navigation = (IBindingNavigation)_actions;
        navigation.NavigateToBindingAsync(implementation, _project).Returns(Task.FromResult(true));
        var invoked = false;

        new JumpCommand(_ideScope).Jump(implementation, _project, _ => invoked = true);

        invoked.Should().BeFalse();
        await navigation.Received(1).NavigateToBindingAsync(implementation, _project);
        _actions.Received(1).ShowProblem(Arg.Is<string>(s => s.Contains("cannot be renamed")), null);
    }

    [Fact]
    public void Native_navigation_failure_is_reported()
    {
        var implementation = Implementation(null);
        ((IBindingNavigation)_actions).NavigateToBindingAsync(implementation, _project).Returns(Task.FromResult(false));

        new JumpCommand(_ideScope).Jump(implementation, _project, null);

        _actions.Received(1).ShowProblem(Arg.Is<string>(s => s.Contains("decompiled")), null);
    }

    [Fact]
    public async Task Existing_local_file_preserves_the_original_navigation_and_continuation()
    {
        const string path = @"C:\local\Steps.cs";
        _ideScope.FileSystem.Directory.CreateDirectory(@"C:\local");
        _ideScope.FileSystem.File.WriteAllText(path, "source");
        var implementation = Implementation(path);
        _actions.NavigateTo(implementation.SourceLocation!).Returns(true);
        var invoked = false;

        new JumpCommand(_ideScope).Jump(implementation, _project, _ => invoked = true);

        invoked.Should().BeTrue();
        await ((IBindingNavigation)_actions).DidNotReceive()
            .NavigateToBindingAsync(Arg.Any<ProjectBindingImplementation>(), Arg.Any<IProjectScope>());
    }

    [Fact]
    public async Task Legacy_connector_without_identity_does_not_guess_a_symbol()
    {
        new JumpCommand(_ideScope).Jump(Implementation(null, false), _project, null);

        await ((IBindingNavigation)_actions).DidNotReceive()
            .NavigateToBindingAsync(Arg.Any<ProjectBindingImplementation>(), Arg.Any<IProjectScope>());
        _actions.Received(1).ShowProblem(Arg.Is<string>(s => s.Contains("No source location")), null);
    }

    [Fact]
    public async Task Successful_project_item_navigation_is_preserved_even_if_file_exists_returns_false()
    {
        var implementation = Implementation(@"C:\missing\Steps.cs");
        _actions.NavigateTo(implementation.SourceLocation!).Returns(true);
        var invoked = false;

        new JumpCommand(_ideScope).Jump(implementation, _project, _ => invoked = true);

        invoked.Should().BeTrue();
        await ((IBindingNavigation)_actions).DidNotReceive()
            .NavigateToBindingAsync(Arg.Any<ProjectBindingImplementation>(), Arg.Any<IProjectScope>());
    }

    [Fact]
    public async Task Existing_file_outside_the_solution_uses_symbol_navigation()
    {
        const string path = @"C:\local\Steps.cs";
        _ideScope.FileSystem.Directory.CreateDirectory(@"C:\local");
        _ideScope.FileSystem.File.WriteAllText(path, "source");

        var implementation = Implementation(path);
        ((IBindingNavigation)_actions).NavigateToBindingAsync(implementation, _project).Returns(Task.FromResult(true));
        new JumpCommand(_ideScope).Jump(implementation, _project, null);

        await ((IBindingNavigation)_actions).Received(1).NavigateToBindingAsync(implementation, _project);
        _actions.DidNotReceive().ShowProblem(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void Native_navigation_exception_is_reported_without_running_a_continuation()
    {
        var implementation = Implementation(null);
        ((IBindingNavigation)_actions).NavigateToBindingAsync(implementation, _project)
            .Returns(Task.FromException<bool>(new InvalidOperationException("Native service failed")));
        var invoked = false;

        new JumpCommand(_ideScope).Jump(implementation, _project, _ => invoked = true);

        invoked.Should().BeFalse();
        _actions.Received(1).ShowProblem("Unable to navigate to the external binding source.", null);
    }

    [Fact]
    public async Task Selecting_an_external_hook_uses_the_originating_project_for_symbol_navigation()
    {
        using var sut = TaggerSut.Arrange(_output);
        sut.IdeScope.Actions = _actions;
        var implementation = Implementation(null);
        var hook = new ProjectHookBinding(implementation, null!, HookType.BeforeScenario, null, null!);
        sut.TagParser.Parse(Arg.Any<ITextSnapshot>()).Returns(call =>
            ImmutableArray.Create(new DeveroomTag(DeveroomTagTypes.ScenarioHookReference,
                new SnapshotSpan(call.Arg<ITextSnapshot>(), 0, 0), new HookMatchResult(new[] { hook }))));
        var tagger = sut.BuildInitializedFeatureFileTagger();
        var provider = Substitute.For<IDeveroomTaggerProvider>();
        provider.CreateTagger<DeveroomTag>(Arg.Any<ITextBuffer>()).Returns(tagger);
        var navigation = (IBindingNavigation)_actions;
        navigation.NavigateToBindingAsync(implementation, sut.ProjectScope).Returns(Task.FromResult(true));
        ContextMenuItem[]? items = null;
        _actions.When(a => a.ShowContextMenu("Go to hooks", false, Arg.Any<ContextMenuItem[]>()))
            .Do(call => items = call.Arg<ContextMenuItem[]>());

        var command = new GoToHooksCommand(sut.IdeScope, null!, provider);
        command.InvokeCommand(sut.IdeScope.CurrentTextView);
        items.Should().ContainSingle();
        items![0].Command(items[0]);

        await navigation.Received(1).NavigateToBindingAsync(implementation, sut.ProjectScope);
    }
}
