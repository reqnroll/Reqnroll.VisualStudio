using Reqnroll.VisualStudio.Wizards.Infrastructure;

namespace Reqnroll.VisualStudio.Tests.Wizards.Infrastructure
{
    class StubNewProjectDataProvider : NewProjectMetaDataProvider
    {
        internal NewProjectMetaRecord Fallback = new NewProjectMetaRecord
        {
            TestFrameworks = new List<FrameworkInfo> { new FrameworkInfo { Tag = "fallbackframework", Label = "FallbackFramework" } },
            DotNetFrameworks = new List<DotNetFrameworkInfo> { new DotNetFrameworkInfo { Tag = "fallbackDotNet", Label = "FallbackDotNetFramework", Default=true} }
        };
        internal StubNewProjectDataProvider(IHttpClient httpClient, IEnvironmentWrapper environmentWrapper)
            : base(httpClient, environmentWrapper)
        {
        }
        internal override NewProjectMetaRecord CreateFallBackMetaDataRecord()
        {
            return Fallback;
        }
    }
    public class NewProjectMetaDataProviderTests
    {
        private readonly Mock<IHttpClient> _httpClientMock;
        private readonly Mock<IEnvironmentWrapper> _environmentWrapperMock;
        private readonly NewProjectMetaDataProvider _sut;

        public NewProjectMetaDataProviderTests()
        {
            _httpClientMock = new Mock<IHttpClient>();
            _environmentWrapperMock = new Mock<IEnvironmentWrapper>();

            _sut = new NewProjectMetaDataProvider(_httpClientMock.Object, _environmentWrapperMock.Object);
        }

        [Fact]
        public async Task RetrieveNewProjectMetaDataAsync_ReturnsMetaData()
        {
            // Arrange
            var validJson = CreateValidMetadataJson();
            _httpClientMock
                .Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationTokenSource>()))
                .Returns(Task.FromResult(validJson));

            NewProjectMetaData? receivedMetadata = null;

            // Act
            receivedMetadata = await _sut.RetrieveNewProjectMetaDataAsync();

            // Assert
            receivedMetadata.Should().NotBeNull();
            receivedMetadata!.TestFrameworks.Should().Contain("Unique");
        }

        [Fact]
        public async Task RetrieveNewProjectMetaDataAsync_UsesDefaultUrl_WhenNoOverride()
        {
            // Arrange
            var validJson = CreateValidMetadataJson();
            _environmentWrapperMock
                .Setup(x => x.GetEnvironmentVariable(It.IsAny<string>()))
                .Returns((string)null!);

            _httpClientMock
                .Setup(x => x.GetStringAsync("https://assets.reqnroll.net/testframeworkmetadata/testframeworks.json", It.IsAny<CancellationTokenSource>()))
                .Returns(Task.FromResult(validJson));

            // Act
            var result = await _sut.RetrieveNewProjectMetaDataAsync();

            // Assert
            result.Should().NotBeNull();
            _httpClientMock.Verify(
                x => x.GetStringAsync("https://assets.reqnroll.net/testframeworkmetadata/testframeworks.json", It.IsAny<CancellationTokenSource>()),
                Times.Once);
        }

        [Fact]
        public async Task RetrieveNewProjectMetaDataAsync_UsesOverrideUrl_WhenSpecified()
        {
            // Arrange
            var customUrl = "https://custom-url/metadata.json";
            var validJson = CreateValidMetadataJson();

            _environmentWrapperMock
                .Setup(x => x.GetEnvironmentVariable("REQNROLL_VISUALSTUDIOEXTENSION_NPW_FRAMEWORKMETADATAENDPOINTURL"))
                .Returns(customUrl);

            _httpClientMock
                .Setup(x => x.GetStringAsync(customUrl, It.IsAny<CancellationTokenSource>()))
                .Returns(Task.FromResult(validJson));

            // Act
            var result = await _sut.RetrieveNewProjectMetaDataAsync();

            // Assert
            result.Should().NotBeNull();
            _httpClientMock.Verify(
                x => x.GetStringAsync(customUrl, It.IsAny<CancellationTokenSource>()),
                Times.Once);
        }

        [Fact]
        public async Task RetrieveNewProjectMetaDataAsync_UsesFallback_WhenHttpRequestFails()
        {
            // Arrange
            _httpClientMock
                .Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationTokenSource>()))
                .Throws(new Exception("Connection error"));


            var sutWithFallbackMock = new StubNewProjectDataProvider(_httpClientMock.Object, _environmentWrapperMock.Object);

            // Act
            var result = await sutWithFallbackMock.RetrieveNewProjectMetaDataAsync();

            // Assert
            result.IsFallback.Should().BeTrue();
            result.Should().BeEquivalentTo(new NewProjectMetaData(sutWithFallbackMock.Fallback, true));
        }

        [Fact]
        public async Task RetrieveNewProjectMetaDataAsync_UsesFallback_WhenDeserializationFails()
        {
            // Arrange
            _httpClientMock
                .Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationTokenSource>()))
                .Returns(Task.FromResult("{ invalid json }"));

             var sutWithFallbackMock = new StubNewProjectDataProvider(_httpClientMock.Object, _environmentWrapperMock.Object);

            // Act
            var result = await sutWithFallbackMock.RetrieveNewProjectMetaDataAsync();

            // Assert
            result.IsFallback.Should().BeTrue();
            result.Should().BeEquivalentTo(new NewProjectMetaData(sutWithFallbackMock.Fallback, true));
        }

        [Fact]
        public async Task DependenciesOf_ReturnsCorrectDependencies_WhenFrameworkExists()
        {
            // Arrange
            var validJson = CreateValidMetadataJson();
            _httpClientMock
                .Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationTokenSource>()))
                .Returns(Task.FromResult(validJson));

            await _sut.RetrieveNewProjectMetaDataAsync();

            // Act
            var dependencies = _sut.DependenciesOf("nunit").ToArray(); // using tag value

            // Assert
            dependencies.Should().NotBeEmpty();
            dependencies.Should().Contain(d => d.name == "NUnit");
            dependencies.Should().Contain(d => d.name == "NUnit3TestAdapter");
        }

        [Fact]
        public async Task DependenciesOf_ReturnsEmptyCollection_WhenFrameworkDoesNotExist()
        {
            // Arrange
            var validJson = CreateValidMetadataJson();
            _httpClientMock
                .Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationTokenSource>()))
                .Returns(Task.FromResult(validJson));

            await _sut.RetrieveNewProjectMetaDataAsync();

            // Act
            var dependencies = _sut.DependenciesOf("NonExistentFramework");

            // Assert
            dependencies.Should().BeEmpty();
        }

        [Fact]
        public void CreateFallBackMetaData_ReturnsValidMetaData_WhenResourceExists()
        {
            // Arrange
            var provider = new NewProjectMetaDataProvider(_httpClientMock.Object, _environmentWrapperMock.Object);

            // Act
            var result = provider.CreateFallBackMetaDataRecord();

            // Assert
            result.Should().NotBeNull();
            result.TestFrameworks.Should().NotBeEmpty();
        }

        private string CreateValidMetadataJson()
        {
            return @"{
                ""testFrameworks"": [
                    {
                        ""tag"": ""nunit"",
                        ""label"": ""NUnit"",
                        ""description"": ""NUnit test framework"",
                        ""url"": ""https://nunit.org"",
                        ""dependencies"": [
                            {
                                ""name"": ""NUnit"",
                                ""version"": ""3.13.2""
                            },
                            {
                                ""name"": ""NUnit3TestAdapter"",
                                ""version"": ""4.0.0""
                            }
                        ]
                    },
                    {
                        ""tag"": ""xunit"",
                        ""label"": ""xUnit"",
                        ""description"": ""xUnit test framework"",
                        ""url"": ""https://xunit.net"",
                        ""dependencies"": [
                            {
                                ""name"": ""xunit"",
                                ""version"": ""2.4.1""
                            },
                            {
                                ""name"": ""xunit.runner.visualstudio"",
                                ""version"": ""2.4.3""
                            }
                        ]
                    },
                    {
                        ""tag"": ""unique"",
                        ""label"": ""Unique"",
                        ""description"": ""Dummy Test Framework Unique To This Test"",
                        ""url"": ""https://xunit.net"",
                        ""dependencies"": [
                            {
                                ""name"": ""xunit"",
                                ""version"": ""2.4.1""
                            },
                            {
                                ""name"": ""xunit.runner.visualstudio"",
                                ""version"": ""2.4.3""
                            }
                        ]
                    }
                ],
                ""dotNetFrameworks"": [
                    {
                        ""label"": "".NET 6.0"",
                        ""tag"": ""net6.0"",
                        ""default"": ""true""
                    }
                ]
            }";
        }
    }
}