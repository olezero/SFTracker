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

// Per-machine overrides (usersettings.json); decides which database file is used.
builder.Services.AddSingleton<UserSettingsService>();

// Add Entity Framework. The connection string is read per scope so a database path change in the
// settings UI takes effect for new circuits without restarting the app.
builder.Services.AddDbContext<SFTrackerDbContext>((serviceProvider, options) => {
	options.UseSqlite(serviceProvider.GetRequiredService<UserSettingsService>().ConnectionString);
	options.EnableDetailedErrors();
	options.EnableSensitiveDataLogging();
});

// Add services
builder.Services.AddSingleton<GameInfoService>();
builder.Services.AddScoped<FactoryService>();
builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<NetworkService>();
builder.Services.AddScoped<GameTransferService>();
builder.Services.AddScoped<DataImportService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// Load the game reference data (parts, recipes, machines)
var gameInfoService = app.Services.GetRequiredService<GameInfoService>();
await gameInfoService.InitializeAsync(Path.Combine(app.Environment.ContentRootPath, "game_data.json"));

// Create/seed the database (see DatabaseInitializer; also re-run when the database path changes)
await DatabaseInitializer.InitializeAsync(app.Services);

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Run();
