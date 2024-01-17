#nullable disable


namespace Reqnroll.VisualStudio.Tests.ProjectSystem;

public class ReqnrollPackageDetectorTests
{
    private const string ReqnrollPackagePath = @"C:\Users\me\.nuget\packages\reqnroll\2.4.1";
    private const string SpecFlow240PackagePath = @"C:\Users\me\.nuget\packages\reqnroll\2.4.0";
    private const string ReqnrollMsTestPackagePath = @"C:\Users\me\.nuget\packages\reqnroll.mstest\2.4.1";
    private const string SpecSyncPackagePath = @"C:\Users\me\.nuget\packages\specsync.azuredevops.reqnroll.2-4\2.0.0";

    private const string SpecSyncPackagePathSolutionPackages =
        @"C:\MyProject\packages\SpecSync.AzureDevOps.Reqnroll.2-4.2.0.0";

    private const string SpecFlow240PackagePathSolutionPackages = @"C:\MyProject\packages\Reqnroll.2.4.0";
    private readonly MockFileSystem _mockFileSystem = new();

    public ReqnrollPackageDetectorTests()
    {
        _mockFileSystem.AddDirectory(ReqnrollPackagePath);
        _mockFileSystem.AddDirectory(SpecFlow240PackagePath);
        _mockFileSystem.AddDirectory(ReqnrollMsTestPackagePath);
        _mockFileSystem.AddDirectory(SpecSyncPackagePath);
    }

    private static NuGetPackageReference CreateReqnrollPackageRef() =>
        new("Reqnroll", new NuGetVersion("2.4.1", "2.4.1"), ReqnrollPackagePath);

    private static NuGetPackageReference CreateReqnrollMsTestPackageRef() =>
        new("Reqnroll.MsTest", new NuGetVersion("2.4.1", "2.4.1"), ReqnrollMsTestPackagePath);

    private NuGetPackageReference CreateSpecSyncPackageRef(string path = SpecSyncPackagePath) =>
        new("SpecSync.AzureDevOps.Reqnroll.2-4", new NuGetVersion("2.0.0", "2.0.0"), path);

    private ReqnrollPackageDetector CreateSut() => new(_mockFileSystem);

    [Fact]
    public void GetReqnrollPackage_returns_null_for_empty_package_list()
    {
        var sut = CreateSut();

        var result = sut.GetReqnrollPackage(new NuGetPackageReference[0]);

        result.Should().BeNull();
    }

    [Fact]
    public void GetReqnrollPackage_finds_Reqnroll_package_when_listed()
    {
        var sut = CreateSut();

        var result = sut.GetReqnrollPackage(new[]
        {
            CreateReqnrollPackageRef()
        });

        result.Should().NotBeNull();
        result.PackageName.Should().Be("Reqnroll");
    }

    [Fact]
    public void
        GetReqnrollPackage_finds_Reqnroll_package_when_listed_even_if_other_reqnroll_related_packages_are_listed_before()
    {
        var sut = CreateSut();

        var result = sut.GetReqnrollPackage(new[]
        {
            CreateSpecSyncPackageRef(),
            CreateReqnrollMsTestPackageRef(),
            CreateReqnrollPackageRef()
        });

        result.Should().NotBeNull();
        result.PackageName.Should().Be("Reqnroll");
    }

    [Fact]
    public void GetReqnrollPackage_finds_Reqnroll_package_when_only_other_Reqnroll_packages_are_listed()
    {
        var sut = CreateSut();

        var result = sut.GetReqnrollPackage(new[]
        {
            CreateSpecSyncPackageRef(),
            CreateReqnrollMsTestPackageRef()
        });

        result.Should().NotBeNull();
        result.PackageName.Should().Be("Reqnroll");
        result.Version.Should().Be(new NuGetVersion("2.4.1", "2.4.1"));
    }

    [Fact]
    public void GetReqnrollPackage_finds_Reqnroll_package_when_only_Reqnroll_extension_packages_are_listed()
    {
        var sut = CreateSut();

        var result = sut.GetReqnrollPackage(new[]
        {
            CreateSpecSyncPackageRef()
        });

        result.Should().NotBeNull();
        result.PackageName.Should().Be("Reqnroll");
        result.Version.Should().Be(new NuGetVersion("2.4.0", "2.4.0"));
    }

    [Fact]
    public void
        GetReqnrollPackage_finds_Reqnroll_package_within_solution_when_only_Reqnroll_extension_packages_are_listed()
    {
        _mockFileSystem.AddDirectory(SpecFlow240PackagePathSolutionPackages);
        _mockFileSystem.AddDirectory(SpecSyncPackagePathSolutionPackages);
        var sut = CreateSut();

        var result = sut.GetReqnrollPackage(new[]
        {
            CreateSpecSyncPackageRef(SpecSyncPackagePathSolutionPackages)
        });

        result.Should().NotBeNull();
        result.PackageName.Should().Be("Reqnroll");
        result.Version.Should().Be(new NuGetVersion("2.4.0", "2.4.0"));
        result.InstallPath.Should().BeEquivalentTo(SpecFlow240PackagePathSolutionPackages);
    }

    [Fact]
    public void GetReqnrollPackage_returns_null_when_extension_package_has_no_path()
    {
        var sut = CreateSut();

        var result = sut.GetReqnrollPackage(new[]
        {
            CreateSpecSyncPackageRef(null)
        });

        result.Should().BeNull();
    }
}
