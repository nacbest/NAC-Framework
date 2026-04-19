using FluentAssertions;
using Nac.Jobs.Abstractions;
using Nac.Testing.Fakes;
using Xunit;

namespace Nac.Jobs.Tests.Fakes;

// Stub handler type for testing
file sealed class StubJobHandler : IJobHandler
{
    public Task ExecuteAsync(CancellationToken ct = default) => Task.CompletedTask;
}

public class FakeRecurringJobManagerTests
{
    private readonly FakeRecurringJobManager _manager = new();

    [Fact]
    public async Task AddOrUpdateAsync_AddsNewJob()
    {
        // Act
        await _manager.AddOrUpdateAsync("job-1", typeof(StubJobHandler), "0 * * * *");

        // Assert
        var jobs = await _manager.GetAllAsync();
        jobs.Should().ContainSingle().Which.JobId.Should().Be("job-1");
    }

    [Fact]
    public async Task AddOrUpdateAsync_UpdatesExistingJob()
    {
        // Arrange
        await _manager.AddOrUpdateAsync("job-1", typeof(StubJobHandler), "0 * * * *");

        // Act
        await _manager.AddOrUpdateAsync("job-1", typeof(StubJobHandler), "*/5 * * * *");

        // Assert
        var jobs = await _manager.GetAllAsync();
        jobs.Should().ContainSingle().Which.CronExpression.Should().Be("*/5 * * * *");
    }

    [Fact]
    public async Task RemoveAsync_RemovesJob()
    {
        // Arrange
        await _manager.AddOrUpdateAsync("job-1", typeof(StubJobHandler), "0 * * * *");

        // Act
        await _manager.RemoveAsync("job-1");

        // Assert
        var jobs = await _manager.GetAllAsync();
        jobs.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveAsync_NonExistentJob_NoError()
    {
        // Act
        var act = () => _manager.RemoveAsync("nonexistent");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllJobs()
    {
        // Arrange
        await _manager.AddOrUpdateAsync("job-1", typeof(StubJobHandler), "0 * * * *");
        await _manager.AddOrUpdateAsync("job-2", typeof(StubJobHandler), "*/10 * * * *");

        // Act
        var jobs = await _manager.GetAllAsync();

        // Assert
        jobs.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_EmptyWhenNoJobs()
    {
        // Act
        var jobs = await _manager.GetAllAsync();

        // Assert
        jobs.Should().BeEmpty();
    }

    [Fact]
    public async Task AddedJob_HasCorrectCronExpression()
    {
        // Act
        await _manager.AddOrUpdateAsync("job-1", typeof(StubJobHandler), "30 2 * * 1");

        // Assert
        var jobs = await _manager.GetAllAsync();
        var job = jobs.Should().ContainSingle().Subject;
        job.CronExpression.Should().Be("30 2 * * 1");
        job.IsRecurring.Should().BeTrue();
    }

    [Fact]
    public async Task Reset_ClearsAllJobs()
    {
        // Arrange
        await _manager.AddOrUpdateAsync("job-1", typeof(StubJobHandler), "0 * * * *");
        await _manager.AddOrUpdateAsync("job-2", typeof(StubJobHandler), "*/5 * * * *");

        // Act
        _manager.Reset();

        // Assert
        var jobs = await _manager.GetAllAsync();
        jobs.Should().BeEmpty();
    }
}
