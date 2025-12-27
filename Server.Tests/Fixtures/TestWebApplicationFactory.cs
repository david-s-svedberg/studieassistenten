using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Server.Services;
using StudieAssistenten.Server.Services.AI;
using StudieAssistenten.Server.Services.AI.Abstractions;
using StudieAssistenten.Server.Tests.Mocks;

namespace StudieAssistenten.Server.Tests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for integration testing.
/// Configures in-memory SQLite database and mocks external services.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove the existing DbContext configuration
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            // Create and open SQLite in-memory connection
            // Connection must stay open for the lifetime of the test
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Add DbContext with in-memory SQLite
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Mock external services to avoid real API calls

            // Mock AI Provider (new abstraction) - Singleton so tests can verify call count
            services.RemoveAll<IAiProvider>();
            services.AddSingleton<IAiProvider, MockAiProvider>();

            // Mock Anthropic API (legacy - for backward compatibility) - Singleton so tests can verify call count
            services.RemoveAll<IAnthropicApiClient>();
            services.AddSingleton<IAnthropicApiClient, MockAnthropicClient>();

            // Mock OCR Service (Azure Computer Vision) - Singleton so tests can access same instance
            services.RemoveAll<IOcrService>();
            services.AddSingleton<IOcrService, MockOcrService>();

            // Configure test authentication - PostConfigure runs after main app configuration
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = AuthenticationFixture.TestAuthScheme;
                options.DefaultAuthenticateScheme = AuthenticationFixture.TestAuthScheme;
                options.DefaultChallengeScheme = AuthenticationFixture.TestAuthScheme;
            });

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    AuthenticationFixture.TestAuthScheme,
                    options => { });

            // Build service provider and ensure database is created
            var serviceProvider = services.BuildServiceProvider();
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Ensure database is created with schema
                dbContext.Database.EnsureCreated();
            }
        });

        // Use test environment
        builder.UseEnvironment("Testing");
    }

    public new void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}
