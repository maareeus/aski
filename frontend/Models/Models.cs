namespace Aski.Tickets.Web.Models;

public enum TicketStatus { Open = 0, InProgress = 1, Waiting = 2, Resolved = 3, Closed = 4 }
public enum TicketPriority { Low = 0, Normal = 1, High = 2, Urgent = 3 }

public record UserInfo(string Id, string Email, string? FullName, int? CompanyId, List<string> Roles);

public record AuthResponse(string AccessToken, string RefreshToken, int ExpiresInSeconds, UserInfo User);

public record TicketListItem(
    int Id, string Title, TicketStatus Status, TicketPriority Priority,
    int CompanyId, int? SoftwareId, string? AssignedAgentUserId,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc, DateTime? ClosedAtUtc);

public record TicketComment(int Id, string Body, bool IsInternal, string AuthorUserId, DateTime CreatedAtUtc);

public record TicketDetail(
    int Id, string Title, string? Description, TicketStatus Status, TicketPriority Priority,
    int CompanyId, int? SoftwareId, string CreatedByUserId, string? AssignedAgentUserId,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc, DateTime? ClosedAtUtc,
    List<TicketComment> Comments);

public record Company(int Id, string Name, string? ContactEmail, bool IsActive, DateTime CreatedAtUtc);
public record Software(int Id, string Name, string? Description, string? Version);
public record AppUserRow(string Id, string Email, string? FullName, int? CompanyId, bool IsActive, List<string> Roles);

// Richieste
public record LoginRequest(string Email, string Password);
public record CreateTicketRequest(string Title, string? Description, int? SoftwareId, TicketPriority Priority, int? CompanyId);
public record AddCommentRequest(string Body, bool IsInternal);
public record ChangeStatusRequest(TicketStatus Status);
public record AssignRequest(string AgentUserId);
public record CreateCompanyRequest(string Name, string? ContactEmail);
public record CreateSoftwareRequest(string Name, string? Description, string? Version);
public record CreateUserRequest(string Email, string Password, string Role, string? FullName, int? CompanyId);
