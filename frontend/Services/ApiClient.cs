using System.Net.Http.Json;
using Aski.Tickets.Web.Models;

namespace Aski.Tickets.Web.Services;

/// <summary>Wrapper tipizzato sulle API (HttpClient con bearer handler).</summary>
public sealed class ApiClient
{
    private readonly HttpClient _http;
    public ApiClient(HttpClient http) => _http = http;

    // --- Tickets ---
    public async Task<TicketListResult> GetTicketsAsync(TicketStatus? status = null, TicketPriority? priority = null,
        int? companyId = null, string? q = null, int page = 1, int pageSize = 20)
    {
        var qs = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (status is not null) qs.Add($"status={(int)status}");
        if (priority is not null) qs.Add($"priority={(int)priority}");
        if (companyId is not null) qs.Add($"companyId={companyId}");
        if (!string.IsNullOrWhiteSpace(q)) qs.Add($"q={Uri.EscapeDataString(q)}");
        return await _http.GetFromJsonAsync<TicketListResult>("api/tickets?" + string.Join("&", qs))
               ?? new TicketListResult(new(), 0, page, pageSize);
    }

    public Task<TicketDetail?> GetTicketAsync(int id) => _http.GetFromJsonAsync<TicketDetail>($"api/tickets/{id}");

    public Task<HttpResponseMessage> CreateTicketAsync(CreateTicketRequest req) =>
        _http.PostAsJsonAsync("api/tickets", req);
    public Task<HttpResponseMessage> UpdateTicketAsync(int id, CreateTicketRequest req) =>
        _http.PutAsJsonAsync($"api/tickets/{id}", req);

    public Task<HttpResponseMessage> AddCommentAsync(int id, AddCommentRequest req) =>
        _http.PostAsJsonAsync($"api/tickets/{id}/comments", req);

    public Task<HttpResponseMessage> ChangeStatusAsync(int id, TicketStatus status) =>
        _http.PatchAsJsonAsync($"api/tickets/{id}/status", new ChangeStatusRequest(status));

    public Task<HttpResponseMessage> TakeAsync(int id, int? unitId) =>
        _http.PostAsJsonAsync($"api/tickets/{id}/take", new TakeRequest(unitId));
    public Task<HttpResponseMessage> AssignAsync(int id, string userId, int unitId) =>
        _http.PostAsJsonAsync($"api/tickets/{id}/assign", new AssignRequest(userId, unitId));
    public Task<HttpResponseMessage> UnassignAsync(int id) =>
        _http.PostAsync($"api/tickets/{id}/unassign", null);
    public Task<HttpResponseMessage> CloseAsync(int id) =>
        _http.PostAsync($"api/tickets/{id}/close", null);
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

    public Task<HttpResponseMessage> UploadAttachmentAsync(int id, MultipartFormDataContent content) =>
        _http.PostAsync($"api/tickets/{id}/attachments", content);
    public Task<byte[]> GetAttachmentBytesAsync(int id, int attId) =>
        _http.GetByteArrayAsync($"api/tickets/{id}/attachments/{attId}");

    // --- Impostazioni brand ---
    public Task<BrandSettings?> GetBrandAsync() => _http.GetFromJsonAsync<BrandSettings>("api/settings");
    public Task<HttpResponseMessage> UpdateBrandAsync(string brandName) =>
        _http.PutAsJsonAsync("api/settings", new { brandName });
    public Task<HttpResponseMessage> UploadBrandImageAsync(bool logo, MultipartFormDataContent content) =>
        _http.PostAsync(logo ? "api/settings/logo" : "api/settings/favicon", content);
    public Task<HttpResponseMessage> DeleteBrandImageAsync(bool logo) =>
        _http.DeleteAsync(logo ? "api/settings/logo" : "api/settings/favicon");

    // --- Lookup ---
    public async Task<List<AppUserRow>> GetStaffUsersAsync() =>
        await _http.GetFromJsonAsync<List<AppUserRow>>("api/lookup/staff") ?? new();
    public async Task<List<MyUnit>> GetMyUnitsAsync() =>
        await _http.GetFromJsonAsync<List<MyUnit>>("api/lookup/my-units") ?? new();

    // --- Units ---
    public async Task<List<UnitRow>> GetUnitsAsync() =>
        await _http.GetFromJsonAsync<List<UnitRow>>("api/units") ?? new();
    public Task<UnitDetail?> GetUnitAsync(int id) => _http.GetFromJsonAsync<UnitDetail>($"api/units/{id}");
    public Task<HttpResponseMessage> CreateUnitAsync(CreateUnitRequest req) => _http.PostAsJsonAsync("api/units", req);
    public Task<HttpResponseMessage> SetUnitManagersAsync(int id, List<string> userIds) =>
        _http.PutAsJsonAsync($"api/units/{id}/managers", new UserIdsRequest(userIds));
    public Task<HttpResponseMessage> AddUnitMemberAsync(int id, string userId) =>
        _http.PostAsJsonAsync($"api/units/{id}/members", new UserIdRequest(userId));
    public Task<HttpResponseMessage> RemoveUnitMemberAsync(int id, string userId) =>
        _http.DeleteAsync($"api/units/{id}/members/{userId}");

    // --- Rubrica ---
    public async Task<List<Contact>> GetContactsAsync(int companyId) =>
        await _http.GetFromJsonAsync<List<Contact>>($"api/companies/{companyId}/contacts") ?? new();
    public Task<HttpResponseMessage> CreateContactAsync(int companyId, CreateContactRequest req) =>
        _http.PostAsJsonAsync($"api/companies/{companyId}/contacts", req);
    public Task<HttpResponseMessage> DeleteContactAsync(int companyId, int id) =>
        _http.DeleteAsync($"api/companies/{companyId}/contacts/{id}");

    // --- Companies ---
    public async Task<List<Company>> GetCompaniesAsync() =>
        await _http.GetFromJsonAsync<List<Company>>("api/companies") ?? new();
    public Task<HttpResponseMessage> CreateCompanyAsync(CreateCompanyRequest req) =>
        _http.PostAsJsonAsync("api/companies", req);
    public Task<HttpResponseMessage> SetCompanySoftwareAsync(int id, List<int> softwareIds) =>
        _http.PutAsJsonAsync($"api/companies/{id}/software", new SoftwareIdsRequest(softwareIds));
    public Task<Company?> GetCompanyAsync(int id) => _http.GetFromJsonAsync<Company>($"api/companies/{id}");
    public async Task<List<CompanyTicket>> GetCompanyTicketsAsync(int id) =>
        await _http.GetFromJsonAsync<List<CompanyTicket>>($"api/companies/{id}/tickets") ?? new();
    public Task<HttpResponseMessage> UpdateCompanyAsync(int id, CreateCompanyRequest req) =>
        _http.PutAsJsonAsync($"api/companies/{id}", req);
    public async Task<List<CompanyUser>> GetCompanyUsersAsync(int id) =>
        await _http.GetFromJsonAsync<List<CompanyUser>>($"api/companies/{id}/users") ?? new();

    // --- Software ---
    public async Task<List<Software>> GetSoftwareAsync() =>
        await _http.GetFromJsonAsync<List<Software>>("api/software") ?? new();
    public Task<SoftwareDetail?> GetSoftwareDetailAsync(int id) =>
        _http.GetFromJsonAsync<SoftwareDetail>($"api/software/{id}");
    public Task<HttpResponseMessage> CreateSoftwareAsync(CreateSoftwareRequest req) =>
        _http.PostAsJsonAsync("api/software", req);
    public Task<HttpResponseMessage> UpdateSoftwareAsync(int id, CreateSoftwareRequest req) =>
        _http.PutAsJsonAsync($"api/software/{id}", req);
    public async Task<List<SoftwareVersion>> GetVersionsAsync(int softwareId) =>
        await _http.GetFromJsonAsync<List<SoftwareVersion>>($"api/software/{softwareId}/versions") ?? new();
    public Task<HttpResponseMessage> AddVersionAsync(int softwareId, CreateVersionRequest req) =>
        _http.PostAsJsonAsync($"api/software/{softwareId}/versions", req);
    public Task<HttpResponseMessage> UpdateVersionAsync(int softwareId, int versionId, CreateVersionRequest req) =>
        _http.PutAsJsonAsync($"api/software/{softwareId}/versions/{versionId}", req);
    public Task<HttpResponseMessage> DeleteVersionAsync(int softwareId, int versionId) =>
        _http.DeleteAsync($"api/software/{softwareId}/versions/{versionId}");
    public Task<HttpResponseMessage> SetVersionActiveAsync(int softwareId, int versionId, bool enabled) =>
        _http.PostAsync($"api/software/{softwareId}/versions/{versionId}/active/{enabled.ToString().ToLower()}", null);

    // --- Users ---
    public async Task<List<AppUserRow>> GetUsersAsync() =>
        await _http.GetFromJsonAsync<List<AppUserRow>>("api/users") ?? new();
    public async Task<List<ClientRow>> GetClientUsersAsync() =>
        await _http.GetFromJsonAsync<List<ClientRow>>("api/users/clients") ?? new();
    public Task<HttpResponseMessage> CreateUserAsync(CreateUserRequest req) =>
        _http.PostAsJsonAsync("api/users", req);
    public Task<HttpResponseMessage> UpdateUserAsync(string id, UpdateUserRequest req) =>
        _http.PutAsJsonAsync($"api/users/{id}", req);
    public Task<AppUserRow?> GetUserAsync(string id) => _http.GetFromJsonAsync<AppUserRow>($"api/users/{id}");
    public Task<HttpResponseMessage> SetUserSoftwareAsync(string id, List<int> softwareIds) =>
        _http.PutAsJsonAsync($"api/users/{id}/software", new SoftwareIdsRequest(softwareIds));
    public Task<HttpResponseMessage> SetUserActiveAsync(string id, bool enabled) =>
        _http.PostAsync($"api/users/{id}/active/{enabled.ToString().ToLower()}", null);
    public Task<HttpResponseMessage> ResetPasswordAsync(string id, string newPassword) =>
        _http.PostAsJsonAsync($"api/users/{id}/reset-password", new ResetPasswordRequest(newPassword));
    public async Task<List<UserUnit>> GetUserUnitsAsync(string id) =>
        await _http.GetFromJsonAsync<List<UserUnit>>($"api/users/{id}/units") ?? new();
    public async Task<List<UserTicket>> GetUserAssignedTicketsAsync(string id) =>
        await _http.GetFromJsonAsync<List<UserTicket>>($"api/users/{id}/tickets") ?? new();
    public async Task<List<UserTicket>> GetUserVisibleTicketsAsync(string id) =>
        await _http.GetFromJsonAsync<List<UserTicket>>($"api/users/{id}/visible-tickets") ?? new();
}
