using System.Net.Http.Json;
using Aski.Tickets.Web.Models;

namespace Aski.Tickets.Web.Services;

/// <summary>Wrapper tipizzato sulle API (HttpClient con bearer handler).</summary>
public sealed class ApiClient
{
    private readonly HttpClient _http;
    public ApiClient(HttpClient http) => _http = http;

    // --- Tickets ---
    public async Task<List<TicketListItem>> GetTicketsAsync(TicketStatus? status = null)
    {
        var url = "api/tickets" + (status is null ? "" : $"?status={(int)status}");
        return await _http.GetFromJsonAsync<List<TicketListItem>>(url) ?? new();
    }

    public Task<TicketDetail?> GetTicketAsync(int id) => _http.GetFromJsonAsync<TicketDetail>($"api/tickets/{id}");

    public Task<HttpResponseMessage> CreateTicketAsync(CreateTicketRequest req) =>
        _http.PostAsJsonAsync("api/tickets", req);

    public Task<HttpResponseMessage> AddCommentAsync(int id, AddCommentRequest req) =>
        _http.PostAsJsonAsync($"api/tickets/{id}/comments", req);

    public Task<HttpResponseMessage> ChangeStatusAsync(int id, TicketStatus status) =>
        _http.PatchAsJsonAsync($"api/tickets/{id}/status", new ChangeStatusRequest(status));

    public Task<HttpResponseMessage> TakeAsync(int id, int? unitId) =>
        _http.PostAsJsonAsync($"api/tickets/{id}/take", new TakeRequest(unitId));
    public Task<HttpResponseMessage> AddAssignmentAsync(int id, string userId, int unitId) =>
        _http.PostAsJsonAsync($"api/tickets/{id}/assignments", new AssignRequest(userId, unitId));
    public Task<HttpResponseMessage> RemoveAssignmentAsync(int id, int assignmentId) =>
        _http.DeleteAsync($"api/tickets/{id}/assignments/{assignmentId}");

    public Task<HttpResponseMessage> CloseAsync(int id) =>
        _http.PostAsync($"api/tickets/{id}/close", null);

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

    // --- Users ---
    public async Task<List<AppUserRow>> GetUsersAsync() =>
        await _http.GetFromJsonAsync<List<AppUserRow>>("api/users") ?? new();
    public Task<HttpResponseMessage> CreateUserAsync(CreateUserRequest req) =>
        _http.PostAsJsonAsync("api/users", req);
    public Task<HttpResponseMessage> SetUserSoftwareAsync(string id, List<int> softwareIds) =>
        _http.PutAsJsonAsync($"api/users/{id}/software", new SoftwareIdsRequest(softwareIds));
}
