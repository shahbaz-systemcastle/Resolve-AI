using resolve_AI.Application.DTOs;
using resolve_AI.Domain.Enums;

namespace resolve_AI.Application.Validation;

/// <summary>
/// Checks an Enterprise Service request against the request contract before
/// any Microsoft 365 call is made.
///
/// Returns null when the request is valid, otherwise the reason code.
/// Only the fields listed in AllowedParameters are accepted; anything else
/// (including an approval flag) is rejected.
/// </summary>
public static class EnterpriseServiceRequestValidator
{
    public const int SupportedSchemaVersion = 1;

    private static readonly HashSet<string> AllowedParameters = new(StringComparer.Ordinal)
    {
        "userIdentifier",
        "tenantIdentifier"
    };

    public static (string? Code, string? Message) Validate(EnterpriseServiceRequestDto request)
    {
        if (request.SchemaVersion != SupportedSchemaVersion)
        {
            return (Phase7Status.UnsupportedSchemaVersion,
                $"Schema version {request.SchemaVersion} is not supported.");
        }

        if (request.OperationId == Guid.Empty)
        {
            return (Phase7Status.InvalidOperationId,
                "OperationId must be a valid GUID.");
        }

        if (!Enum.TryParse<UseCaseType>(request.UseCaseCode, out var useCase))
        {
            return (Phase7Status.UnknownOperation,
                $"UseCaseCode '{request.UseCaseCode}' is not known.");
        }

        var expectedOperation = EnterpriseServiceRequestDto.DetermineOperationCode(useCase);

        if (expectedOperation == "UNKNOWN" ||
            !string.Equals(request.OperationCode, expectedOperation, StringComparison.Ordinal))
        {
            return (Phase7Status.UnknownOperation,
                $"OperationCode '{request.OperationCode}' does not match UseCaseCode '{request.UseCaseCode}'.");
        }

        foreach (var key in request.Parameters.Keys)
        {
            if (!AllowedParameters.Contains(key))
            {
                return (Phase7Status.InvalidParameters,
                    $"Parameter '{key}' is not allowed for this operation.");
            }
        }

        if (!request.Parameters.TryGetValue("userIdentifier", out var userElement) ||
            userElement.ValueKind != System.Text.Json.JsonValueKind.String ||
            string.IsNullOrWhiteSpace(userElement.GetString()))
        {
            return (Phase7Status.InvalidParameters,
                "Parameter 'userIdentifier' is required and must be a string.");
        }

        if (string.IsNullOrWhiteSpace(request.TargetReference) ||
            !string.Equals(request.TargetReference, userElement.GetString(),
                StringComparison.OrdinalIgnoreCase))
        {
            return (Phase7Status.TargetMismatch,
                "TargetReference must match the userIdentifier parameter.");
        }

        return (null, null);
    }
}
