using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Nac.WebApi.ExceptionHandling;
using Xunit;

namespace Nac.WebApi.Tests.ExceptionHandling;

/// <summary>
/// Tests for NacExceptionHandler — accessible via InternalsVisibleTo.
/// Uses DefaultHttpContext with MemoryStream body so responses can be inspected.
/// Logger uses NullLogger to avoid NSubstitute proxy restrictions on internal types.
/// </summary>
public sealed class NacExceptionHandlerTests
{
    private static NacExceptionHandler BuildHandler()
    {
        // NSubstitute cannot proxy ILogger<NacExceptionHandler> because the type argument
        // is internal and Microsoft.Extensions.Logging.Abstractions is strong-named.
        // NullLogger is the correct lightweight substitute here.
        var logger = NullLogger<NacExceptionHandler>.Instance;
        return new NacExceptionHandler(logger);
    }

    private static DefaultHttpContext BuildHttpContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    private static async Task<ProblemDetails?> ReadProblemDetails(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(response.Body).ReadToEndAsync();
        return JsonSerializer.Deserialize<ProblemDetails>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private static async Task<ValidationProblemDetails?> ReadValidationProblemDetails(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(response.Body).ReadToEndAsync();
        return JsonSerializer.Deserialize<ValidationProblemDetails>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    [Fact]
    public async Task TryHandle_ValidationException_Returns400WithFieldErrors()
    {
        // Arrange
        var handler = BuildHandler();
        var ctx = BuildHttpContext();
        var failures = new[] { new ValidationFailure("Name", "Name is required") };
        var ex = new ValidationException(failures);

        // Act
        var handled = await handler.TryHandleAsync(ctx, ex, CancellationToken.None);

        // Assert
        handled.Should().BeTrue();
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var problem = await ReadValidationProblemDetails(ctx.Response);
        problem!.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public async Task TryHandle_UnauthorizedAccessException_Returns401()
    {
        // Arrange
        var handler = BuildHandler();
        var ctx = BuildHttpContext();

        // Act
        var handled = await handler.TryHandleAsync(ctx, new UnauthorizedAccessException(), CancellationToken.None);

        // Assert
        handled.Should().BeTrue();
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task TryHandle_KeyNotFoundException_Returns404()
    {
        // Arrange
        var handler = BuildHandler();
        var ctx = BuildHttpContext();

        // Act
        var handled = await handler.TryHandleAsync(ctx, new KeyNotFoundException(), CancellationToken.None);

        // Assert
        handled.Should().BeTrue();
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task TryHandle_ArgumentException_Returns400()
    {
        // Arrange
        var handler = BuildHandler();
        var ctx = BuildHttpContext();

        // Act
        var handled = await handler.TryHandleAsync(ctx, new ArgumentException("bad arg"), CancellationToken.None);

        // Assert
        handled.Should().BeTrue();
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task TryHandle_GenericException_Returns500()
    {
        // Arrange
        var handler = BuildHandler();
        var ctx = BuildHttpContext();

        // Act
        var handled = await handler.TryHandleAsync(ctx, new Exception("boom"), CancellationToken.None);

        // Assert
        handled.Should().BeTrue();
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task TryHandle_GenericException_DoesNotLeakDetails()
    {
        // Arrange — exception message must NOT appear in the response body
        var handler = BuildHandler();
        var ctx = BuildHttpContext();
        var secret = "super-secret-internal-message";

        // Act
        await handler.TryHandleAsync(ctx, new Exception(secret), CancellationToken.None);

        // Assert
        var problem = await ReadProblemDetails(ctx.Response);
        problem!.Detail.Should().Be("An unexpected error occurred.");
        problem.Detail.Should().NotContain(secret);
    }

    [Fact]
    public async Task TryHandle_AlwaysReturnsTrue()
    {
        // Arrange — handler must always signal it handled the exception
        var handler = BuildHandler();

        foreach (var ex in new Exception[]
        {
            new ValidationException([new ValidationFailure("F", "e")]),
            new UnauthorizedAccessException(),
            new KeyNotFoundException(),
            new ArgumentException("x"),
            new InvalidOperationException("y"),
        })
        {
            var ctx = BuildHttpContext();
            var result = await handler.TryHandleAsync(ctx, ex, CancellationToken.None);
            result.Should().BeTrue(because: $"{ex.GetType().Name} should always be handled");
        }
    }
}
