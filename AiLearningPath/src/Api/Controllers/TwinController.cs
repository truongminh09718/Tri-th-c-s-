using AiLearningPath.Application.Twin;
using AiLearningPath.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiLearningPath.Api.Controllers;

/// <summary>
/// Endpoint mô phỏng AI Academic Twin — dự đoán xác suất đạt mục tiêu theo thời lượng học (R9).
/// Mọi route đều gắn <c>{userId}</c> để áp dụng ownership-based authorization (R14):
/// middleware <c>OwnershipAuthorizationMiddleware</c> đảm bảo người gọi chỉ mô phỏng trên
/// dữ liệu của chính mình (401 nếu chưa xác thực, 403 nếu truy cập tài khoản khác).
/// </summary>
[ApiController]
[Authorize]
[Route("api/users/{userId:guid}/twin")]
[Produces("application/json")]
public sealed class TwinController : ControllerBase
{
    private readonly IAcademicTwinService _twinService;

    public TwinController(IAcademicTwinService twinService)
    {
        _twinService = twinService ?? throw new ArgumentNullException(nameof(twinService));
    }

    /// <summary>
    /// Mô phỏng kết quả với một thời lượng học mỗi ngày (R9.1): trả về xác suất đạt mục tiêu
    /// ước lượng tương ứng với thời lượng đó, tính trên dữ liệu mới nhất của sinh viên (R9.3).
    /// </summary>
    [HttpPost("simulate")]
    [ProducesResponseType(typeof(PredictionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Simulate(
        Guid userId, [FromBody] SimulateRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(ApiErrorResponse.Create(
                "VALIDATION_ERROR", "Yêu cầu mô phỏng không được để trống."));
        }

        var prediction = await _twinService.SimulateAsync(userId, request.HoursPerDay, cancellationToken);
        return Ok(PredictionResponse.From(prediction));
    }

    /// <summary>
    /// Mô phỏng kết quả với nhiều mức thời lượng học mỗi ngày (R9.2): trả về đúng một dự đoán
    /// cho mỗi mức, với xác suất đơn điệu không giảm khi thời lượng tăng.
    /// </summary>
    [HttpPost("simulate-range")]
    [ProducesResponseType(typeof(IReadOnlyList<PredictionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SimulateRange(
        Guid userId, [FromBody] SimulateRangeRequest request, CancellationToken cancellationToken)
    {
        if (request is null || request.HoursOptions is null)
        {
            return BadRequest(ApiErrorResponse.Create(
                "VALIDATION_ERROR", "Danh sách mức thời lượng không được để trống."));
        }

        var predictions = await _twinService.SimulateRangeAsync(
            userId, request.HoursOptions, cancellationToken);

        var response = predictions.Select(PredictionResponse.From).ToList();
        return Ok(response);
    }
}

/// <summary>Body yêu cầu mô phỏng với một mức thời lượng (R9.1).</summary>
/// <param name="HoursPerDay">Số giờ học mỗi ngày dùng để mô phỏng.</param>
public sealed record SimulateRequest(double HoursPerDay);

/// <summary>Body yêu cầu mô phỏng với nhiều mức thời lượng (R9.2).</summary>
/// <param name="HoursOptions">Danh sách các mức thời lượng học mỗi ngày cần mô phỏng.</param>
public sealed record SimulateRangeRequest(IReadOnlyList<double> HoursOptions);

/// <summary>Phản hồi dự đoán xác suất đạt mục tiêu cho một mức thời lượng (R9.1, R9.2).</summary>
/// <param name="HoursPerDay">Số giờ học mỗi ngày được mô phỏng.</param>
/// <param name="SuccessProbability">Xác suất đạt mục tiêu ước lượng, trong khoảng [0, 1].</param>
public sealed record PredictionResponse(double HoursPerDay, double SuccessProbability)
{
    public static PredictionResponse From(Prediction prediction) =>
        new(prediction.HoursPerDay, prediction.SuccessProbability);
}
