using System.Net;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using HRMS.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HRMS.API.Tests;

public sealed class ErrorHandlingMiddlewareTests
{
    private static async Task<(int status, JsonElement body)> Run(Exception? toThrow)
    {
        var middleware = new ErrorHandlingMiddleware(
            _ => toThrow is null ? Task.CompletedTask : throw toThrow,
            NullLogger<ErrorHandlingMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var text = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var body = string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement;
        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task NoException_PassesThrough()
    {
        var (status, _) = await Run(null);
        Assert.Equal(200, status);
    }

    [Fact]
    public async Task ValidationException_Returns400WithErrors()
    {
        var ex = new ValidationException(new[] { new ValidationFailure("Email", "Email is required") });
        var (status, body) = await Run(ex);

        Assert.Equal((int)HttpStatusCode.BadRequest, status);
        Assert.Equal("Validation failed", body.GetProperty("message").GetString());
        Assert.Equal("Email is required", body.GetProperty("errors")[0].GetString());
    }

    [Theory]
    [InlineData(typeof(UnauthorizedAccessException), 401)]
    [InlineData(typeof(KeyNotFoundException), 404)]
    [InlineData(typeof(InvalidOperationException), 400)]
    [InlineData(typeof(Exception), 500)]
    public async Task MapsExceptionToStatusCode(Type exceptionType, int expected)
    {
        var (status, _) = await Run((Exception)Activator.CreateInstance(exceptionType, "boom")!);
        Assert.Equal(expected, status);
    }

    [Fact]
    public async Task InternalError_DoesNotLeakExceptionMessage()
    {
        var (_, body) = await Run(new Exception("connection string password=123"));
        Assert.DoesNotContain("password", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task BusinessError_ShowsMessageToClient()
    {
        var (_, body) = await Run(new InvalidOperationException("Leave request overlaps with existing leave"));
        Assert.Equal("Leave request overlaps with existing leave", body.GetProperty("message").GetString());
    }
}
