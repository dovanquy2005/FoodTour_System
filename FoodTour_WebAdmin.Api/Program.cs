using Microsoft.EntityFrameworkCore;
using FoodTour_WebAdmin.Api.Data;
using MudBlazor.Services;
// Thêm 2 namespace này để sử dụng PasswordHasher và UserModel
using Microsoft.AspNetCore.Identity;
using FoodTour_WebAdmin.Api.Models;
using FoodTour_WebAdmin.Api.Hubs;

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

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<FoodTour_WebAdmin.Api.Services.GitHubReleaseService>();

// SignalR — cho phép Server đẩy thông báo cập nhật tới Mobile App theo thời gian thực
builder.Services.AddSignalR();

// API Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Blazor Server Auth & Components
builder.Services.AddAuthorizationCore();

// Đăng ký Cookie Authentication Scheme để thoả mãn ASP.NET Core Authorization Middleware.
// QUAN TRỌNG: Không đặt LoginPath — để Cookie middleware không tự Challenge (redirect /login)
// các anonymous request. Việc redirect do AuthorizeRouteView + RedirectToLogin đảm nhiệm
// ở tầng Blazor, đảm bảo [AllowAnonymous] trên các trang công khai (FoodTour.razor...) hoạt động.
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Không set LoginPath — tắt auto-challenge của Cookie middleware
        // (nếu set, nó sẽ redirect cả [AllowAnonymous] pages về /login)
        options.Events.OnRedirectToLogin = ctx =>
        {
            // Trả về 401 thay vì redirect — Blazor AuthorizeRouteView sẽ xử lý tiếp
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

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

// CORS — cho phép MAUI app và mọi client truy cập API
// Lưu ý: SignalR yêu cầu AllowCredentials nên không dùng AllowAnyOrigin được,
// thay vào đó dùng SetIsOriginAllowed để chấp nhận mọi origin.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
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

// Authentication & Authorization Middleware (phải sau UseCors, trước MapControllers)
app.UseAuthentication();
app.UseAuthorization();

// API Controllers
app.MapControllers();

// SignalR Hub — endpoint để Mobile App kết nối nhận thông báo real-time
app.MapHub<UpdateHub>("/api/updatesHub");

// Blazor
app.MapRazorComponents<FoodTour_WebAdmin.Api.Components.App>()
    .AddInteractiveServerRenderMode()
    .AllowAnonymous(); // Bỏ qua chặn 401 ở tầng HTTP, để AuthorizeRouteView tự xử lý qua LocalStorage.

FoodTour_WebAdmin.Api.Constants.ServiceProvider = app.Services;
app.Run();