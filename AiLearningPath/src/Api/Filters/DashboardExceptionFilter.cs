using AiLearningPath.Application.Progress;
using AiLearningPath.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AiLearningPath.Api.Filters;

/// <summary>
/// Ánh xạ các ngoại lệ nghiệp vụ của Progress Dashboard sang phản hồi lỗi API nhất quán
/// (<c>{ "error": { code, message, details } }</c>):
/// <list type="bullet">
///   <item><see cref="DashboardTaskNotFoundException"/> → <c>404 Not Found</c> (không tìm thấy nhiệm vụ).</item>
/// </list>
/// </summary>
public sealed class DashboardExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case DashboardTaskNotFoundException notFoundException:
                context.Result = new NotFoundObjectResult(
                    ApiErrorResponse.Create("NOT_FOUND", notFoundException.Message))
                {
                    StatusCode = StatusCodes.Status404NotFound
                };
                context.ExceptionHandled = true;
                break;
        }
    }
}
