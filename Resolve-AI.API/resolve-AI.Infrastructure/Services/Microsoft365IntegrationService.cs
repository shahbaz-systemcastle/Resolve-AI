using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
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
            var user = await _graphClient.Users[userIdentifier]
                .GetAsync();

            return new Microsoft365UserDto
            {
                UserIdentifier = user?.UserPrincipalName ?? userIdentifier,
                DisplayName = user?.DisplayName ?? "Unknown User",
                Found = user != null,
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

    public async Task<Microsoft365LicenseStatusDto> GetLicenseStatusAsync(
        string userIdentifier,
        string? tenantIdentifier)
    {
        // Errors propagate. A failed lookup must never look like "no license",
        // because that would lead to an assignment.
        var licenseDetails = await _graphClient.Users[userIdentifier]
            .LicenseDetails
            .GetAsync();

        var hasLicense = licenseDetails?.Value?.Count > 0;

        return new Microsoft365LicenseStatusDto
        {
            UserIdentifier = userIdentifier,
            HasAssignedLicense = hasLicense,
            LicenseStatus = hasLicense
                ? $"{licenseDetails?.Value?.Count ?? 0} license(s) assigned"
                : "No licenses assigned",
            IsMock = false
        };
    }

    public async Task<Microsoft365LicenseAssignmentResultDto> AssignLicenseAsync(
        string userIdentifier,
        string? tenantIdentifier,
        bool isApproved)
    {
        try
        {
            if (!isApproved)
            {
                return new Microsoft365LicenseAssignmentResultDto
                {
                    UserIdentifier = userIdentifier,
                    Success = false,
                    Message = "License assignment not approved",
                    AssignmentStatus = "NOT_APPROVED",
                    IsMock = false
                };
            }

            // SKUs the user already holds must not be assigned again
            var currentLicenses = await _graphClient.Users[userIdentifier]
                .LicenseDetails
                .GetAsync();

            var currentSkuIds = currentLicenses?.Value?
                .Select(l => l.SkuId)
                .ToHashSet() ?? new HashSet<Guid?>();

            // Get SKUs from tenant
            var skus = await _graphClient.SubscribedSkus.GetAsync();
            if (skus?.Value == null || skus.Value.Count == 0)
            {
                return new Microsoft365LicenseAssignmentResultDto
                {
                    UserIdentifier = userIdentifier,
                    Success = false,
                    Message = "No licenses available in tenant",
                    AssignmentStatus = "FAILED",
                    IsMock = false
                };
            }

            // Choose a SKU that is enabled, has free seats, and is not
            // already held by the user. Which SKU is the right one for a
            // given user is still an open business rule.
            var availableSku = skus.Value.FirstOrDefault(s =>
                s.CapabilityStatus == "Enabled" &&
                (s.PrepaidUnits?.Enabled ?? 0) - (s.ConsumedUnits ?? 0) > 0 &&
                !currentSkuIds.Contains(s.SkuId));

            if (availableSku == null)
            {
                return new Microsoft365LicenseAssignmentResultDto
                {
                    UserIdentifier = userIdentifier,
                    Success = false,
                    Message = "No SKU with free seats is available for this user",
                    AssignmentStatus = "FAILED",
                    IsMock = false
                };
            }

            // Prepare license assignment request
            var assignLicenseRequestBody = new Microsoft.Graph.Users.Item.AssignLicense.AssignLicensePostRequestBody
            {
                AddLicenses = new()
                {
                    new()
                    {
                        SkuId = availableSku.SkuId
                    }
                },
                RemoveLicenses = new()
            };

            // Assign license to user
            await _graphClient.Users[userIdentifier]
                .AssignLicense
                .PostAsync(assignLicenseRequestBody);

            return new Microsoft365LicenseAssignmentResultDto
            {
                UserIdentifier = userIdentifier,
                Success = true,
                Message = $"License assigned successfully",
                AssignmentStatus = "SUCCESS",
                AssignedSku = availableSku.SkuPartNumber ?? "LICENSE_ASSIGNED",
                IsMock = false
            };
        }
        catch (Exception ex)
        {
            return new Microsoft365LicenseAssignmentResultDto
            {
                UserIdentifier = userIdentifier,
                Success = false,
                Message = $"License assignment failed: {ex.Message}",
                AssignmentStatus = "FAILED",
                IsMock = false
            };
        }
    }
}
