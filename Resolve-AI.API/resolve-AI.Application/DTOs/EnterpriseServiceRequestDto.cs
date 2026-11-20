using System.Text.Json;
using resolve_AI.Domain.Entities.Phase4;
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
    // Approval is NOT a parameter. It is a separate field on AutomationRequest
    // and is checked by the workflow, so it cannot be injected through Parameters.
    public static EnterpriseServiceRequestDto FromAutomationRequest(AutomationRequest request)
    {
        var parameters = new Dictionary<string, JsonElement>
        {
            { "userIdentifier", JsonSerializer.SerializeToElement(request.UserId) },
            { "tenantIdentifier", JsonSerializer.SerializeToElement("default") }
        };

        // An invalid id becomes Guid.Empty; the validator rejects it.
        var operationId = Guid.TryParse(request.Id, out var parsed) ? parsed : Guid.Empty;

        return new EnterpriseServiceRequestDto(
            SchemaVersion: 1,
            OperationId: operationId,
            UseCaseCode: request.UseCase.ToString(),
            OperationCode: DetermineOperationCode(request.UseCase),
            TargetReference: request.UserId ?? string.Empty,
            Parameters: parameters
        );
    }

    public static string DetermineOperationCode(UseCaseType useCase)
    {
        return useCase switch
        {
            UseCaseType.Microsoft365LicenseNotAssigned => "ASSIGN_LICENSE",
            UseCaseType.MailboxCapacityThresholdAlert => "CHECK_MAILBOX_CAPACITY",
            _ => "UNKNOWN"
        };
    }
}
