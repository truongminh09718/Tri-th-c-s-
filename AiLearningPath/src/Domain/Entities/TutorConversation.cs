namespace AiLearningPath.Domain.Entities;

public class TutorConversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public ICollection<TutorMessage> Messages { get; set; } = new List<TutorMessage>();
}

public class TutorMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TutorConversationId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public bool UsedFallback { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TutorConversation? Conversation { get; set; }
}
