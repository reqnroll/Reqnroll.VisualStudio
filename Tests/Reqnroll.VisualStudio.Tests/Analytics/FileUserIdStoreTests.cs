#nullable disable
using NSubstitute;

namespace Reqnroll.VisualStudio.Tests.Analytics;

public class FileUserIdStoreTests
{
    private const string UserId = "491ed5c0-9f25-4c27-941a-19b17cc81c87";
    private IFileSystemForVs fileSystemStub;

    [Fact]
    public void Should_GetUserIdFromFile_WhenFileExists()
    {
        var sut = CreateSut();

        GivenFileExists();
        GivenUserIdStringInFile(UserId);

        string userId = sut.GetUserId();

        userId.Should().Be(UserId);
    }

    [Fact]
    public void Should_PersistNewlyGeneratedUserId_WhenNoUserIdExists()
    {
        var sut = CreateSut();

        GivenFileDoesNotExists();

        string userId = sut.GetUserId();

        userId.Should().NotBeEmpty();
        fileSystemStub.File.Received(1).WriteAllText(FileUserIdStore.UserIdFilePath, userId);
    }


    public FileUserIdStore CreateSut()
    {
        fileSystemStub = Substitute.For<IFileSystemForVs>();
        return new FileUserIdStore(fileSystemStub);
    }

    private void GivenFileExists()
    {
        fileSystemStub.File.Exists(Arg.Any<string>()).Returns(true);
    }

    private void GivenFileDoesNotExists()
    {
        fileSystemStub.File.Exists(Arg.Any<string>()).Returns(false);
        fileSystemStub.Directory.Exists(Arg.Any<string>()).Returns(true);
    }

    private void GivenUserIdStringInFile(string userIdString)
    {
        fileSystemStub.File.ReadAllText(Arg.Any<string>()).Returns(userIdString);
    }
}
