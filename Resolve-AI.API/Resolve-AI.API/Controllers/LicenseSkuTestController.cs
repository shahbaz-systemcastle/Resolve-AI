using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;

namespace Resolve_AI.API.Controllers;

[ApiController]
[Route("api/phase7-test")]
public class LicenseSkuTestController : ControllerBase
{
    [HttpGet("license-skus")]
    public async Task<IActionResult> GetLicenseSkus(
        CancellationToken cancellationToken)
    {
        var credential = new ClientSecretCredential(
            Environment.GetEnvironmentVariable("AZURE_TENANT_ID"),
            Environment.GetEnvironmentVariable("AZURE_CLIENT_ID"),
            Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET"));

        var graph = new GraphServiceClient(credential);
        var skus = await graph.SubscribedSkus.GetAsync(
            cancellationToken: cancellationToken);

        return Ok(new
        {
            IsMock = false,
            Licenses = skus?.Value?.Select(s => new
            {
                s.SkuPartNumber,
                s.SkuId,
                s.CapabilityStatus,
                TotalSeats = s.PrepaidUnits?.Enabled ?? 0,
                UsedSeats = s.ConsumedUnits ?? 0,
                FreeSeats = (s.PrepaidUnits?.Enabled ?? 0)
                            - (s.ConsumedUnits ?? 0),
                ServicePlans = s.ServicePlans?.Select(p => new
                {
                    p.ServicePlanName,
                    p.ProvisioningStatus
                })
            }).ToList()
        });
    }
}
