using AiLearningPath.Application.Assessments;
using AiLearningPath.Application.Career;
using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Application.Paths;

namespace AiLearningPath.Infrastructure.Services;

public sealed class AiContentGeneratorAdapter : IContentGenerator
{
    private readonly IAiGateway _aiGateway;

    public AiContentGeneratorAdapter(IAiGateway aiGateway)
    {
        _aiGateway = aiGateway ?? throw new ArgumentNullException(nameof(aiGateway));
    }

    public async Task<ContentGenerationResult> GenerateAsync(
        ContentGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var operation = request.Kind switch
        {
            AssessmentContentKind.Assessment => AiOperations.AssessmentQuestions,
            PathContentKind.LearningPath => AiOperations.LearningPathContent,
            CareerContentKind.CareerPath => AiOperations.CareerPath,
            _ => request.Kind
        };

        var result = await _aiGateway.GenerateAsync(
            new AiGatewayRequest(null, operation, request.LearningGoal, request.Parameters),
            cancellationToken);

        return new ContentGenerationResult(result.Content);
    }
}
