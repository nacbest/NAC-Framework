using FluentAssertions;
using Nac.Jobs.Abstractions;
using Xunit;

namespace Nac.Jobs.Tests.Abstractions;

public class JobDefinitionTests
{
    [Fact]
    public void IsRecurring_WithCronExpression_ReturnsTrue()
    {
        // Arrange
        var job = new JobDefinition
        {
            JobId = "job-1",
            HandlerType = typeof(IJobHandler),
            CronExpression = "0 * * * *"
        };

        // Assert
        job.IsRecurring.Should().BeTrue();
    }

    [Fact]
    public void IsRecurring_WithNullCronExpression_ReturnsFalse()
    {
        // Arrange
        var job = new JobDefinition
        {
            JobId = "job-1",
            HandlerType = typeof(IJobHandler),
            CronExpression = null
        };

        // Assert
        job.IsRecurring.Should().BeFalse();
    }

    [Fact]
    public void Create_WithRequiredProperties_SetsValues()
    {
        // Arrange & Act
        var job = new JobDefinition
        {
            JobId = "my-job",
            HandlerType = typeof(IJobHandler),
            CronExpression = "*/5 * * * *",
            NextRunAt = DateTimeOffset.UtcNow,
            LastRunAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };

        // Assert
        job.JobId.Should().Be("my-job");
        job.HandlerType.Should().Be(typeof(IJobHandler));
        job.CronExpression.Should().Be("*/5 * * * *");
        job.NextRunAt.Should().NotBeNull();
        job.LastRunAt.Should().NotBeNull();
    }

    [Fact]
    public void NextRunAt_DefaultNull()
    {
        var job = new JobDefinition
        {
            JobId = "job-1",
            HandlerType = typeof(IJobHandler)
        };

        job.NextRunAt.Should().BeNull();
    }

    [Fact]
    public void LastRunAt_DefaultNull()
    {
        var job = new JobDefinition
        {
            JobId = "job-1",
            HandlerType = typeof(IJobHandler)
        };

        job.LastRunAt.Should().BeNull();
    }

    [Fact]
    public void HandlerType_StoresCorrectType()
    {
        var job = new JobDefinition
        {
            JobId = "job-1",
            HandlerType = typeof(JobDefinitionTests)
        };

        job.HandlerType.Should().Be(typeof(JobDefinitionTests));
    }
}
