using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Server.Services;
using StudieAssistenten.Shared.Models;
using System.Security.Claims;
using System.Net;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);

// Add Application Insights telemetry
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});

// Add services to the container.

// Configure SQLite database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=studieassistenten.db"));

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings (not used for Google OAuth, but required by Identity)
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // User settings
    options.User.RequireUniqueEmail = true;

    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = false; // Google confirms email
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme; // Use Cookie, not Google
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    var isDevelopment = builder.Environment.IsDevelopment();
    
    options.Cookie.Name = "StudieAssistenten.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = isDevelopment 
        ? CookieSecurePolicy.SameAsRequest  // Allow HTTP in development
        : CookieSecurePolicy.Always;         // HTTPS only in production
    options.Cookie.SameSite = isDevelopment 
        ? SameSiteMode.Lax                  // More permissive in development
        : SameSiteMode.None;                 // Required for CORS in production
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/login";
    options.LogoutPath = "/api/auth/logout";
    options.AccessDeniedPath = "/login?error=access_denied";
    
    // Prevent API endpoints from redirecting to login page (return 401 instead)
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    
    // Prevent API endpoints from redirecting on access denied
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// Add Google authentication (skip in test environment)
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddAuthentication().AddGoogle("Google", options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]
            ?? throw new InvalidOperationException("Google ClientId not configured");
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
            ?? throw new InvalidOperationException("Google ClientSecret not configured");
        options.SaveTokens = true;

        // Request user profile information
        options.Scope.Add("profile");
        options.Scope.Add("email");

        // Increase timeout and configure backchannel for IPv4
        options.BackchannelTimeout = TimeSpan.FromSeconds(30);
        options.BackchannelHttpHandler = new SocketsHttpHandler
        {
            ConnectTimeout = TimeSpan.FromSeconds(15),
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectCallback = async (context, cancellationToken) =>
            {
                // Force IPv4 to avoid IPv6 timeout issues
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    NoDelay = true
                };
                try
                {
                    await socket.ConnectAsync(context.DnsEndPoint, cancellationToken).ConfigureAwait(false);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        };

        // Add event handlers for better error reporting
        options.Events.OnRemoteFailure = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(context.Failure, "Google OAuth remote failure");

            context.Response.Redirect($"/login?error=oauth_failure&message={Uri.EscapeDataString(context.Failure?.Message ?? "Unknown error")}");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });
}

builder.Services.AddAuthorization();

// Register authorization handlers
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, StudieAssistenten.Server.Authorization.TestAuthorizationHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, StudieAssistenten.Server.Authorization.DocumentAuthorizationHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, StudieAssistenten.Server.Authorization.GeneratedContentAuthorizationHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, StudieAssistenten.Server.Authorization.TestShareAuthorizationHandler>();

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Register repositories
builder.Services.AddScoped<StudieAssistenten.Server.Infrastructure.Repositories.ITestRepository, StudieAssistenten.Server.Infrastructure.Repositories.TestRepository>();
builder.Services.AddScoped<StudieAssistenten.Server.Infrastructure.Repositories.IDocumentRepository, StudieAssistenten.Server.Infrastructure.Repositories.DocumentRepository>();
builder.Services.AddScoped<StudieAssistenten.Server.Infrastructure.Repositories.IGeneratedContentRepository, StudieAssistenten.Server.Infrastructure.Repositories.GeneratedContentRepository>();
builder.Services.AddScoped<StudieAssistenten.Server.Infrastructure.Repositories.ITestShareRepository, StudieAssistenten.Server.Infrastructure.Repositories.TestShareRepository>();

// Register infrastructure services
builder.Services.AddSingleton<StudieAssistenten.Server.Infrastructure.Storage.IFileStorage, StudieAssistenten.Server.Infrastructure.Storage.LocalFileStorage>();

// Register application services
builder.Services.AddSingleton<IEmailWhitelistService, EmailWhitelistService>();
builder.Services.AddScoped<ITestService, TestService>();
builder.Services.AddScoped<ITestShareService, TestShareService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IFileValidationService, FileValidationService>();
builder.Services.AddScoped<IOcrService, AzureComputerVisionOcrService>();
builder.Services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
builder.Services.AddScoped<IRateLimitingService, RateLimitingService>();

// AI services - Provider abstraction
builder.Services.AddScoped<StudieAssistenten.Server.Services.AI.Abstractions.IAiProvider, StudieAssistenten.Server.Services.AI.Providers.AnthropicAiProvider>();
builder.Services.AddScoped<StudieAssistenten.Server.Services.AI.Abstractions.IAiProvider, StudieAssistenten.Server.Services.AI.Providers.GeminiAiProvider>();
builder.Services.AddScoped<StudieAssistenten.Server.Services.AI.AiProviderFactory>();

// Legacy Anthropic client (for backward compatibility with tests)
builder.Services.AddScoped<StudieAssistenten.Server.Services.AI.IAnthropicApiClient, StudieAssistenten.Server.Services.AI.AnthropicApiClient>();

// Content generators
builder.Services.AddScoped<StudieAssistenten.Server.Services.AI.Generators.IFlashcardGenerator, StudieAssistenten.Server.Services.AI.Generators.FlashcardGenerator>();
builder.Services.AddScoped<StudieAssistenten.Server.Services.AI.Generators.IPracticeTestGenerator, StudieAssistenten.Server.Services.AI.Generators.PracticeTestGenerator>();
builder.Services.AddScoped<StudieAssistenten.Server.Services.AI.Generators.ISummaryGenerator, StudieAssistenten.Server.Services.AI.Generators.SummaryGenerator>();
builder.Services.AddScoped<StudieAssistenten.Server.Services.AI.ITestNamingService, StudieAssistenten.Server.Services.AI.TestNamingService>();
builder.Services.AddScoped<IAiContentGenerationService, AiContentGenerationService>();

// PDF generation services
builder.Services.AddScoped<IFlashcardPdfGenerationService, FlashcardPdfGenerationService>();
builder.Services.AddScoped<IPracticeTestPdfGenerationService, PracticeTestPdfGenerationService>();
builder.Services.AddScoped<ISummaryPdfGenerationService, SummaryPdfGenerationService>();

// Configure Kestrel to allow larger request body sizes (60MB)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 60 * 1024 * 1024; // 60 MB
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 60 * 1024 * 1024; // 60 MB
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Configure HSTS (HTTP Strict Transport Security)
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365); // 1 year (recommended for production)
    options.IncludeSubDomains = true;        // Apply to all subdomains
    options.Preload = true;                  // Enable HSTS preload list submission
});

// Configure CORS (only needed in Development or when API/Client are on different origins)
var corsEnabled = builder.Configuration.GetValue<bool>("Cors:EnableCors");
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

if (corsEnabled && allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowBlazorClient",
            policy => policy
                .WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()); // Required for cookie authentication
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
}

// HTTPS enforcement - always redirect HTTP to HTTPS
app.UseHttpsRedirection();

// HSTS (HTTP Strict Transport Security) - enforce HTTPS on client side
// Only send in production to avoid issues with localhost development
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// Note: Uploaded files are NOT served as static files for security reasons.
// They are served through the DocumentsController with authorization checks.
// Ensure uploads directory exists
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseRouting();

// Apply CORS policy if configured (Development or separate deployments)
if (corsEnabled && allowedOrigins.Length > 0)
{
    app.UseCors("AllowBlazorClient");
}

// Security headers middleware
app.Use(async (context, next) =>
{
    // Content Security Policy - Prevents XSS attacks
    // Adjusted for Blazor WebAssembly requirements
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-eval' 'wasm-unsafe-eval' 'sha256-AA99+JSnoA8VDU0S18bLsAs2mB/pE6UorFNrO+yEj0E=' 'sha256-rix1Vs83ItBtb257nN0MhMQIyfZxlSmE12KoEoUV6po=' 'sha256-yei5Fza+Eyx4G0smvN0xBqEesIKumz6RSyGsU3FJowI=' 'sha256-e4ZVW9jRfeAm2B9N1Iy64Gt0SVjlulOOfv6EZaZz2IQ='; " +  // Required for Blazor WASM + inline scripts + flashcard player
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +  // Required for Blazor inline styles + Bootstrap Icons CDN
        "img-src 'self' data: https: blob:; " +                   // blob: required for image preview from File objects
        "font-src 'self' https://cdn.jsdelivr.net; " +            // Bootstrap Icons fonts from CDN
        "connect-src 'self' https://accounts.google.com https://oauth2.googleapis.com; " +  // Google OAuth
        "frame-src 'self' https://accounts.google.com; " +        // Google OAuth iframe
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'self'; " +                               // Allow same-origin iframes (for document viewer)
        "upgrade-insecure-requests;");

    // X-Content-Type-Options - Prevents MIME-sniffing
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

    // X-Frame-Options - Prevents clickjacking (allow same-origin for document viewer)
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");

    // Referrer-Policy - Controls referrer information
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

    // Permissions-Policy - Restricts browser features
    context.Response.Headers.Append("Permissions-Policy",
        "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");

    await next();
});

// Authentication & Authorization (ORDER MATTERS)
app.UseAuthentication();
app.UseAuthorization();

// Initialize database and apply migrations (skip in test environment)
if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            // Apply pending migrations
            dbContext.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error applying database migrations");
            throw;
        }
    }
}


app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
