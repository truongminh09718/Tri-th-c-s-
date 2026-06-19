using AiLearningPath.Application.Ai;
using AiLearningPath.Domain.Entities;
using AiLearningPath.Infrastructure.Persistence;

namespace AiLearningPath.Infrastructure.Services;

public sealed class AiFeedbackService : IAiFeedbackService
{
    private readonly AppDbContext _db;

    public AiFeedbackService(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<AiFeedbackResponse> SaveFeedbackAsync(
        Guid userId,
        AiFeedbackRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Rating is < 1 or > 5)
        {
            throw new InvalidOperationException("AI feedback rating must be between 1 and 5.");
        }

        var feedback = new AiFeedback
        {
            UserId = userId,
            Operation = request.Operation,
            TargetId = request.TargetId,
            Rating = request.Rating,
            Comment = request.Comment
        };

        _db.AiFeedback.Add(feedback);
        await _db.SaveChangesAsync(cancellationToken);
        return new AiFeedbackResponse(feedback.Id, feedback.CreatedAt);
    }
}
