namespace Aski.Tickets.Portal.Models;

public enum TicketStatus { Open = 0, InProgress = 1, Waiting = 2, Resolved = 3, Closed = 4 }
public enum TicketPriority { Low = 0, Normal = 1, High = 2, Urgent = 3 }

public record UserInfo(string Id, string Email, string? FullName, int? CompanyId, List<string> Roles);
public record AuthResponse(string AccessToken, string RefreshToken, int ExpiresInSeconds, UserInfo User);
public record LoginRequest(string Email, string Password);

// --- Ticket ---
public record TicketListItem(
    int Id, string? Number, string Title, TicketStatus Status, TicketPriority Priority,
    int CompanyId, string? CompanyName, int? SoftwareId, string? SoftwareName,
    string? AssignedUserId, string? AssignedUserName, string? AssignedUnitName,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc, DateTime? ClosedAtUtc);

public record TicketListResult(List<TicketListItem> Items, int Total, int Page, int PageSize);

public record TicketAttachmentDto(int Id, string FileName, string ContentType, long Size, DateTime CreatedAtUtc);

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

public record TicketDetail(
    int Id, string? Number, string Title, string? Description, TicketStatus Status, TicketPriority Priority,
    int CompanyId, string? CompanyName, int? SoftwareId, int? SoftwareVersionId, string CreatedByUserId,
    string? AssignedUserId, string? AssignedUserName, string? AssignedUserEmail, int? AssignedUnitId, string? AssignedUnitName,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc, DateTime? ClosedAtUtc,
    List<TicketAttachmentDto> Attachments, List<TicketComment> Comments);

public record CreateTicketRequest(string Title, string? Description, int? SoftwareId, int? SoftwareVersionId, TicketPriority Priority, int? CompanyId);
public record AddCommentRequest(string Body, bool IsInternal);

// --- Software / release notes ---
public record PortalSoftwareVersion(int Id, string Version, string? ReleaseNotes, DateTime? ReleasedAtUtc, bool IsActive, DateTime CreatedAtUtc);
public record PortalSoftware(int Id, string Name, string? Description, List<PortalSoftwareVersion> Versions);

// --- Operatori ---
public record PortalOperator(string Id, string Email, string? FirstName, string? LastName, string? JobTitle, string? Phone, List<string> Softwares, List<string> Roles)
{
    public string Display => string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

// --- Profilo ---
public record PortalProfile(string Id, string Email, string? FirstName, string? LastName, string? JobTitle, string? Phone, int? CompanyId, string? CompanyName)
{
    public string Display => string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
