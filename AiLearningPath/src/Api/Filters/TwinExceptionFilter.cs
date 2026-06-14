using AiLearningPath.Application.Twin;
using AiLearningPath.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AiLearningPath.Api.Filters;

/// <summary>
/// Ánh xạ các ngoại lệ nghiệp vụ của Academic Twin sang phản hồi lỗi API nhất quán
/// (<c>{ "error": { code, message, details } }</c>):
/// <list type="bullet">
///   <item><see cref="TwinPrerequisitesException"/> → <c>400 Bad Request</c> (R9.4).</item>
/// </list>
/// </summary>
public sealed class TwinExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is TwinPrerequisitesException prerequisitesException)
        {
            context.Result = new BadRequestObjectResult(
                ApiErrorResponse.Create(
                    "VALIDATION_ERROR",
                    prerequisitesException.Message,
                    BuildDetails(prerequisitesException.Validation)));
            context.ExceptionHandled = true;
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
