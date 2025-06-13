using FluentAssertions;
using Moq;
using Reqnroll.VisualStudio.Wizards.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Reqnroll.VisualStudio.Tests.Wizards.Infrastructure
{
    class StubNewProjectDataProvider : NewProjectMetaDataProvider
    {
        private NewProjectMetaRecord _stubData;

        internal StubNewProjectDataProvider(IHttpClient httpClient, IEnvironmentWrapper environmentWrapper, NewProjectMetaRecord stubData)
            : base(httpClient, environmentWrapper)
        {
            _stubData = stubData;
        }
        internal override NewProjectMetaRecord CreateFallBackMetaData()
        {
            return _stubData;
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
        public void RetrieveNewProjectMetaData_InvokesCallback_WithMetadata()
        {
            // Arrange
            var validJson = CreateValidMetadataJson();
            _httpClientMock
                .Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationTokenSource>()))
                .Returns(Task.FromResult(validJson));

            NewProjectMetaData? receivedMetadata = null;
            Action<NewProjectMetaData> callback = md => receivedMetadata = md;

            // Act
            _sut.RetrieveNewProjectMetaData(callback);

            // Assert
            receivedMetadata.Should().NotBeNull();
            receivedMetadata!.TestFrameworks.Should().Contain("NUnit");
        }

        [Fact]
        public void FetchDescriptorsFromReqnrollWebsite_UsesDefaultUrl_WhenNoOverride()
        {
            // Arrange
            var validJson = CreateValidMetadataJson();
            _environmentWrapperMock
                .Setup(x => x.GetEnvironmentVariable(It.IsAny<string>()))
                .Returns((string)null);

            _httpClientMock
                .Setup(x => x.GetStringAsync("https://assets.reqnroll.net/testframeworkmetadata/testframeworks.json", It.IsAny<CancellationTokenSource>()))
                .Returns(Task.FromResult(validJson));

            // Act
            var result = _sut.FetchDescriptorsFromReqnrollWebsite(_httpClientMock.Object);

            // Assert
            result.Should().NotBeNull();
            _httpClientMock.Verify(
                x => x.GetStringAsync("https://assets.reqnroll.net/testframeworkmetadata/testframeworks.json", It.IsAny<CancellationTokenSource>()),
                Times.Once);
        }

        [Fact]
        public void FetchDescriptorsFromReqnrollWebsite_UsesOverrideUrl_WhenSpecified()
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
            var result = _sut.FetchDescriptorsFromReqnrollWebsite(_httpClientMock.Object);

            // Assert
            result.Should().NotBeNull();
            _httpClientMock.Verify(
                x => x.GetStringAsync(customUrl, It.IsAny<CancellationTokenSource>()),
                Times.Once);
        }

        [Fact]
        public void FetchDescriptorsFromReqnrollWebsite_UsesFallback_WhenHttpRequestFails()
        {
            // Arrange
            _httpClientMock
                .Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationTokenSource>()))
                .Throws(new Exception("Connection error"));


            var fallbackData = new NewProjectMetaRecord
            {
                TestFrameworks = new List<FrameworkInfo> { new FrameworkInfo { Label = "FallbackFramework" } }
            };

            var sutWithFallbackMock = new StubNewProjectDataProvider(
                _httpClientMock.Object, _environmentWrapperMock.Object, fallbackData);


            // Act
            var result = sutWithFallbackMock.FetchDescriptorsFromReqnrollWebsite(_httpClientMock.Object);

            // Assert
            result.Should().BeSameAs(fallbackData);
        }

        [Fact]
        public void FetchDescriptorsFromReqnrollWebsite_UsesFallback_WhenDeserializationFails()
        {
            // Arrange
            _httpClientMock
                .Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationTokenSource>()))
                .Returns(Task.FromResult("{ invalid json }"));

            var fallbackData = new NewProjectMetaRecord
            {
                TestFrameworks = new List<FrameworkInfo> { new FrameworkInfo { Label = "FallbackFramework" } }
            };

            var sutWithFallbackMock = new StubNewProjectDataProvider(
                _httpClientMock.Object, _environmentWrapperMock.Object, fallbackData);

            // Act
            var result = sutWithFallbackMock.FetchDescriptorsFromReqnrollWebsite(_httpClientMock.Object);

            // Assert
            result.Should().BeSameAs(fallbackData);
        }

        [Fact]
        public void DependenciesOf_ReturnsCorrectDependencies_WhenFrameworkExists()
        {
            // Arrange
            var validJson = CreateValidMetadataJson();
            _httpClientMock
                .Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationTokenSource>()))
                .Returns(Task.FromResult(validJson));

            _sut.RetrieveNewProjectMetaData(_ => { });

            // Act
            var dependencies = _sut.DependenciesOf("NUnit");

            // Assert
            dependencies.Should().NotBeEmpty();
            dependencies.Should().Contain(d => d.name == "NUnit");
            dependencies.Should().Contain(d => d.name == "NUnit3TestAdapter");
        }

        [Fact]
        public void DependenciesOf_ReturnsEmptyCollection_WhenFrameworkDoesNotExist()
        {
            // Arrange
            var validJson = CreateValidMetadataJson();
            _httpClientMock
                .Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationTokenSource>()))
                .Returns(Task.FromResult(validJson));

            _sut.RetrieveNewProjectMetaData(_ => { });

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
            var result = provider.CreateFallBackMetaData();

            // Assert
            result.Should().NotBeNull();
            result.TestFrameworks.Should().NotBeEmpty();
        }

        private string CreateValidMetadataJson()
        {
            return @"{
                ""testFrameworks"": [
                    {
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