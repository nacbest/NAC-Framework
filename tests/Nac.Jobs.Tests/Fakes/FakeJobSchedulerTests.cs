using FluentAssertions;
using Nac.Jobs.Abstractions;
using Nac.Testing.Fakes;
using Xunit;

namespace Nac.Jobs.Tests.Fakes;

// Stub job handler for testing
file sealed class TestJobHandler : IJobHandler
{
    public Task ExecuteAsync(CancellationToken ct = default) => Task.CompletedTask;
}

public class FakeJobSchedulerTests
{
    private readonly FakeJobScheduler _scheduler = new();

    [Fact]
    public async Task EnqueueAsync_CapturesJobType()
    {
        // Act
        await _scheduler.EnqueueAsync<TestJobHandler>();

        // Assert
        _scheduler.ScheduledJobs.Should().ContainSingle()
            .Which.HandlerType.Should().Be(typeof(TestJobHandler));
    }

    [Fact]
    public async Task EnqueueAsync_ReturnsUniqueJobId()
    {
        // Act
        var id1 = await _scheduler.EnqueueAsync<TestJobHandler>();
        var id2 = await _scheduler.EnqueueAsync<TestJobHandler>();

        // Assert
        id1.Should().NotBe(id2);
    }

    [Fact]
    public async Task EnqueueAsync_NullDelay()
    {
        // Act
        await _scheduler.EnqueueAsync<TestJobHandler>();

        // Assert
        _scheduler.ScheduledJobs.Should().ContainSingle()
            .Which.Delay.Should().BeNull();
    }

    [Fact]
    public async Task ScheduleAsync_CapturesDelay()
    {
        // Arrange
        var delay = TimeSpan.FromMinutes(5);

        // Act
        await _scheduler.ScheduleAsync<TestJobHandler>(delay);

        // Assert
        _scheduler.ScheduledJobs.Should().ContainSingle()
            .Which.Delay.Should().Be(delay);
    }

    [Fact]
    public async Task ScheduleAsync_ReturnsUniqueJobId()
    {
        // Act
        var id1 = await _scheduler.ScheduleAsync<TestJobHandler>(TimeSpan.FromSeconds(1));
        var id2 = await _scheduler.ScheduleAsync<TestJobHandler>(TimeSpan.FromSeconds(2));

        // Assert
        id1.Should().NotBe(id2);
    }

    [Fact]
    public async Task Reset_ClearsAllJobs()
    {
        // Arrange
        await _scheduler.EnqueueAsync<TestJobHandler>();
        await _scheduler.ScheduleAsync<TestJobHandler>(TimeSpan.FromMinutes(1));

        // Act
        _scheduler.Reset();

        // Assert
        _scheduler.ScheduledJobs.Should().BeEmpty();
    }

    [Fact]
    public async Task MultipleEnqueues_IncrementCounter()
    {
        // Act
        var id1 = await _scheduler.EnqueueAsync<TestJobHandler>();
        var id2 = await _scheduler.EnqueueAsync<TestJobHandler>();
        var id3 = await _scheduler.EnqueueAsync<TestJobHandler>();

        // Assert
        _scheduler.ScheduledJobs.Should().HaveCount(3);
        id1.Should().Be("fake-job-1");
        id2.Should().Be("fake-job-2");
        id3.Should().Be("fake-job-3");
    }
}
