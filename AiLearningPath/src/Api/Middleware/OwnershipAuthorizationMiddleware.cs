using System.Security.Claims;
using System.Text.Json;
using AiLearningPath.Application.Authorization;
using AiLearningPath.Domain.Common;

namespace AiLearningPath.Api.Middleware;

/// <summary>
/// Middleware phân quyền theo chủ sở hữu dữ liệu (R2, R14).
/// Với mọi endpoint yêu cầu xác thực (<c>[Authorize]</c>):
/// <list type="bullet">
///   <item>JWT thiếu/không hợp lệ/hết hạn (người dùng chưa xác thực) → <c>401 Unauthorized</c>.</item>
///   <item>Người dùng đã xác thực truy cập tài nguyên của tài khoản khác → <c>403 Forbidden</c>.</item>
/// </list>
/// Body lỗi tuân theo cấu trúc nhất quán <c>{ "error": { code, message, details } }</c>.
/// </summary>
public sealed class OwnershipAuthorizationMiddleware
{
    /// <summary>
    /// Các tên route value/query được hiểu là định danh chủ sở hữu tài nguyên.
    /// Khi xuất hiện, giá trị phải trùng với định danh tài khoản đã xác thực.
    /// </summary>
    private static readonly string[] OwnerIdKeys = { "userId", "ownerId" };

    private readonly RequestDelegate _next;
    private readonly IResourceAuthorizer _authorizer;

    public OwnershipAuthorizationMiddleware(RequestDelegate next, IResourceAuthorizer authorizer)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _authorizer = authorizer ?? throw new ArgumentNullException(nameof(authorizer));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var requiresAuth = endpoint?.Metadata
            .GetMetadata<Microsoft.AspNetCore.Authorization.IAuthorizeData>() is not null;

        // Endpoint công khai (không yêu cầu xác thực) → bỏ qua kiểm tra.
        if (!requiresAuth)
        {
            await _next(context);
            return;
        }

        // JWT không hợp lệ/hết hạn/thiếu → người dùng không được xác thực → 401.
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await WriteErrorAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "UNAUTHORIZED",
                "Yêu cầu cần xác thực hợp lệ. Vui lòng đăng nhập lại.");
            return;
        }

        var requesterId = GetAuthenticatedUserId(context.User);

        // Token thiếu định danh người dùng → coi như không hợp lệ → 401.
        if (requesterId is null)
        {
            await WriteErrorAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "UNAUTHORIZED",
                "Token không chứa định danh người dùng hợp lệ.");
            return;
        }

        // Nếu route/query chỉ định chủ sở hữu tài nguyên, áp dụng kiểm tra ownership → 403.
        if (TryGetResourceOwnerId(context, out var ownerId) &&
            !_authorizer.CanAccess(requesterId.Value, ownerId))
        {
            await WriteErrorAsync(
                context,
                StatusCodes.Status403Forbidden,
                "FORBIDDEN",
                "Bạn không có quyền truy cập tài nguyên của tài khoản khác.");
            return;
        }

        await _next(context);
    }

    private static Guid? GetAuthenticatedUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue("sub")
                  ?? user.FindFirstValue("userId");

        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static bool TryGetResourceOwnerId(HttpContext context, out Guid ownerId)
    {
        foreach (var key in OwnerIdKeys)
        {
            if (context.Request.RouteValues.TryGetValue(key, out var routeValue) &&
                Guid.TryParse(routeValue?.ToString(), out ownerId))
            {
                return true;
            }

            if (context.Request.Query.TryGetValue(key, out var queryValue) &&
                Guid.TryParse(queryValue.ToString(), out ownerId))
            {
                return true;
            }
        }

        ownerId = Guid.Empty;
        return false;
    }

    private static async Task WriteErrorAsync(
        HttpContext context, int statusCode, string code, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var body = ApiErrorResponse.Create(code, message);
        var json = JsonSerializer.Serialize(body, SerializerOptions);
        await context.Response.WriteAsync(json);
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

/// <summary>Tiện ích gắn middleware vào pipeline.</summary>
public static class OwnershipAuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseOwnershipAuthorization(this IApplicationBuilder app) =>
        app.UseMiddleware<OwnershipAuthorizationMiddleware>();
}
