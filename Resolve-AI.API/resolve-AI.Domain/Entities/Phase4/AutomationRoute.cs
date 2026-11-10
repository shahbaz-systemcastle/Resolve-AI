namespace resolve_AI.Domain.Entities.Phase4;

public class AutomationRoute
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RequestId { get; set; }
    public int TargetPhase { get; set; }
    public string Reason { get; set; }
    public int Priority { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
