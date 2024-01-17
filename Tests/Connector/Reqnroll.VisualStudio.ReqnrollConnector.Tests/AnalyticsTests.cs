using System;
using System.Linq;

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
        var serialized = JsonSerialization.SerializeObject(container);
        var deserialized = JsonSerialization.DeserializeObject<AnalyticsContainer>(serialized);

        //assert
        deserialized.Should().BeOfType(typeof(Some<AnalyticsContainer>));
        var containerProperties = container;
        var deserializedProperties = (deserialized as Some<AnalyticsContainer>)?.Content;
        deserializedProperties.Should().BeEquivalentTo(containerProperties);
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
        var serialized = JsonSerialization.SerializeObject(container);
        var deserialized = JsonSerialization.DeserializeObject<Dictionary<string, string>>(serialized);

        //assert
        deserialized.Should().BeOfType(typeof(Some<Dictionary<string, string>>));
        (deserialized as Some<Dictionary<string, string>>)?.Content.Should().BeEquivalentTo(
            new Dictionary<string, string>
            {
                ["k1"] = "v1", ["k2"] = "v2"
            });
    }
}
