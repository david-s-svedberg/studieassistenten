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
})
.AddGoogle("Google", options =>
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

builder.Services.AddAuthorization();

// Register authorization handlers
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, StudieAssistenten.Server.Authorization.TestAuthorizationHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, StudieAssistenten.Server.Authorization.DocumentAuthorizationHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, StudieAssistenten.Server.Authorization.GeneratedContentAuthorizationHandler>();

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Register repositories
builder.Services.AddScoped<StudieAssistenten.Server.Infrastructure.Repositories.ITestRepository, StudieAssistenten.Server.Infrastructure.Repositories.TestRepository>();
builder.Services.AddScoped<StudieAssistenten.Server.Infrastructure.Repositories.IDocumentRepository, StudieAssistenten.Server.Infrastructure.Repositories.DocumentRepository>();
builder.Services.AddScoped<StudieAssistenten.Server.Infrastructure.Repositories.IGeneratedContentRepository, StudieAssistenten.Server.Infrastructure.Repositories.GeneratedContentRepository>();

// Register infrastructure services
builder.Services.AddSingleton<StudieAssistenten.Server.Infrastructure.Storage.IFileStorage, StudieAssistenten.Server.Infrastructure.Storage.LocalFileStorage>();

// Register application services
builder.Services.AddScoped<IEmailWhitelistService, EmailWhitelistService>();
builder.Services.AddScoped<ITestService, TestService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IFileValidationService, FileValidationService>();
builder.Services.AddScoped<IOcrService, TesseractOcrService>();
builder.Services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
builder.Services.AddScoped<IRateLimitingService, RateLimitingService>();

// AI services
builder.Services.AddScoped<StudieAssistenten.Server.Services.AI.IAnthropicApiClient, StudieAssistenten.Server.Services.AI.AnthropicApiClient>();
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

// Configure CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient",
        policy => policy
            .WithOrigins(
                "https://localhost:7247",  // Server URL (Blazor WASM is hosted here)
                "http://localhost:5059"     // Alternative HTTP port
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()); // Required for cookie authentication
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

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

app.UseCors("AllowBlazorClient");

// Security headers middleware
app.Use(async (context, next) =>
{
    // Content Security Policy - Prevents XSS attacks
    // Adjusted for Blazor WebAssembly requirements
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-eval' 'wasm-unsafe-eval' 'sha256-AA99+JSnoA8VDU0S18bLsAs2mB/pE6UorFNrO+yEj0E='; " +  // Required for Blazor WASM + inline PWA script
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +  // Required for Blazor inline styles + Bootstrap Icons CDN
        "img-src 'self' data: https:; " +
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

// Initialize database and apply migrations
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


app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
