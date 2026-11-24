# Environment Setup Guide

## Creating Your .env File

1. **Create file** in solution root: `D:\Resolve AI\Resolve-AI.API\.env`

2. **Add these values:**
   ```
   AZURE_TENANT_ID=your_value_here
   AZURE_CLIENT_ID=your_value_here
   AZURE_CLIENT_SECRET=your_value_here
   POWERSHELL_PATH=C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe
   EXCHANGE_APP_ID=your_exchange_app_id
   EXCHANGE_ORGANIZATION=yourtenant.onmicrosoft.com
   EXCHANGE_CERTIFICATE_THUMBPRINT=your_certificate_thumbprint
   ALERT_SENDER_USER=alerts@yourtenant.onmicrosoft.com
   ```

3. **Get values from Azure Portal:**
   - Go to: https://portal.azure.com
   - Search: App Registrations
   - Find your app
   - Copy: Tenant ID, Client ID, Client Secret

4. **Important:** 
   - .env is in .gitignore (never commit it)
   - Only you see your local .env
   - Keep secrets safe

## Troubleshooting

- If "Azure credentials not configured" error: check .env path
- If "authentication failed" error: verify secret in Azure Portal is active
- If Exchange returns HTTP 502: configure all three `EXCHANGE_*` values,
  install the ExchangeOnlineManagement PowerShell module, and confirm the
  certificate thumbprint exists in the local certificate store.
- For real UC-08 email alerts, configure `ALERT_SENDER_USER` and grant the
  application Microsoft Graph `Mail.Send` permission with admin consent.
- Restart API after changing .env file
