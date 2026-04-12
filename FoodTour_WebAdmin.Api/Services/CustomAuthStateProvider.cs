using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;
using FoodTour_WebAdmin.Api.Models;

namespace FoodTour_WebAdmin.Api.Services;

/// <summary>
/// DTO nhỏ gọn cho session — chỉ lưu những gì cần thiết để tái tạo ClaimsPrincipal.
/// Không lưu PasswordHash hay thông tin nhạy cảm.
/// </summary>
public sealed class UserSessionData
{
    public string Id       { get; set; } = string.Empty;
    public string Email    { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role     { get; set; } = string.Empty;
}

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private const string StorageKey = "user_session";

    private readonly ProtectedLocalStorage _storage;

    // Cache in-memory cho lần gọi tiếp theo trong cùng Scoped lifetime
    // (tránh đọc LocalStorage lặp đi lặp lại trên mỗi lần render)
    private ClaimsPrincipal? _cachedUser;

    public CustomAuthStateProvider(ProtectedLocalStorage protectedLocalStorage)
    {
        _storage = protectedLocalStorage;
    }

    // ══════════════════════════════════════════════════════════════════════
    // GetAuthenticationStateAsync — được gọi bởi Blazor mỗi khi cần biết
    // user hiện tại là ai. Sau khi F5, _cachedUser = null và phải đọc lại
    // từ LocalStorage.
    // ══════════════════════════════════════════════════════════════════════
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Nếu đã có cache trong phiên làm việc hiện tại, trả về ngay
        if (_cachedUser != null)
            return new AuthenticationState(_cachedUser);

        // ── PRERENDERING GUARD ─────────────────────────────────────────────
        // Trong giai đoạn Prerender (Server-Side Rendering lần đầu), Blazor
        // chưa thiết lập kết nối WebSocket/SignalR nên JS Interop chưa hoạt
        // động. Gọi LocalStorage lúc này sẽ ném InvalidOperationException:
        //   "JavaScript interop calls cannot be issued at this time"
        // → Bắt lỗi này và trả về AuthState rỗng (anonymous). Sau khi trang
        //   hydrate xong, Blazor sẽ gọi lại hàm này qua circuit thật và
        //   LocalStorage sẽ đọc được bình thường.
        // ──────────────────────────────────────────────────────────────────
        try
        {
            var result = await _storage.GetAsync<UserSessionData>(StorageKey);

            if (!result.Success || result.Value is null)
                return Anonymous();

            // Tái tạo ClaimsPrincipal từ dữ liệu đã lưu
            var session = result.Value;
            _cachedUser = BuildPrincipal(session);
            return new AuthenticationState(_cachedUser);
        }
        catch (InvalidOperationException)
        {
            // Prerender phase — JS chưa sẵn sàng, trả về anonymous tạm thời
            return Anonymous();
        }
        catch (Exception ex)
        {
            // Lỗi không xác định (VD: session bị corrupt, decrypt fail)
            // → Xóa session lỗi để tránh loop và trả về anonymous
            System.Diagnostics.Debug.WriteLine(
                $"[AuthStateProvider] Lỗi đọc session: {ex.Message}");

            try { await _storage.DeleteAsync(StorageKey); } catch { /* ignore */ }

            return Anonymous();
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // MarkUserAsAuthenticated — gọi sau khi login thành công.
    // Lưu session xuống LocalStorage (mã hóa bởi ASP.NET Data Protection)
    // và thông báo cho Blazor cập nhật UI ngay lập tức.
    // ══════════════════════════════════════════════════════════════════════
    public async Task MarkUserAsAuthenticated(UserModel user)
    {
        var session = new UserSessionData
        {
            Id       = user.Id,
            Email    = user.Email,
            FullName = user.FullName,
            Role     = user.Role,
        };

        // Lưu vào ProtectedLocalStorage (ASP.NET Data Protection tự mã hóa —
        // người dùng không thể đọc/giả mạo giá trị này trong DevTools)
        await _storage.SetAsync(StorageKey, session);

        _cachedUser = BuildPrincipal(session);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_cachedUser)));
    }

    // ══════════════════════════════════════════════════════════════════════
    // MarkUserAsLoggedOut — gọi khi Logout.
    // Xóa session khỏi LocalStorage và reset cache.
    // ══════════════════════════════════════════════════════════════════════
    public async Task MarkUserAsLoggedOut()
    {
        try
        {
            await _storage.DeleteAsync(StorageKey);
        }
        catch
        {
            // Bỏ qua nếu session đã không còn (VD: đã bị xóa ở tab khác)
        }

        _cachedUser = null;
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous()));
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private static ClaimsPrincipal BuildPrincipal(UserSessionData session)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, session.Id),
            new Claim(ClaimTypes.Name,           session.FullName),
            new Claim(ClaimTypes.Email,          session.Email),
            new Claim(ClaimTypes.Role,           session.Role),
        }, authenticationType: "CustomAuth");

        return new ClaimsPrincipal(identity);
    }

    private static AuthenticationState Anonymous()
        => new(new ClaimsPrincipal(new ClaimsIdentity()));
}
