using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;
using StudieAssistenten.Client;
using StudieAssistenten.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure client-side logging
builder.Logging.SetMinimumLevel(LogLevel.Warning);
// Filter Blazor framework logs to reduce WASM noise
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.Extensions", LogLevel.Warning);

// Configure HttpClient
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Authentication
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<CustomAuthStateProvider>());

// Register application services
builder.Services.AddScoped<ITestService, TestService>();
builder.Services.AddScoped<ITestShareService, TestShareService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ContentGenerationService>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddSingleton<TestStateService>(); // Singleton to share state across components

await builder.Build().RunAsync();
