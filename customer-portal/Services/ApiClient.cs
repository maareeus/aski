using System.Net.Http.Json;
using Aski.Tickets.Portal.Models;

namespace Aski.Tickets.Portal.Services;

/// <summary>Wrapper tipizzato sulle API per il Customer Portal.</summary>
public sealed class ApiClient
{
    private readonly HttpClient _http;
    public ApiClient(HttpClient http) => _http = http;

    // --- Tickets (l'API è già limitata all'azienda del client) ---
    public async Task<TicketListResult> GetTicketsAsync(TicketStatus? status = null, string? q = null, int page = 1, int pageSize = 20)
    {
        var qs = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (status is not null) qs.Add($"status={(int)status}");
        if (!string.IsNullOrWhiteSpace(q)) qs.Add($"q={Uri.EscapeDataString(q)}");
        return await _http.GetFromJsonAsync<TicketListResult>("api/tickets?" + string.Join("&", qs))
               ?? new TicketListResult(new(), 0, page, pageSize);
    }

    public Task<TicketDetail?> GetTicketAsync(int id) => _http.GetFromJsonAsync<TicketDetail>($"api/tickets/{id}");

    public Task<HttpResponseMessage> CreateTicketAsync(CreateTicketRequest req) =>
        _http.PostAsJsonAsync("api/tickets", req);

    public Task<HttpResponseMessage> AddCommentAsync(int id, string body) =>
        _http.PostAsJsonAsync($"api/tickets/{id}/comments", new AddCommentRequest(body, false));

    public Task<HttpResponseMessage> UploadAttachmentAsync(int id, MultipartFormDataContent content) =>
        _http.PostAsync($"api/tickets/{id}/attachments", content);
    public Task<byte[]> GetAttachmentBytesAsync(int id, int attId) =>
        _http.GetByteArrayAsync($"api/tickets/{id}/attachments/{attId}");
    public Task<HttpResponseMessage> RemindAsync(int id) =>
        _http.PostAsync($"api/tickets/{id}/remind", null);

    // --- Notifiche ---
    public async Task<List<NotificationDto>> GetNotificationsAsync(bool unreadOnly = false) =>
        await _http.GetFromJsonAsync<List<NotificationDto>>($"api/notifications?unreadOnly={unreadOnly.ToString().ToLower()}") ?? new();
    public async Task<int> GetUnreadCountAsync()
    {
        try { var r = await _http.GetFromJsonAsync<UnreadCount>("api/notifications/unread-count"); return r?.Count ?? 0; }
        catch { return 0; }
    }
    public Task<HttpResponseMessage> MarkNotificationReadAsync(int id) =>
        _http.PostAsync($"api/notifications/{id}/read", null);
    public Task<HttpResponseMessage> MarkAllNotificationsReadAsync() =>
        _http.PostAsync("api/notifications/read-all", null);
    public Task<HttpResponseMessage> DeleteNotificationAsync(int id) =>
        _http.DeleteAsync($"api/notifications/{id}");
    public Task<HttpResponseMessage> DeleteAllNotificationsAsync() =>
        _http.DeleteAsync("api/notifications");

    // --- Portal-only ---
    public Task<PortalProfile?> GetMeAsync() => _http.GetFromJsonAsync<PortalProfile>("api/portal/me");
    public async Task<List<PortalSoftware>> GetSoftwareAsync() =>
        await _http.GetFromJsonAsync<List<PortalSoftware>>("api/portal/software") ?? new();
    public async Task<List<PortalOperator>> GetOperatorsAsync() =>
        await _http.GetFromJsonAsync<List<PortalOperator>>("api/portal/operators") ?? new();

    // --- Account ---
    public Task<HttpResponseMessage> ChangePasswordAsync(string current, string @new) =>
        _http.PostAsJsonAsync("api/auth/change-password", new ChangePasswordRequest(current, @new));
}
