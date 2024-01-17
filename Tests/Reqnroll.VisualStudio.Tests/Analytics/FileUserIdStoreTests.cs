#nullable disable
namespace Reqnroll.VisualStudio.Tests.Analytics;

public class FileUserIdStoreTests
{
    private const string UserId = "491ed5c0-9f25-4c27-941a-19b17cc81c87";
    private Mock<IFileSystem> fileSystemStub;

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
        fileSystemStub.Verify(fileSystem => fileSystem.File.WriteAllText(FileUserIdStore.UserIdFilePath, userId),
            Times.Once());
    }


    public FileUserIdStore CreateSut()
    {
        fileSystemStub = new Mock<IFileSystem>();
        return new FileUserIdStore(fileSystemStub.Object);
    }

    private void GivenFileExists()
    {
        fileSystemStub.Setup(fileSystem => fileSystem.File.Exists(It.IsAny<string>())).Returns(true);
    }

    private void GivenFileDoesNotExists()
    {
        fileSystemStub.Setup(fileSystem => fileSystem.File.Exists(It.IsAny<string>())).Returns(false);
        fileSystemStub.Setup(fileSystem => fileSystem.Directory.Exists(It.IsAny<string>())).Returns(true);
    }

    private void GivenUserIdStringInFile(string userIdString)
    {
        fileSystemStub.Setup(fileSystem => fileSystem.File.ReadAllText(It.IsAny<string>())).Returns(userIdString);
    }
}
