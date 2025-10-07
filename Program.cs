using Blzr.BootstrapSelect;
using Microsoft.EntityFrameworkCore;
using SFTracker.Components;
using SFTracker.Data;
using SFTracker.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

// Configure host shutdown timeout for immediate shutdown
builder.Services.Configure<HostOptions>(options => {
	options.ShutdownTimeout = TimeSpan.FromSeconds(0);
});

builder.Services.AddBlazorBootstrap();
builder.Services.AddBootstrapSelect(defaults => {
	defaults.ShowSearch = true;
});

// Add Entity Framework
builder.Services.AddDbContext<SFTrackerDbContext>(options =>
	options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=sftracker.db"));

// Add services
builder.Services.AddSingleton<GameInfoService>();
builder.Services.AddScoped<FactoryService>();
builder.Services.AddScoped<DataImportService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

// Initialize game info service
var gameInfoService = services.GetRequiredService<GameInfoService>();
await gameInfoService.InitializeAsync();

// Initialize database
var context = services.GetRequiredService<SFTrackerDbContext>();
await context.Database.EnsureCreatedAsync();

// Import parts if needed
var dataImportService = services.GetRequiredService<DataImportService>();
await dataImportService.ImportPartsAsync();

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Run();
