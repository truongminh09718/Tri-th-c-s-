using AiLearningPath.Application.Career;
using AiLearningPath.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AiLearningPath.Api.Filters;

/// <summary>
/// Ánh xạ các ngoại lệ nghiệp vụ của Career Path AI sang phản hồi lỗi API nhất quán
/// (<c>{ "error": { code, message, details } }</c>):
/// <list type="bullet">
///   <item><see cref="CareerValidationException"/> → <c>400 Bad Request</c> (R10.1 — nghề không hỗ trợ).</item>
///   <item><see cref="CareerGenerationException"/> → <c>502 Bad Gateway</c> (lỗi dịch vụ sinh nội dung AI).</item>
/// </list>
/// </summary>
public sealed class CareerExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case CareerValidationException validationException:
                context.Result = new BadRequestObjectResult(
                    ApiErrorResponse.Create(
                        "VALIDATION_ERROR",
                        validationException.Message,
                        BuildDetails(validationException.Validation)));
                context.ExceptionHandled = true;
                break;

            case CareerGenerationException generationException:
                context.Result = new ObjectResult(
                    ApiErrorResponse.Create("CONTENT_GENERATION_ERROR", generationException.Message))
                {
                    StatusCode = StatusCodes.Status502BadGateway
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
