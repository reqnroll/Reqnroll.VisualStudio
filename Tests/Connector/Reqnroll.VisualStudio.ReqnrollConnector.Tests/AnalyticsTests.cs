using System;
using System.Linq;
using ReqnrollConnector.Utils;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Tests;

public class AnalyticsTests
{
    [Fact]
    public void AnalyticsContainer_is_serializable()
    {
        //arrange
        var container = new AnalyticsContainer();
        container.AddAnalyticsProperty("k1", "v1");
        container.AddAnalyticsProperty("k2", "v2");

        //act
        var serialized = JsonSerialization.SerializeObjectCamelCase(container);
        var deserialized = ApprovalTestBase.DeserializeObject<AnalyticsContainer>(serialized);

        //assert
        deserialized.Should().NotBeNull();
        var deserializedProperties = deserialized;
        deserializedProperties.Should().BeEquivalentTo(container);
        deserializedProperties.Should().Contain(
            new KeyValuePair<string, string>("k1", "v1"),
            new KeyValuePair<string, string>("k2", "v2")
        );
    }

    [Fact]
    public void AnalyticsContainer_is_deserializable_into_dictionary()
    {
        //arrange
        var container = new AnalyticsContainer();
        container.AddAnalyticsProperty("k1", "v1");
        container.AddAnalyticsProperty("k2", "v2");

        //act
        var serialized = JsonSerialization.SerializeObjectCamelCase(container);
        var deserialized = ApprovalTestBase.DeserializeObject<Dictionary<string, string>>(serialized);

        //assert
        deserialized.Should().NotBeNull();
        deserialized.Should().BeEquivalentTo(
            new Dictionary<string, string>
            {
                ["k1"] = "v1", ["k2"] = "v2"
            });
    }
}
