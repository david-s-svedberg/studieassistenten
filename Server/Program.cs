using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure SQLite database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=studieassistenten.db"));

// Register application services
builder.Services.AddScoped<IFileUploadService, FileUploadService>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Configure CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
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

app.UseRouting();

app.UseCors("AllowBlazorClient");

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}


app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
