namespace AiLearningPath.Domain.Entities;

public class AiCacheEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CacheKey { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string SchemaVersion { get; set; } = "v1";
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}
