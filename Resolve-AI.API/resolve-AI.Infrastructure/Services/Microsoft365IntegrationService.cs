using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Text;
using Microsoft.Kiota.Abstractions;
using resolve_AI.Application.DTOs;
using resolve_AI.Application.Interfaces;

namespace resolve_AI.Infrastructure.Services;

/// <summary>
/// Real Microsoft 365 integration service using Microsoft Graph API.
/// Connects to actual Azure AD and retrieves real user and license data.
///
/// Errors from Graph are not hidden here. They propagate to the workflow,
/// which reports them. The only exception is a 404 for a user lookup,
/// which means the user does not exist in the tenant.
/// </summary>
public class Microsoft365IntegrationService : IMicrosoft365IntegrationService
{
    private readonly GraphServiceClient _graphClient;

    public Microsoft365IntegrationService()
    {
        var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
        var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        var clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");

        if (string.IsNullOrEmpty(tenantId) ||
            string.IsNullOrEmpty(clientId) ||
            string.IsNullOrEmpty(clientSecret))
        {
            throw new InvalidOperationException(
                "Azure credentials not configured. Set AZURE_TENANT_ID, " +
                "AZURE_CLIENT_ID, and AZURE_CLIENT_SECRET environment variables.");
        }

        var credential = new ClientSecretCredential(
            tenantId, clientId, clientSecret);

        _graphClient = new GraphServiceClient(credential);
    }

    public async Task<bool> CheckAvailabilityAsync(string? tenantIdentifier)
    {
        // Simple connectivity check using organization endpoint.
        // Errors propagate so the workflow can report the real cause.
        var org = await _graphClient.Organization.GetAsync();
        return org != null;
    }

    public async Task<List<Microsoft365UserDto>> GetUsersListAsync(
        string? tenantIdentifier)
    {
        var users = await _graphClient.Users
            .GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Select = new[] { "id", "userPrincipalName", "displayName" };
                requestConfiguration.QueryParameters.Top = 999;
            });

        return users?.Value?.Select(u => new Microsoft365UserDto
        {
            UserIdentifier = u.UserPrincipalName ?? u.Id ?? "Unknown",
            DisplayName = u.DisplayName ?? "Unknown User",
            Found = true,
            IsMock = false
        }).ToList() ?? new List<Microsoft365UserDto>();
    }

    public async Task<Microsoft365UserDto> GetUserAsync(
        string userIdentifier,
        string? tenantIdentifier)
    {
        try
        {
            // accountEnabled and usageLocation are not returned unless selected.
            var user = await _graphClient.Users[userIdentifier]
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select = new[]
                    {
                        "id", "userPrincipalName", "displayName",
                        "accountEnabled", "usageLocation"
                    };
                });

            return new Microsoft365UserDto
            {
                UserIdentifier = user?.UserPrincipalName ?? userIdentifier,
                DisplayName = user?.DisplayName ?? "Unknown User",
                Found = user != null,
                AccountEnabled = user?.AccountEnabled ?? false,
                UsageLocation = user?.UsageLocation,
                IsMock = false
            };
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 404)
        {
            // Graph returns 404 when the user does not exist in the tenant.
            return new Microsoft365UserDto
            {
                UserIdentifier = userIdentifier,
                DisplayName = "Unknown User",
                Found = false,
                IsMock = false
            };
        }
    }

    private static readonly Guid RequiredSkuId =
        Guid.Parse("cbdc14ab-d96c-4c30-b9f4-6ada7cdc1d46");

    public async Task<Microsoft365LicenseStatusDto> GetLicenseStatusAsync(
        string userIdentifier,
        string? tenantIdentifier)
    {
        var details = new List<LicenseDetails>();
        var page = await _graphClient.Users[userIdentifier]
            .LicenseDetails.GetAsync();

        if (page?.Value == null)
            throw new InvalidOperationException(
                "Microsoft Graph did not return license status.");

        details.AddRange(page.Value);

        while (!string.IsNullOrWhiteSpace(page.OdataNextLink))
        {
            page = await _graphClient.Users[userIdentifier]
                .LicenseDetails.WithUrl(page.OdataNextLink).GetAsync();

            if (page?.Value == null)
                throw new InvalidOperationException(
                    "Microsoft Graph did not return the next license page.");

            details.AddRange(page.Value);
        }

        var requiredLicense = details.FirstOrDefault(
            license => license.SkuId == RequiredSkuId);

        var teamsPlan = requiredLicense?.ServicePlans?.FirstOrDefault(
            plan => plan.ServicePlanName == "TEAMS1");

        bool? teamsEnabled = teamsPlan == null
            ? null
            : teamsPlan.ProvisioningStatus == "Success";

        return new Microsoft365LicenseStatusDto
        {
            UserIdentifier = userIdentifier,
            HasAssignedLicense = details.Count > 0,
            AssignedSkuIds = details
                .Where(license => license.SkuId.HasValue)
                .Select(license => license.SkuId!.Value)
                .Distinct()
                .ToList(),
            RequiredSkuId = RequiredSkuId,
            HasRequiredLicense = requiredLicense != null,
            IsTeamsEnabled = teamsEnabled,
            LicenseStatus = requiredLicense == null
                ? "Required Microsoft 365 Business Premium license is missing."
                : teamsEnabled == true
                    ? "Microsoft 365 Business Premium is assigned; Teams is enabled."
                    : "Business Premium is assigned; Teams is not confirmed active. "
                      + $"Teams status: {teamsPlan?.ProvisioningStatus ?? "Unavailable"}.",
            IsMock = false
        };
    }

    public async Task<MailboxUsageDto> GetMailboxUsageAsync(
        string userIdentifier,
        string? tenantIdentifier)
    {
        // Graph usage report. It is refreshed with a delay (often about a day)
        // and needs the Reports.Read.All application permission.
        var stream = await _graphClient.Reports
            .GetMailboxUsageDetailWithPeriod("D7")
            .GetAsync();

        if (stream == null)
            throw new InvalidOperationException(
                "Microsoft Graph returned no mailbox usage report.");

        using var reader = new StreamReader(stream);

        var headerLine = await reader.ReadLineAsync()
            ?? throw new InvalidOperationException(
                "Mailbox usage report is empty.");

        var header = ParseCsvLine(headerLine);
        var upnIndex = header.IndexOf("User Principal Name");
        var usedIndex = header.IndexOf("Storage Used (Byte)");
        var quotaIndex = header.IndexOf("Prohibit Send/Receive Quota (Byte)");
        var deletedIndex = header.IndexOf("Is Deleted");

        if (upnIndex < 0 || usedIndex < 0 || quotaIndex < 0)
            throw new InvalidOperationException(
                "Mailbox usage report columns were not found as expected.");

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (line.Length == 0)
                continue;

            var columns = ParseCsvLine(line);

            if (columns.Count <= Math.Max(upnIndex, Math.Max(usedIndex, quotaIndex)))
                continue;

            // Deleted mailboxes can keep a row with the same UPN; skip them.
            if (deletedIndex >= 0 && columns.Count > deletedIndex &&
                string.Equals(columns[deletedIndex].Trim(), "True",
                    StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.Equals(columns[upnIndex], userIdentifier,
                    StringComparison.OrdinalIgnoreCase))
                continue;

            long.TryParse(columns[usedIndex], out var usedBytes);
            long.TryParse(columns[quotaIndex], out var quotaBytes);

            return new MailboxUsageDto
            {
                UserIdentifier = userIdentifier,
                Found = true,
                UsedBytes = usedBytes,
                QuotaBytes = quotaBytes,
                IsMock = false
            };
        }

        return new MailboxUsageDto
        {
            UserIdentifier = userIdentifier,
            Found = false,
            IsMock = false
        };
    }

    // Splits one CSV line and respects quoted fields (display names may contain commas).
    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"')
                inQuotes = !inQuotes;
            else if (ch == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
                current.Append(ch);
        }

        fields.Add(current.ToString());
        return fields;
    }

    public async Task<Microsoft365LicenseAssignmentResultDto> AssignLicenseAsync(
        string userIdentifier,
        string? tenantIdentifier,
        bool isApproved)
    {
        Microsoft365LicenseAssignmentResultDto Result(
            bool success, string status, string message) => new()
        {
            UserIdentifier = userIdentifier,
            Success = success,
            AssignmentStatus = status,
            Message = message,
            AssignedSku = "SPB",
            IsMock = false
        };

        try
        {
            if (!isApproved)
                return Result(false, "NOT_APPROVED",
                    "Business Premium assignment is not approved.");

            var current = await GetLicenseStatusAsync(
                userIdentifier, tenantIdentifier);

            if (current.HasRequiredLicense)
            {
                return current.IsTeamsEnabled == true
                    ? Result(true, "NO_ACTION_REQUIRED",
                        "Business Premium with Teams is already assigned.")
                    : Result(false, "TEAMS_REVIEW_REQUIRED",
                        current.LicenseStatus
                        + " Existing service-plan settings need review.");
            }

            var skus = new List<SubscribedSku>();
            var page = await _graphClient.SubscribedSkus.GetAsync();

            if (page?.Value == null)
                throw new InvalidOperationException(
                    "Microsoft Graph did not return tenant licenses.");

            skus.AddRange(page.Value);

            while (!string.IsNullOrWhiteSpace(page.OdataNextLink))
            {
                page = await _graphClient.SubscribedSkus
                    .WithUrl(page.OdataNextLink).GetAsync();

                if (page?.Value == null)
                    throw new InvalidOperationException(
                        "Microsoft Graph did not return the next SKU page.");

                skus.AddRange(page.Value);
            }

            var requiredSku = skus.FirstOrDefault(
                sku => sku.SkuId == RequiredSkuId);

            if (requiredSku == null)
                return Result(false, "SKU_NOT_AVAILABLE",
                    "Required Business Premium SKU is not available in this tenant.");

            if (requiredSku.CapabilityStatus != "Enabled")
                return Result(false, "SKU_NOT_ENABLED",
                    "Required Business Premium SKU is not enabled.");

            var teamsIncluded = requiredSku.ServicePlans?.Any(
                plan => plan.ServicePlanName == "TEAMS1"
                        && plan.ProvisioningStatus == "Success") == true;

            if (!teamsIncluded)
                return Result(false, "TEAMS_NOT_AVAILABLE",
                    "Required SKU does not have an available Teams plan.");

            var freeSeats = (requiredSku.PrepaidUnits?.Enabled ?? 0)
                            - (requiredSku.ConsumedUnits ?? 0);

            if (freeSeats <= 0)
                return Result(false, "NO_FREE_SEATS",
                    "No free Business Premium seats are available.");

            var body =
                new Microsoft.Graph.Users.Item.AssignLicense.AssignLicensePostRequestBody
                {
                    AddLicenses = new()
                    {
                        new() { SkuId = RequiredSkuId }
                    },
                    RemoveLicenses = new()
                };

            await _graphClient.Users[userIdentifier]
                .AssignLicense.PostAsync(body);

            return Result(true, "ASSIGNMENT_ACCEPTED",
                "Microsoft Graph accepted the Business Premium assignment. "
                + "User license and Teams activation must be verified afterward.");
        }
        catch (Exception ex)
        {
            return Result(false, "FAILED",
                $"Business Premium assignment failed: {ex.Message}");
        }
    }

    // WBS 7.17 - License subscription snapshot for audit trail.
    public async Task<LicenseSnapshotDto> GetLicenseSubscriptionSnapshotAsync(
        string? tenantIdentifier)
    {
        var skus = new List<Microsoft.Graph.Models.SubscribedSku>();
        var page = await _graphClient.SubscribedSkus.GetAsync();

        if (page?.Value == null)
            throw new InvalidOperationException(
                "Microsoft Graph did not return tenant licenses.");

        skus.AddRange(page.Value);

        while (!string.IsNullOrWhiteSpace(page.OdataNextLink))
        {
            page = await _graphClient.SubscribedSkus
                .WithUrl(page.OdataNextLink).GetAsync();

            if (page?.Value == null)
                throw new InvalidOperationException(
                    "Microsoft Graph did not return the next SKU page.");

            skus.AddRange(page.Value);
        }

        var requiredSku = skus.FirstOrDefault(
            sku => sku.SkuId == RequiredSkuId);

        int totalSeats = 0;
        int consumedSeats = 0;

        if (requiredSku != null)
        {
            totalSeats = requiredSku.PrepaidUnits?.Enabled ?? 0;
            consumedSeats = requiredSku.ConsumedUnits ?? 0;
        }

        return new LicenseSnapshotDto
        {
            TotalSeats = totalSeats,
            ConsumedSeats = consumedSeats,
            AvailableSeats = Math.Max(0, totalSeats - consumedSeats),
            SnapshotAtUtc = DateTime.UtcNow,
            IsMock = false
        };
    }
}
