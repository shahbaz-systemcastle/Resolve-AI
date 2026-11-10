using System.Text.Json;

namespace resolve_AI.Application.DTOs;

public sealed record EnterpriseServiceResultDto(
    int SchemaVersion,
    Guid OperationId,
    string Status,
    string OutcomeCode,
    string? VerificationEvidenceReference,
    DateTimeOffset CompletedAtUtc)
{
    public static EnterpriseServiceResultDto FromPhase7Result(
        Phase7WorkflowResultDto phase7Result,
        Guid operationId)
    {
        return new EnterpriseServiceResultDto(
            SchemaVersion: 1,
            OperationId: operationId,
            Status: phase7Result.Success ? "SucceededVerified" : "Failed",
            OutcomeCode: phase7Result.Status,
            VerificationEvidenceReference: phase7Result.Success ? $"phase7/{operationId}" : null,
            CompletedAtUtc: DateTimeOffset.UtcNow
        );
    }

    public Phase7WorkflowResultDto ToPhase7Result(Phase7WorkflowResultDto original)
    {
        return new Phase7WorkflowResultDto
        {
            RequestId = OperationId.ToString(),
            UseCase = original.UseCase,
            Success = Status == "SucceededVerified",
            Status = OutcomeCode,
            Message = VerificationEvidenceReference ?? "Operation failed",
            IsMock = original.IsMock
        };
    }
}
