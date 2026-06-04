using LogDashboard.Web.Models;
using LogDashboard.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Bind LogDashboard config section ──
builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection("LogDashboard"));

// ── Services ──
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<LogParserService>();   // stateless, safe as singleton
builder.Services.AddScoped<DashboardState>();        // per Blazor circuit
builder.Services.AddScoped<LogWatcherService>();     // per Blazor circuit

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<LogDashboard.Web.Components.App>()
   .AddInteractiveServerRenderMode();

app.Run();
