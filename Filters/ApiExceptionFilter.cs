using System;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using stockmind.Commons.Responses;

namespace stockmind.Filters;

public class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.ExceptionHandled)
        {
            return;
        }

        var exception = context.Exception;

        var (statusCode, response) = exception switch
        {
            BizAuthenticationException authEx => (
                StatusCodes.Status401Unauthorized,
                BuildErrorResponse(authEx, ErrorCode4xx.Unauthorized)),

            BizAuthorizationException authzEx => (
                StatusCodes.Status403Forbidden,
                BuildErrorResponse(authzEx, ErrorCode4xx.Forbidden)),

            BizNotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                BuildErrorResponse(notFoundEx, notFoundEx.Error)),

            BizDataAlreadyExistsException dataExistsEx => (
                StatusCodes.Status409Conflict,
                BuildErrorResponse(dataExistsEx, dataExistsEx.Error)),

            BizException bizEx => (
                StatusCodes.Status400BadRequest,
                BuildErrorResponse(bizEx, bizEx.Error)),

            _ => HandleUnknownException(exception)
        };

        context.Result = new ObjectResult(response)
        {
            StatusCode = statusCode
        };

        context.ExceptionHandled = true;
    }

    private (int StatusCode, ErrorResponseModel Response) HandleUnknownException(Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception encountered.");

        var response = new ErrorResponseModel(
            ErrorCode5xx.InternalServerError.Code,
            ErrorCode5xx.InternalServerError.MessageTemplate,
            new[] { exception.Message });

        return (StatusCodes.Status500InternalServerError, response);
    }

    private static ErrorResponseModel BuildErrorResponse(BizException exception, IResponseCode responseCode)
    {
        var message = FormatMessage(responseCode, exception.Params?.ToArray());
        return new ErrorResponseModel(responseCode.Code, message, exception.Params);
    }

    private static string FormatMessage(IResponseCode responseCode, string[]? parameters)
    {
        if (parameters is null || parameters.Length == 0)
        {
            return responseCode.MessageTemplate;
        }

        try
        {
            return string.Format(CultureInfo.CurrentCulture, responseCode.MessageTemplate, parameters);
        }
        catch (FormatException)
        {
            return responseCode.MessageTemplate;
        }
    }
}
