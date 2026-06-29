namespace Aski.Tickets.Web.Models;

public enum TicketStatus { Open = 0, InProgress = 1, Waiting = 2, Resolved = 3, Closed = 4 }
public enum TicketPriority { Low = 0, Normal = 1, High = 2, Urgent = 3 }

public record UserInfo(string Id, string Email, string? FullName, int? CompanyId, List<string> Roles);
public record AuthResponse(string AccessToken, string RefreshToken, int ExpiresInSeconds, UserInfo User);

public record TicketListItem(
    int Id, string Title, TicketStatus Status, TicketPriority Priority,
    int CompanyId, int? SoftwareId, int? SoftwareVersionId, string? AssigneeUserId, int? AssigneeUnitId,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc, DateTime? ClosedAtUtc);

public record TicketComment(
    int Id, string Body, bool IsInternal, string AuthorUserId, DateTime CreatedAtUtc,
    string? AuthorFirst, string? AuthorLast, string? AuthorEmail, bool AuthorIsStaff)
{
    public string AuthorName
    {
        get
        {
            var n = $"{AuthorFirst} {AuthorLast}".Trim();
            return string.IsNullOrWhiteSpace(n) ? (AuthorEmail ?? "Utente") : n;
        }
    }
}
public record TicketAssignmentDto(int Id, int UnitId, string UserId);

public record TicketDetail(
    int Id, string Title, string? Description, TicketStatus Status, TicketPriority Priority,
    int CompanyId, int? SoftwareId, int? SoftwareVersionId, string CreatedByUserId,
    string? AssigneeUserId, int? AssigneeUnitId,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc, DateTime? ClosedAtUtc,
    List<TicketAssignmentDto> Assignments, List<TicketComment> Comments);

// Unit
public record UnitRow(int Id, string Name, string? Description, int MembersCount, List<string> Managers);
public record UnitMemberDto(string UserId, bool IsManager, string Email, string? FirstName, string? LastName);
public record UnitDetail(int Id, string Name, string? Description, List<UnitMemberDto> Members);

public record MyUnit(int UnitId, string Name, bool IsManager);

// Rubrica
public record Contact(int Id, string Name, string? Title, string? Email, string? Phone, string? Notes);

public record Company(
    int Id, string Name, string? VatNumber, string? ContactEmail, string? Phone, string? Address,
    bool IsActive, DateTime CreatedAtUtc, int UsersCount, List<int> SoftwareIds);

public record Software(int Id, string Name, string? Description, int VersionsCount, string? LatestVersion);
public record SoftwareDetail(int Id, string Name, string? Description, bool IsActive);
public record SoftwareVersion(int Id, string Version, string? Notes, DateTime? ReleasedAtUtc, bool IsActive, DateTime CreatedAtUtc);
public record CompanyUser(string Id, string Email, string? FirstName, string? LastName, string? Phone, bool IsActive);

public record AppUserRow(
    string Id, string Email, string? FirstName, string? LastName, string? JobTitle, string? Phone,
    int? CompanyId, bool IsActive, List<int> SoftwareIds, List<string> Roles)
{
    public string Display => string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

public record ClientRow(
    string Id, string Email, string? FirstName, string? LastName, string? JobTitle, string? Phone,
    int? CompanyId, string? CompanyName, bool IsActive, List<string> Roles)
{
    public string Display => string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

// Richieste
public record LoginRequest(string Email, string Password);
public record CreateTicketRequest(string Title, string? Description, int? SoftwareId, int? SoftwareVersionId, TicketPriority Priority, int? CompanyId);
public record AddCommentRequest(string Body, bool IsInternal);
public record ChangeStatusRequest(TicketStatus Status);
public record AssignRequest(string UserId, int UnitId);
public record TakeRequest(int? UnitId);
public record CreateUnitRequest(string Name, string? Description);
public record UserIdsRequest(List<string> UserIds);
public record UserIdRequest(string UserId);
public record CreateContactRequest(string Name, string? Title, string? Email, string? Phone, string? Notes);
public record CreateCompanyRequest(string Name, string? VatNumber, string? ContactEmail, string? Phone, string? Address);
public record CreateSoftwareRequest(string Name, string? Description);
public record CreateVersionRequest(string Version, string? Notes, DateTime? ReleasedAtUtc);
public record CreateUserRequest(
    string Email, string Password, string Role,
    string? FirstName, string? LastName, string? JobTitle, string? Phone, int? CompanyId, List<int>? SoftwareIds);
public record UpdateUserRequest(string? FirstName, string? LastName, string? JobTitle, string? Phone, int? CompanyId);
public record SoftwareIdsRequest(List<int> SoftwareIds);
