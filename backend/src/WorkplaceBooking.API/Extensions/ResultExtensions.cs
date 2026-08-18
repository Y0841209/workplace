using Ardalis.Result;
using Microsoft.AspNetCore.Mvc;

namespace WorkplaceBooking.Api.Extensions;

public static class ResultExtensions
{
    public static ActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new OkResult();

        return result.Status switch
        {
            ResultStatus.Invalid => new BadRequestObjectResult(new ValidationProblemDetails
            {
                Errors = result.ValidationErrors.ToDictionary(
                    e => e.Identifier,
                    e => new[] { e.ErrorMessage })
            }),
            ResultStatus.NotFound => new NotFoundObjectResult(new ProblemDetails
            {
                Title = "Not Found",
                Detail = result.Errors.FirstOrDefault()?.Message ?? "Resource not found"
            }),
            ResultStatus.Conflict => new ConflictObjectResult(new ProblemDetails
            {
                Title = "Conflict",
                Detail = result.Errors.FirstOrDefault()?.Message ?? "Conflict occurred"
            }),
            ResultStatus.Forbidden => new ForbidResult(),
            ResultStatus.Unauthorized => new UnauthorizedResult(),
            ResultStatus.Error => new ObjectResult(new ProblemDetails
            {
                Title = "Error",
                Detail = result.Errors.FirstOrDefault()?.Message ?? "An error occurred"
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },
            _ => new ObjectResult(new ProblemDetails
            {
                Title = "Error",
                Detail = "An unexpected error occurred"
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
    }

    public static ActionResult<T> ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        return result.Status switch
        {
            ResultStatus.Invalid => new BadRequestObjectResult(new ValidationProblemDetails
            {
                Errors = result.ValidationErrors.ToDictionary(
                    e => e.Identifier,
                    e => new[] { e.ErrorMessage })
            }),
            ResultStatus.NotFound => new NotFoundObjectResult(new ProblemDetails
            {
                Title = "Not Found",
                Detail = result.Errors.FirstOrDefault()?.Message ?? "Resource not found"
            }),
            ResultStatus.Conflict => new ConflictObjectResult(new ProblemDetails
            {
                Title = "Conflict",
                Detail = result.Errors.FirstOrDefault()?.Message ?? "Conflict occurred"
            }),
            ResultStatus.Forbidden => new ForbidResult(),
            ResultStatus.Unauthorized => new UnauthorizedResult(),
            ResultStatus.Error => new ObjectResult(new ProblemDetails
            {
                Title = "Error",
                Detail = result.Errors.FirstOrDefault()?.Message ?? "An error occurred"
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },
            _ => new ObjectResult(new ProblemDetails
            {
                Title = "Error",
                Detail = "An unexpected error occurred"
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
    }
}