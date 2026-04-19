using FluentAssertions;
using Nac.Testing.Fakes;
using Nac.Testing.Tests.TestHelpers;
using Xunit;

namespace Nac.Testing.Tests.Fakes;

public class FakeSenderTests
{
    [Fact]
    public async Task Send_ConfiguredResponse_Returns()
    {
        var sender = new FakeSender();
        sender.Setup<SampleRequest, string>("hello");
        var request = new SampleRequest("test");

        var result = await sender.SendAsync(request);

        result.Should().Be("hello");
    }

    [Fact]
    public async Task Send_NoSetup_Throws()
    {
        var sender = new FakeSender();
        var request = new SampleRequest("test");

        Func<Task> act = async () => await sender.SendAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*SampleRequest*");
    }

    [Fact]
    public async Task Send_RecordsSentRequests()
    {
        var sender = new FakeSender();
        sender.Setup<SampleRequest, string>("response");
        var request = new SampleRequest("input");

        await sender.SendAsync(request);

        sender.SentRequests.Should().ContainSingle().Which.Should().Be(request);
    }

    [Fact]
    public async Task Send_MultipleRequests_TracksAll()
    {
        var sender = new FakeSender();
        sender.Setup<SampleRequest, string>("r1");
        var r1 = new SampleRequest("a");
        var r2 = new SampleRequest("b");

        await sender.SendAsync(r1);
        await sender.SendAsync(r2);

        sender.SentRequests.Should().HaveCount(2);
        sender.SentRequests[0].Should().Be(r1);
        sender.SentRequests[1].Should().Be(r2);
    }
}
