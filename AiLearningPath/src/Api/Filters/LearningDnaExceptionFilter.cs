using AiLearningPath.Application.LearningDna;
using AiLearningPath.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AiLearningPath.Api.Filters;

/// <summary>
/// Ánh xạ các ngoại lệ nghiệp vụ của Learning DNA Engine sang phản hồi lỗi API nhất quán
/// (<c>{ "error": { code, message, details } }</c>):
/// <list type="bullet">
///   <item><see cref="LearningDnaNotFoundException"/> → <c>404 Not Found</c> (R6.4).</item>
/// </list>
/// </summary>
public sealed class LearningDnaExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is LearningDnaNotFoundException notFoundException)
        {
            context.Result = new NotFoundObjectResult(
                ApiErrorResponse.Create("NOT_FOUND", notFoundException.Message))
            {
                StatusCode = StatusCodes.Status404NotFound
            };
            context.ExceptionHandled = true;
        }
    }
}
