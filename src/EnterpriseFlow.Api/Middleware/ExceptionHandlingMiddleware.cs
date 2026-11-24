using System.Diagnostics;
using System.Net;
using System.Text.Json;
using EnterpriseFlow.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseFlow.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        _logger.LogError(exception, "Unhandled Exception occurred in EnterpriseFlow pipeline. TraceId: {TraceId}", traceId);

        var (statusCode, problemDetails) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                CreateValidationProblemDetails(context, validationEx, traceId)
            ),

            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                CreateProblemDetails(context, HttpStatusCode.BadRequest, "Bad Request", argEx.Message, traceId)
            ),

            UnauthorizedAccessException unauthEx => (
                HttpStatusCode.Unauthorized,
                CreateProblemDetails(context, HttpStatusCode.Unauthorized, "Unauthorized", unauthEx.Message, traceId)
            ),

            UnauthorizedWorkflowActionException workflowEx => (
                HttpStatusCode.Forbidden,
                CreateProblemDetails(context, HttpStatusCode.Forbidden, "Forbidden Workflow Action", workflowEx.Message, traceId)
            ),

            EntityNotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                CreateProblemDetails(context, HttpStatusCode.NotFound, "Resource Not Found", notFoundEx.Message, traceId)
            ),

            KeyNotFoundException keyEx => (
                HttpStatusCode.NotFound,
                CreateProblemDetails(context, HttpStatusCode.NotFound, "Resource Not Found", keyEx.Message, traceId)
            ),

            InvalidStateTransitionException stateEx => (
                HttpStatusCode.Conflict,
                CreateStateTransitionProblemDetails(context, stateEx, traceId)
            ),

            InvalidOperationException opEx => (
                HttpStatusCode.Conflict,
                CreateProblemDetails(context, HttpStatusCode.Conflict, "Conflict", opEx.Message, traceId)
            ),

            _ => (
                HttpStatusCode.InternalServerError,
                CreateProblemDetails(context, HttpStatusCode.InternalServerError, "Internal Server Error", "An unexpected enterprise system error occurred.", traceId)
            )
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(json);
    }

    private static ProblemDetails CreateProblemDetails(
        HttpContext context,
        HttpStatusCode statusCode,
        string title,
        string detail,
        string traceId)
    {
        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = traceId;
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;
        return problemDetails;
    }

    private static ProblemDetails CreateValidationProblemDetails(
        HttpContext context,
        ValidationException validationEx,
        string traceId)
    {
        var validationErrors = validationEx.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        var problemDetails = new ProblemDetails
        {
            Status = (int)HttpStatusCode.BadRequest,
            Title = "Validation Failed",
            Detail = "One or more validation errors occurred.",
            Instance = context.Request.Path
        };

        problemDetails.Extensions["errors"] = validationErrors;
        problemDetails.Extensions["traceId"] = traceId;
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;
        return problemDetails;
    }

    private static ProblemDetails CreateStateTransitionProblemDetails(
        HttpContext context,
        InvalidStateTransitionException stateEx,
        string traceId)
    {
        var problemDetails = new ProblemDetails
        {
            Status = (int)HttpStatusCode.Conflict,
            Title = "Invalid State Machine Transition",
            Detail = stateEx.Message,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["currentStatus"] = stateEx.CurrentStatus.ToString();
        problemDetails.Extensions["targetStatus"] = stateEx.TargetStatus.ToString();
        problemDetails.Extensions["traceId"] = traceId;
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;
        return problemDetails;
    }
}
