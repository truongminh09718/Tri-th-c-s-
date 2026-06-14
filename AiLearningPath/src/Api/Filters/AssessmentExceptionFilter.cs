using AiLearningPath.Application.Assessments;
using AiLearningPath.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AiLearningPath.Api.Filters;

/// <summary>
/// Ánh xạ các ngoại lệ nghiệp vụ của Assessment sang phản hồi lỗi API nhất quán
/// (<c>{ "error": { code, message, details } }</c>):
/// <list type="bullet">
///   <item><see cref="AssessmentValidationException"/> → <c>400 Bad Request</c> (R5.4, R4.4).</item>
///   <item><see cref="AssessmentNotFoundException"/> → <c>404 Not Found</c>.</item>
///   <item><see cref="AssessmentGenerationException"/> → <c>502 Bad Gateway</c> (lỗi dịch vụ sinh nội dung AI).</item>
/// </list>
/// </summary>
public sealed class AssessmentExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case AssessmentValidationException validationException:
                context.Result = new BadRequestObjectResult(
                    ApiErrorResponse.Create(
                        "VALIDATION_ERROR",
                        validationException.Message,
                        BuildDetails(validationException.Validation)));
                context.ExceptionHandled = true;
                break;

            case AssessmentNotFoundException notFoundException:
                context.Result = new NotFoundObjectResult(
                    ApiErrorResponse.Create("NOT_FOUND", notFoundException.Message))
                {
                    StatusCode = StatusCodes.Status404NotFound
                };
                context.ExceptionHandled = true;
                break;

            case AssessmentGenerationException generationException:
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
