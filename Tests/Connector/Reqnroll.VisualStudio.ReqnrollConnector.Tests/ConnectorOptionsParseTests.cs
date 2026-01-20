using ReqnrollConnector.CommandLineOptions;
using ReqnrollConnector.Utils;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Tests;

public class ConnectorOptionsParseTests
{
    [Fact]
    public void Should_parse_discovery_with_assembly()
    {
        var options = ConnectorOptions.Parse(new[] {"discovery", "../targetAssembly.dll"})
            .Should().BeOfType<DiscoveryOptions>().Subject;

        var assemblyPath = FileDetails.FromPath("../targetAssembly.dll").FullName;
        var connectorFolder = new FileInfo(typeof(Runner).Assembly.Location).Directory!.FullName;

        options.DebugMode.Should().BeFalse();
        options.AssemblyFile.Should().Be(assemblyPath);
        options.ConfigFile.Should().BeNull();
        options.ConnectorFolder.Should().Be(connectorFolder);
    }

    [Fact]
    public void Should_parse_discovery_with_assembly_and_configuration()
    {
        var options = ConnectorOptions.Parse(new[] {"discovery", "../targetAssembly.dll", "../configuration.json"})
            .Should().BeOfType<DiscoveryOptions>().Subject;

        var assemblyPath = FileDetails.FromPath("../targetAssembly.dll").FullName;
        var configPath = FileDetails.FromPath("../configuration.json").FullName;
        var connectorFolder = new FileInfo(typeof(Runner).Assembly.Location).Directory!.FullName;

        options.DebugMode.Should().BeFalse();
        options.AssemblyFile.Should().Be(assemblyPath);
        options.ConfigFile.Should().Be(configPath);
        options.ConnectorFolder.Should().Be(connectorFolder);
    }

    [Fact]
    public void Should_parse_discovery_with_debug_flag()
    {
        var options = ConnectorOptions.Parse(new[] {"discovery", "--debug", "../targetAssembly.dll"})
            .Should().BeOfType<DiscoveryOptions>().Subject;

        options.DebugMode.Should().BeTrue();
    }

    [Fact]
    public void Should_fail_when_command_missing()
    {
        var ex = Assert.Throws<ArgumentException>(() => ConnectorOptions.Parse(Array.Empty<string>()));
        ex.Message.Should().Be("Command is missing!");
    }

    [Fact]
    public void Should_fail_on_invalid_command()
    {
        var ex = Assert.Throws<ArgumentException>(() => ConnectorOptions.Parse(new[] {"xxx"}));
        ex.Message.Should().Be("Invalid command: xxx");
    }

    [Fact]
    public void Should_fail_when_debug_before_command()
    {
        var ex = Assert.Throws<ArgumentException>(() => ConnectorOptions.Parse(new[] {"--debug", "yyy"}));
        ex.Message.Should().Be("Invalid command: --debug");
    }

    [Fact]
    public void Should_fail_when_discovery_arguments_missing()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => ConnectorOptions.Parse(new[] {"discovery"}));
        ex.Message.Should().Be("Usage: discovery <test-assembly-path> [<configuration-file-path>]");
    }
}
