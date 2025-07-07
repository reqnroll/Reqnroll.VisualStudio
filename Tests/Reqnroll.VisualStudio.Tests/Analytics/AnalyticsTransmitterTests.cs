#nullable disable
using System;
using System.Linq;

namespace Reqnroll.VisualStudio.Tests.Analytics;

public class AnalyticsTransmitterTests
{
    private IAnalyticsTransmitterSink analyticsTransmitterSinkStub;
    private IEnableAnalyticsChecker enableAnalyticsCheckerStub;

    [Fact]
    public void Should_NotSendAnalytics_WhenDisabled()
    {
        var sut = CreateSut();
        GivenAnalyticsDisabled();

        sut.TransmitEvent(Substitute.For<IAnalyticsEvent>());

        enableAnalyticsCheckerStub.Received(1).IsEnabled();
        analyticsTransmitterSinkStub.DidNotReceive().TransmitEvent(Arg.Any<IAnalyticsEvent>());
    }

    [Fact]
    public void Should_SendAnalytics_WhenEnabled()
    {
        var sut = CreateSut();
        GivenAnalyticsEnabled();

        sut.TransmitEvent(Substitute.For<IAnalyticsEvent>());

        enableAnalyticsCheckerStub.Received(1).IsEnabled();
        analyticsTransmitterSinkStub.Received(1).TransmitEvent(Arg.Any<IAnalyticsEvent>());
    }

    [Theory]
    [InlineData("Extension loaded")]
    [InlineData("Extension installed")]
    [InlineData("100 day usage")]
    public void Should_TransmitEvents(string eventName)
    {
        var sut = CreateSut();
        GivenAnalyticsEnabled();

        sut.TransmitEvent(new GenericEvent(eventName));

        analyticsTransmitterSinkStub.Received(1).TransmitEvent(Arg.Is<IAnalyticsEvent>(ae => ae.EventName == eventName));
    }

    private void GivenAnalyticsEnabled()
    {
        enableAnalyticsCheckerStub.IsEnabled().Returns(true);
    }

    private void GivenAnalyticsDisabled()
    {
        enableAnalyticsCheckerStub.IsEnabled().Returns(false);
    }

    public AnalyticsTransmitter CreateSut()
    {
        analyticsTransmitterSinkStub = Substitute.For<IAnalyticsTransmitterSink>();
        enableAnalyticsCheckerStub = Substitute.For<IEnableAnalyticsChecker>();
        return new AnalyticsTransmitter(analyticsTransmitterSinkStub, enableAnalyticsCheckerStub);
    }
}
