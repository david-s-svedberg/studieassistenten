# Secrets Management Guide

## Overview

This application uses **User Secrets** for development and **Environment Variables** for production to securely manage sensitive configuration values.

## Development Setup

### Initialize User Secrets

User Secrets are already configured for this project with ID: `3fe8c021-7c0f-429b-9406-63048a817dd0`

To manage secrets, use the dotnet CLI from the Server project directory:

```bash
cd StudieAssistenten/Server

# Set a secret
dotnet user-secrets set "GoogleAuth:ClientId" "your-client-id-here"
dotnet user-secrets set "GoogleAuth:ClientSecret" "your-client-secret-here"

# List all secrets
dotnet user-secrets list

# Remove a secret
dotnet user-secrets remove "GoogleAuth:ClientId"

# Clear all secrets
dotnet user-secrets clear
```

### Required Secrets

#### Google OAuth (Required)
```bash
dotnet user-secrets set "GoogleAuth:ClientId" "YOUR_GOOGLE_CLIENT_ID"
dotnet user-secrets set "GoogleAuth:ClientSecret" "YOUR_GOOGLE_CLIENT_SECRET"
```

Get these from: https://console.cloud.google.com/apis/credentials

#### Anthropic API (Required for AI features)
```bash
dotnet user-secrets set "Anthropic:ApiKey" "YOUR_ANTHROPIC_API_KEY"
```

Get from: https://console.anthropic.com/

#### Azure Computer Vision (Optional - only if using Azure OCR)
```bash
dotnet user-secrets set "AzureComputerVision:Endpoint" "https://your-resource.cognitiveservices.azure.com/"
dotnet user-secrets set "AzureComputerVision:ApiKey" "YOUR_AZURE_API_KEY"
```

**Note:** Azure OCR is optional. The app will fall back to Tesseract OCR if Azure is not configured.

### User Secrets Location

User secrets are stored at:
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\3fe8c021-7c0f-429b-9406-63048a817dd0\secrets.json`
- **Linux/Mac**: `~/.microsoft/usersecrets/3fe8c021-7c0f-429b-9406-63048a817dd0/secrets.json`

## Production Setup

### Environment Variables

For production, set these environment variables:

```bash
# Google OAuth
export GoogleAuth__ClientId="your-client-id"
export GoogleAuth__ClientSecret="your-client-secret"

# Anthropic API
export Anthropic__ApiKey="your-api-key"

# Azure OCR (optional)
export AzureComputerVision__Endpoint="https://your-resource.cognitiveservices.azure.com/"
export AzureComputerVision__ApiKey="your-api-key"

# Email Whitelist (optional - override config)
export GoogleAuth__AllowedEmails__0="user1@gmail.com"
export GoogleAuth__AllowedEmails__1="user2@gmail.com"
```

**Note:** Use double underscores (`__`) for nested configuration in environment variables.

### Docker Deployment

When using Docker, pass secrets via environment variables:

```bash
docker run -e GoogleAuth__ClientId="..." -e GoogleAuth__ClientSecret="..." ...
```

Or use a `.env` file (never commit this file!):

```bash
docker run --env-file .env.production ...
```

## Security Best Practices

### ✅ DO:
- Use User Secrets for development
- Use Environment Variables or Key Vault for production
- Keep `appsettings.json` in version control (with placeholder values)
- Add `appsettings.Development.json` to `.gitignore`
- Rotate API keys regularly
- Use different OAuth clients for dev/staging/production

### ❌ DON'T:
- Commit `appsettings.Development.json`
- Hardcode secrets in code
- Share secrets in chat/email
- Use production secrets in development
- Commit `.env` files

## Verification

### Check gitignore

Verify these patterns are in `.gitignore`:

```
**/appsettings.Development.json
**/appsettings.*.json
!**/appsettings.json
.env
.env.*
secrets.json
```

### Verify No Secrets in Git

Run from the repository root:

```bash
# Check for accidentally committed secrets
git log --all --full-history --source -- '**/appsettings.Development.json'
git log --all --full-history --source -- '**/.env'
```

If any secrets were accidentally committed, contact your team lead immediately.

## Troubleshooting

### "Google ClientId is not configured"

**Solution:** Set the Google OAuth secrets:
```bash
dotnet user-secrets set "GoogleAuth:ClientId" "your-client-id"
dotnet user-secrets set "GoogleAuth:ClientSecret" "your-client-secret"
```

### "Anthropic API key is not configured"

**Solution:** Set the Anthropic API key:
```bash
dotnet user-secrets set "Anthropic:ApiKey" "your-api-key"
```

### User Secrets Not Loading

1. Check you're in the Server project directory
2. Verify `UserSecretsId` is in the `.csproj` file
3. Check the secrets file exists at the location above
4. Restart Visual Studio / VS Code / your terminal

### Production Environment Variables Not Working

1. Check environment variable names use double underscores (`__`)
2. Verify variables are set before starting the application
3. Check for typos in variable names (case-sensitive on Linux)
4. Use `printenv | grep Auth` to verify variables are set

## Configuration Priority

Configuration values are loaded in this order (later sources override earlier):

1. `appsettings.json` (committed to git)
2. `appsettings.{Environment}.json` (gitignored for Development)
3. User Secrets (development only)
4. Environment Variables (all environments)
5. Command-line arguments (highest priority)

## Support

For issues with secrets management:
1. Check this documentation
2. Verify .gitignore is correct
3. Check User Secrets are properly set
4. Contact the development team
