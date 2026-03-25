using Microsoft.EntityFrameworkCore;
using FoodTour_WebAdmin.Api.Data;
using MudBlazor.Services;
// Thêm 2 namespace này để sử dụng PasswordHasher và UserModel
using Microsoft.AspNetCore.Identity;
using FoodTour_WebAdmin.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// ═══════ SERVICES ═══════

// EF Core + PostgreSQL (Supabase) via IDbContextFactory (tránh lỗi ObjectDisposedException trong Blazor Server)
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Services
builder.Services.AddHttpClient<FoodTour_WebAdmin.Api.Services.LangblyTranslateService>();
builder.Services.AddSingleton<FoodTour_WebAdmin.Api.Services.ISupabaseStorageService, FoodTour_WebAdmin.Api.Services.SupabaseStorageService>();
builder.Services.AddHttpClient<FoodTour_WebAdmin.Api.Services.ITtsService, FoodTour_WebAdmin.Api.Services.GoogleTtsService>();
builder.Services.AddSingleton<FoodTour_WebAdmin.Api.Services.IQrCodeService, FoodTour_WebAdmin.Api.Services.QrCodeService>();
builder.Services.AddScoped<FoodTour_WebAdmin.Api.Services.ManageFoodTourService>();
builder.Services.AddScoped<FoodTour_WebAdmin.Api.Services.AuthService>();

// API Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Blazor Server Auth & Components
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, FoodTour_WebAdmin.Api.Services.CustomAuthStateProvider>();
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

// ═══════ ĐOẠN MÃ TẠO MÃ HASH (Tạm thời) ═══════
// Đoạn này sẽ in mã Hash ra Console khi bạn chạy project
// using (var scope = app.Services.CreateScope())
// {
//     var hasher = new PasswordHasher<UserModel>();
//     var dummyUser = new UserModel { Email = "admin@foodtour.com" };
//     string secureHash = hasher.HashPassword(dummyUser, "Admin123");

//     Console.WriteLine("\n========================================");
//     Console.WriteLine("--- COPY MA HASH NAY CHO SUPABASE ---");
//     Console.WriteLine(secureHash);
//     Console.WriteLine("========================================\n");
// }

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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