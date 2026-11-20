namespace resolve_AI.Domain.Entities.Phase4;

using resolve_AI.Domain.Enums;

public class AutomationRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; }
    public UseCaseType UseCase { get; set; }
    public string RequestData { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Approval for the action. Defaults to false, so nothing is changed in
    // Microsoft 365 unless approval is explicitly given on the request.
    public bool IsApproved { get; set; } = false;
}
