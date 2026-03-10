using Microsoft.EntityFrameworkCore;
using FoodTour_WebAdmin.Api.Data;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// ═══════ SERVICES ═══════

// EF Core + SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
                     ?? "Data Source=foodtour.db"));

// Register Services
builder.Services.AddHttpClient<FoodTour_WebAdmin.Api.Services.LangblyTranslateService>();
builder.Services.AddScoped<FoodTour_WebAdmin.Api.Services.ManageFoodTourService>();

// API Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// MudBlazor
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 3000;
    config.SnackbarConfiguration.ShowTransitionDuration = 200;
    config.SnackbarConfiguration.HideTransitionDuration = 200;
    config.SnackbarConfiguration.SnackbarVariant = MudBlazor.Variant.Filled;
});

// CORS — allow MAUI app and any client to consume the API
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ═══════ DATABASE INITIALIZATION ═══════
// Use EF Core Migrations instead (dotnet ef database update)

// ═══════ MIDDLEWARE PIPELINE ═══════
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// CORS
app.UseCors();

// API Controllers
app.MapControllers();

// Blazor
app.MapRazorComponents<FoodTour_WebAdmin.Api.Components.App>()
    .AddInteractiveServerRenderMode();

FoodTour_WebAdmin.Api.Constants.ServiceProvider = app.Services;
app.Run();
