using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nac.Core.Results;
using Nac.WebApi.ExceptionHandling;
using Xunit;

namespace Nac.WebApi.Tests.ExceptionHandling;

/// <summary>
/// Tests for the public static ResultToHttpMapper — no mocking required.
/// </summary>
public sealed class ResultToHttpMapperTests
{
    [Fact]
    public void ToActionResult_Success_Returns204NoContent()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var actionResult = ResultToHttpMapper.ToActionResult(result);

        // Assert
        actionResult.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public void ToActionResultT_Success_Returns200WithValue()
    {
        // Arrange
        var result = Result<string>.Success("hello");

        // Act
        var actionResult = ResultToHttpMapper.ToActionResult(result);

        // Assert
        var ok = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be("hello");
    }

    [Fact]
    public void ToActionResult_Invalid_Returns400()
    {
        // Arrange
        var result = Result.Invalid(new ValidationError("Name", "Required"));

        // Act
        var actionResult = ResultToHttpMapper.ToActionResult(result);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void ToActionResult_Invalid_ContainsValidationErrors()
    {
        // Arrange
        var result = Result.Invalid(new ValidationError("Name", "Required"));

        // Act
        var actionResult = ResultToHttpMapper.ToActionResult(result);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        var vpd = obj.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        vpd.Errors.Should().ContainKey("Name");
        vpd.Errors["Name"].Should().Contain("Required");
    }

    [Fact]
    public void ToActionResult_NotFound_Returns404()
    {
        // Arrange
        var result = Result.NotFound("Widget not found");

        // Act
        var actionResult = ResultToHttpMapper.ToActionResult(result);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void ToActionResult_NotFound_ContainsMessage()
    {
        // Arrange
        var result = Result.NotFound("Widget not found");

        // Act
        var actionResult = ResultToHttpMapper.ToActionResult(result);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        var pd = obj.Value.Should().BeOfType<ProblemDetails>().Subject;
        pd.Detail.Should().Contain("Widget not found");
    }

    [Fact]
    public void ToActionResult_Forbidden_Returns403()
    {
        // Arrange
        var result = Result.Forbidden("Access denied");

        // Act
        var actionResult = ResultToHttpMapper.ToActionResult(result);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public void ToActionResult_Conflict_Returns409()
    {
        // Arrange
        var result = Result.Conflict("Already exists");

        // Act
        var actionResult = ResultToHttpMapper.ToActionResult(result);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public void ToActionResult_Error_Returns500()
    {
        // Arrange
        var result = Result.Error("Something went wrong");

        // Act
        var actionResult = ResultToHttpMapper.ToActionResult(result);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public void ToActionResultT_NotFound_Returns404()
    {
        // Arrange
        var result = Result<string>.NotFound("Item missing");

        // Act
        var actionResult = ResultToHttpMapper.ToActionResult(result);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}
