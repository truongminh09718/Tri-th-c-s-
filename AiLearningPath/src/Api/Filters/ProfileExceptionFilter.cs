using AiLearningPath.Application.Profiles;
using AiLearningPath.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AiLearningPath.Api.Filters;

/// <summary>
/// Ánh xạ các ngoại lệ nghiệp vụ của Profile sang phản hồi lỗi API nhất quán
/// (<c>{ "error": { code, message, details } }</c>):
/// <list type="bullet">
///   <item><see cref="ProfileValidationException"/> → <c>400 Bad Request</c> (R3.4, R4.3, R4.4).</item>
///   <item><see cref="ProfileNotFoundException"/> → <c>404 Not Found</c> (R3.2).</item>
/// </list>
/// </summary>
public sealed class ProfileExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case ProfileValidationException validationException:
                context.Result = new BadRequestObjectResult(
                    ApiErrorResponse.Create(
                        "VALIDATION_ERROR",
                        validationException.Message,
                        BuildDetails(validationException.Validation)));
                context.ExceptionHandled = true;
                break;

            case ProfileNotFoundException notFoundException:
                context.Result = new NotFoundObjectResult(
                    ApiErrorResponse.Create("NOT_FOUND", notFoundException.Message))
                {
                    StatusCode = StatusCodes.Status404NotFound
                };
                context.ExceptionHandled = true;
                break;
        }
    }

    private static IReadOnlyDictionary<string, object>? BuildDetails(ValidationResult validation)
    {
        if (validation.Errors.Count <= 1)
        {
            return null;
        }

        return new Dictionary<string, object>
        {
            ["errors"] = validation.Errors.ToArray()
        };
    }
}
