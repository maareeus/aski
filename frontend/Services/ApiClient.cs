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

    public Task<HttpResponseMessage> AssignAsync(int id, string agentUserId) =>
        _http.PostAsJsonAsync($"api/tickets/{id}/assign", new AssignRequest(agentUserId));

    public Task<HttpResponseMessage> CloseAsync(int id) =>
        _http.PostAsync($"api/tickets/{id}/close", null);

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
