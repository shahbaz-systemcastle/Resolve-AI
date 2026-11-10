namespace resolve_AI.Domain.Entities.Phase4;

public class ExecutionResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RequestId { get; set; }
    public int PhaseNumber { get; set; }
    public string Status { get; set; }
    public string? Data { get; set; }
    public string? Error { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}
