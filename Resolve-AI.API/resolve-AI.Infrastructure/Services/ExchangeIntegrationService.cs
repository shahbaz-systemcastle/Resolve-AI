using System.Diagnostics;
using System.Text;
using resolve_AI.Application.Interfaces;

namespace resolve_AI.Infrastructure.Services;

public class ExchangeIntegrationService : IExchangeIntegrationService
{
    public async Task<bool> CheckAvailabilityAsync(
        CancellationToken cancellationToken = default)
    {
        var powershellPath = Required("POWERSHELL_PATH");

        if (!File.Exists(powershellPath))
            throw new InvalidOperationException(
                "Configured PowerShell executable was not found.");

        const string script = """
            $ErrorActionPreference = 'Stop'
            $ProgressPreference = 'SilentlyContinue'
            Import-Module ExchangeOnlineManagement

            try {
                Connect-ExchangeOnline `
                    -AppId $env:EXCHANGE_APP_ID `
                    -CertificateThumbprint $env:EXCHANGE_CERTIFICATE_THUMBPRINT `
                    -Organization $env:EXCHANGE_ORGANIZATION `
                    -ShowBanner:$false

                $config = Get-OrganizationConfig -ErrorAction Stop
                if ($null -eq $config) {
                    throw 'Exchange organization could not be retrieved.'
                }

                [Console]::Out.WriteLine('RESOLVE_AI_EXCHANGE_OK')
            }
            catch {
                [Console]::Error.WriteLine($_.Exception.Message)
                exit 1
            }
            finally {
                Disconnect-ExchangeOnline -Confirm:$false -ErrorAction SilentlyContinue
            }
            """;

        var startInfo = new ProcessStartInfo
        {
            FileName = powershellPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.Environment["EXCHANGE_APP_ID"] =
            Required("EXCHANGE_APP_ID");
        startInfo.Environment["EXCHANGE_ORGANIZATION"] =
            Required("EXCHANGE_ORGANIZATION");
        startInfo.Environment["EXCHANGE_CERTIFICATE_THUMBPRINT"] =
            Required("EXCHANGE_CERTIFICATE_THUMBPRINT");

        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-EncodedCommand");
        startInfo.ArgumentList.Add(
            Convert.ToBase64String(Encoding.Unicode.GetBytes(script)));

        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
            throw new InvalidOperationException("PowerShell could not start.");

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(120));

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);

            await process.WaitForExitAsync(CancellationToken.None);
            await Task.WhenAll(outputTask, errorTask);

            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException(
                "Exchange connection check exceeded 120 seconds.");
        }

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"Exchange connection failed: {error.Trim()}");

        if (!output.Split('\n').Any(
                line => line.Trim() == "RESOLVE_AI_EXCHANGE_OK"))
            throw new InvalidOperationException(
                "Exchange connection confirmation was not received.");

        return true;
    }

    private static string Required(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{name} is not configured.");

        return value;
    }
}
