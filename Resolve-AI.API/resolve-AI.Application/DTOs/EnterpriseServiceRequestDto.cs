using System.Text.Json;
using resolve_AI.Domain.Enums;

namespace resolve_AI.Application.DTOs;

public sealed record EnterpriseServiceRequestDto(
    int SchemaVersion,
    Guid OperationId,
    string UseCaseCode,
    string OperationCode,
    string TargetReference,
    IReadOnlyDictionary<string, JsonElement> Parameters)
{
    public static EnterpriseServiceRequestDto FromPhase7Request(Phase7WorkflowRequestDto request)
    {
        var parameters = new Dictionary<string, JsonElement>
        {
            { "userIdentifier", JsonSerializer.SerializeToElement(request.UserIdentifier) },
            { "tenantIdentifier", JsonSerializer.SerializeToElement(request.TenantIdentifier ?? "default") },
            { "isApproved", JsonSerializer.SerializeToElement(request.IsApproved ?? false) }
        };

        return new EnterpriseServiceRequestDto(
            SchemaVersion: 1,
            OperationId: Guid.Parse(request.RequestId),
            UseCaseCode: request.UseCase.ToString(),
            OperationCode: DetermineOperationCode(request.UseCase),
            TargetReference: request.UserIdentifier,
            Parameters: parameters
        );
    }

    private static string DetermineOperationCode(UseCaseType useCase)
    {
        return useCase switch
        {
            UseCaseType.Microsoft365LicenseNotAssigned => "ASSIGN_LICENSE",
            UseCaseType.MailboxCapacityThresholdAlert => "CHECK_MAILBOX_CAPACITY",
            _ => "UNKNOWN"
        };
    }
}
