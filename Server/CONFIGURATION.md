# Application Configuration Guide

## Overview

This document describes all configuration options available in `appsettings.json`. Configuration values can be overridden using environment variables (for production) or User Secrets (for development).

## Configuration Sections

### Connection Strings

Configures database connections for the application.

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=studieassistenten.db"
}
```

| Setting | Description | Default | Required |
|---------|-------------|---------|----------|
| `DefaultConnection` | SQLite database file path | `studieassistenten.db` | Yes |

**Environment Variable Override:**
```bash
export ConnectionStrings__DefaultConnection="Data Source=/path/to/database.db"
```

**Production Note:** For production deployments, use a fully qualified path or a more robust database like PostgreSQL.

---

### Authentication

Configures OAuth 2.0 authentication providers.

#### Google OAuth

```json
"Authentication": {
  "Google": {
    "ClientId": "",
    "ClientSecret": ""
  }
}
```

| Setting | Description | Required | Security |
|---------|-------------|----------|----------|
| `ClientId` | Google OAuth 2.0 Client ID | Yes | Not sensitive (public) |
| `ClientSecret` | Google OAuth 2.0 Client Secret | Yes | **SENSITIVE - Never commit** |

**How to Get Credentials:**
1. Go to [Google Cloud Console](https://console.cloud.google.com/apis/credentials)
2. Create OAuth 2.0 credentials
3. Add authorized redirect URIs: `https://yourdomain.com/signin-google`

**Development Setup:**
```bash
cd StudieAssistenten/Server
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_CLIENT_SECRET"
```

**Production Setup:**
```bash
export Authentication__Google__ClientId="YOUR_CLIENT_ID"
export Authentication__Google__ClientSecret="YOUR_CLIENT_SECRET"
```

---

### Anthropic (Claude AI)

Configures the Anthropic Claude API for AI-powered content generation.

```json
"Anthropic": {
  "ApiKey": "",
  "Model": "claude-3-5-sonnet-20241022",
  "MaxTokens": 4000
}
```

| Setting | Description | Default | Required | Security |
|---------|-------------|---------|----------|----------|
| `ApiKey` | Anthropic API key | - | Yes | **SENSITIVE - Never commit** |
| `Model` | Claude model to use | `claude-3-5-sonnet-20241022` | No | Public |
| `MaxTokens` | Maximum response tokens | `4000` | No | Public |

**Available Models:**
- `claude-3-5-sonnet-20241022` (Recommended - Best balance)
- `claude-3-opus-20240229` (Most capable, slower, expensive)
- `claude-3-haiku-20240307` (Fastest, cheapest)

**How to Get API Key:**
1. Sign up at [Anthropic Console](https://console.anthropic.com/)
2. Create an API key in the dashboard
3. Monitor usage to avoid unexpected costs

**Development Setup:**
```bash
dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-..."
```

**Production Setup:**
```bash
export Anthropic__ApiKey="sk-ant-..."
```

**Cost Management:**
- Enable rate limiting (see RateLimiting section)
- Monitor daily token usage
- Set billing alerts in Anthropic Console

---

### Azure Computer Vision (Optional)

Configures Azure Computer Vision for OCR processing. If not configured, the application falls back to Tesseract OCR.

```json
"Azure": {
  "ComputerVision": {
    "Endpoint": "",
    "ApiKey": ""
  }
}
```

| Setting | Description | Required | Security |
|---------|-------------|----------|----------|
| `Endpoint` | Azure Computer Vision endpoint URL | No | Public |
| `ApiKey` | Azure Computer Vision API key | No | **SENSITIVE - Never commit** |

**When to Use Azure OCR:**
- Need higher accuracy than Tesseract
- Processing complex documents
- Willing to pay for Azure services

**When to Use Tesseract (Default):**
- Free option preferred
- Simple documents
- Privacy concerns (on-premise processing)

**Setup:**
1. Create Azure Computer Vision resource
2. Copy endpoint and key from Azure Portal

```bash
dotnet user-secrets set "Azure:ComputerVision:Endpoint" "https://your-resource.cognitiveservices.azure.com/"
dotnet user-secrets set "Azure:ComputerVision:ApiKey" "YOUR_KEY"
```

---

### Email Whitelist

Controls access to the application based on Google email addresses.

```json
"EmailWhitelist": {
  "EnableWhitelist": true,
  "AllowedEmails": [
    "user1@gmail.com",
    "user2@gmail.com"
  ]
}
```

| Setting | Description | Default | Type |
|---------|-------------|---------|------|
| `EnableWhitelist` | Enable email-based access control | `true` | Boolean |
| `AllowedEmails` | List of permitted email addresses | `[]` | Array |

**Security Implications:**
- When `EnableWhitelist` is `true`: Only listed emails can authenticate
- When `EnableWhitelist` is `false`: Any Google account can authenticate (⚠️ **Not recommended for production**)

**Production Override:**
```bash
export EmailWhitelist__EnableWhitelist="true"
export EmailWhitelist__AllowedEmails__0="admin@company.com"
export EmailWhitelist__AllowedEmails__1="user@company.com"
```

**Use Cases:**
- Private beta testing
- Internal company tools
- Small user base with manual approval

---

### Rate Limiting

Prevents excessive AI API usage by limiting daily token consumption.

```json
"RateLimiting": {
  "DailyTokenLimit": 1000000,
  "EnableRateLimiting": true
}
```

| Setting | Description | Default | Recommended |
|---------|-------------|---------|-------------|
| `DailyTokenLimit` | Maximum tokens per day | `1,000,000` | Adjust based on budget |
| `EnableRateLimiting` | Enable rate limiting | `true` | `true` (always) |

**Token Consumption Estimates:**
- Flashcard generation: ~2,000-4,000 tokens per request
- Practice test generation: ~3,000-6,000 tokens per request
- Summary generation: ~2,000-5,000 tokens per request

**Cost Calculation:**
With Claude 3.5 Sonnet ($3 per million input tokens, $15 per million output tokens):
- Daily limit of 1M tokens ≈ $9-$18 per day (depending on input/output ratio)

**Recommendations:**
- **Development**: 100,000 tokens/day
- **Small production** (< 50 users): 500,000 tokens/day
- **Medium production** (50-200 users): 2,000,000 tokens/day

**Disable for Testing:**
```bash
export RateLimiting__EnableRateLimiting="false"
```

---

### CORS (Cross-Origin Resource Sharing)

Configures cross-origin request handling for the API.

```json
"Cors": {
  "EnableCors": false,
  "AllowedOrigins": []
}
```

| Setting | Description | Default | Type |
|---------|-------------|---------|------|
| `EnableCors` | Enable CORS policy | `false` | Boolean |
| `AllowedOrigins` | List of allowed origin URLs | `[]` | Array |

**When CORS is Needed:**
- **Development**: Blazor WASM hot reload may use different ports ✅
- **Separate Deployments**: API and client hosted on different domains ✅
- **Production (hosted app)**: NOT needed - same origin ❌

**Development Configuration (appsettings.Development.json):**
```json
"Cors": {
  "EnableCors": true,
  "AllowedOrigins": [
    "https://localhost:7247",
    "http://localhost:5059"
  ]
}
```

**Production with Separate Domains:**
```json
"Cors": {
  "EnableCors": true,
  "AllowedOrigins": [
    "https://app.studieassistenten.com",
    "https://www.studieassistenten.com"
  ]
}
```

**Environment Variable Override:**
```bash
export Cors__EnableCors="true"
export Cors__AllowedOrigins__0="https://app.example.com"
export Cors__AllowedOrigins__1="https://www.example.com"
```

**Important Notes:**
- CORS is **disabled by default** in production (hosted Blazor WASM = same origin)
- CORS is **enabled in Development** for local debugging
- Only enable in production if deploying API and client separately
- `AllowCredentials` is always enabled for cookie-based authentication

---

### File Upload

Configures file upload restrictions for security and resource management.

```json
"FileUpload": {
  "MaxFileSizeBytes": 52428800,
  "AllowedExtensions": [ ".pdf", ".jpg", ".jpeg", ".png" ]
}
```

| Setting | Description | Default | Unit |
|---------|-------------|---------|------|
| `MaxFileSizeBytes` | Maximum file size | `52,428,800` (50MB) | Bytes |
| `AllowedExtensions` | Permitted file extensions | `[".pdf", ".jpg", ".jpeg", ".png"]` | Array |

**Size Conversions:**
- 10 MB = 10,485,760 bytes
- 50 MB = 52,428,800 bytes (default)
- 100 MB = 104,857,600 bytes

**Supported File Types:**
- **PDF**: `application/pdf` (.pdf)
- **Images**: `image/jpeg` (.jpg, .jpeg), `image/png` (.png)

**Security Features:**
- File size validation (prevents DoS attacks)
- Content-Type header validation
- Magic byte validation (prevents type spoofing)

**Adjust for Your Needs:**
```bash
# Allow 100MB uploads
export FileUpload__MaxFileSizeBytes="104857600"
```

---

### Logging

Configures application logging levels.

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning"
  }
}
```

| Log Level | Description | When to Use |
|-----------|-------------|-------------|
| `Trace` | Very detailed logs | Debugging specific issues |
| `Debug` | Detailed logs | Development |
| `Information` | General informational logs | Production (default) |
| `Warning` | Warning messages | Production |
| `Error` | Error messages | Always |
| `Critical` | Critical failures | Always |

**Production Recommendations:**
```json
"Logging": {
  "LogLevel": {
    "Default": "Warning",
    "Microsoft.AspNetCore": "Error",
    "StudieAssistenten": "Information"
  }
}
```

**Development Setup (Verbose):**
```json
"Logging": {
  "LogLevel": {
    "Default": "Debug",
    "Microsoft.AspNetCore": "Information"
  }
}
```

---

### Allowed Hosts

Specifies which hostnames can serve the application.

```json
"AllowedHosts": "*"
```

| Value | Description | Security |
|-------|-------------|----------|
| `"*"` | Allow all hosts | ⚠️ Only for development |
| `"yourdomain.com"` | Single domain | ✅ Production recommended |
| `"yourdomain.com;api.yourdomain.com"` | Multiple domains | ✅ Production recommended |

**Production Example:**
```json
"AllowedHosts": "studieassistenten.com;www.studieassistenten.com"
```

---

## Environment-Specific Configuration

### Development (`appsettings.Development.json`)

**⚠️ NEVER commit this file to source control** - it's in `.gitignore` for a reason.

Example for local development:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  },
  "RateLimiting": {
    "DailyTokenLimit": 100000,
    "EnableRateLimiting": false
  }
}
```

### Production

Use environment variables or a secure configuration management system (Azure Key Vault, AWS Secrets Manager, etc.).

**Docker Example:**
```dockerfile
ENV Authentication__Google__ClientId="YOUR_CLIENT_ID"
ENV Authentication__Google__ClientSecret="YOUR_CLIENT_SECRET"
ENV Anthropic__ApiKey="sk-ant-..."
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/production.db"
```

**Kubernetes Secret Example:**
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: studieassistenten-secrets
type: Opaque
stringData:
  AUTHENTICATION__GOOGLE__CLIENTSECRET: "YOUR_SECRET"
  ANTHROPIC__APIKEY: "sk-ant-..."
```

---

## Configuration Priority

ASP.NET Core loads configuration in this order (later sources override earlier):

1. `appsettings.json` (committed to source control)
2. `appsettings.{Environment}.json` (Development is gitignored)
3. **User Secrets** (development only, never in production)
4. **Environment Variables** (recommended for production)
5. **Command-line arguments** (highest priority)

---

## Security Best Practices

### ✅ DO:

- Use User Secrets for development
- Use Environment Variables or Key Vault for production
- Keep `appsettings.json` in source control with **empty/placeholder secrets**
- Rotate API keys regularly (quarterly recommended)
- Use different OAuth credentials for dev/staging/production
- Enable rate limiting to control costs
- Set `AllowedHosts` to specific domains in production
- Monitor API usage and costs

### ❌ DON'T:

- Commit `appsettings.Development.json`
- Hardcode API keys in code
- Share secrets via email/Slack
- Use production secrets in development
- Disable rate limiting in production
- Set `AllowedHosts` to `"*"` in production
- Commit `.env` files

---

## Validation & Troubleshooting

### Check Configuration at Runtime

The application validates critical configuration on startup. Check logs for:
```
[Error] Google ClientId is not configured
[Error] Anthropic API key is not configured
```

### Test Configuration

```bash
# List all User Secrets
dotnet user-secrets list

# Test environment variables
printenv | grep -i auth
printenv | grep -i anthropic

# Verify appsettings.json is valid JSON
cat appsettings.json | jq .
```

### Common Issues

**Issue:** "Google ClientId is not configured"
**Solution:** Set User Secrets or environment variables for Google OAuth

**Issue:** "Rate limit exceeded"
**Solution:** Increase `DailyTokenLimit` or wait until next day (resets at midnight UTC)

**Issue:** "File size exceeds maximum"
**Solution:** Increase `FileUpload:MaxFileSizeBytes` or compress files before upload

**Issue:** "Authentication failed"
**Solution:** Verify email is in `EmailWhitelist.AllowedEmails`

---

## Support

For more information:
- **Secrets Management**: See `SECRETS.md`
- **Deployment**: See deployment documentation (TBD)
- **API Documentation**: See Swagger UI at `/swagger` (development only)

