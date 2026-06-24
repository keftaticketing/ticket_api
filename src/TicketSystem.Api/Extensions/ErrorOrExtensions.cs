namespace TicketSystem.Api.Extensions;

using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Contracts.Common;

public static class ErrorOrExtensions
{
    public static ActionResult<T> ToActionResult<T>(this ErrorOr<T> result) =>
        result.IsError ? ToErrorResult<T>(result.Errors) : new OkObjectResult(result.Value);

    public static ActionResult<TResponse> ToErrorActionResult<TResponse>(this ErrorOr<TResponse> result) =>
        result.IsError ? ToErrorResult<TResponse>(result.Errors) : throw new InvalidOperationException();

    public static ActionResult<TResponse> ToErrorActionResult<T, TResponse>(this ErrorOr<T> result) =>
        result.IsError ? ToErrorResult<TResponse>(result.Errors) : throw new InvalidOperationException();

    public static ActionResult<T> ToCreatedResult<T>(
        this ErrorOr<T> result,
        ControllerBase controller,
        string actionName,
        Func<T, object> routeValuesFactory) =>
        result.IsError
            ? ToErrorResult<T>(result.Errors)
            : controller.CreatedAtAction(actionName, routeValuesFactory(result.Value), result.Value);

    public static ActionResult<T> ToCreatedResult<T>(
        this ErrorOr<T> result,
        string location) =>
        result.IsError
            ? ToErrorResult<T>(result.Errors)
            : new CreatedResult(location, result.Value);

    private static ActionResult<T> ToErrorResult<T>(List<Error> errors)
    {
        var primary = errors[0];
        return new ObjectResult(new ErrorResponse(primary.Code, primary.Description))
        {
            StatusCode = MapStatusCode(primary.Type)
        };
    }

    private static int MapStatusCode(ErrorType type) => type switch
    {
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status400BadRequest
    };
}
