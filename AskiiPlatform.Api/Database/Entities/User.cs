using Askii.Common;
using Askii.Common.Exceptions;
using Askii.Common.Extensions;
using Askii.Database.Entities.Common;

namespace Askii.Database.Entities;

public class User : BaseEntity
{
    public string Email {get;private set;} = string.Empty;
    public string PasswordHash {get;private set;} = string.Empty;
    public string Name {get;  set;} = string.Empty;
    public string LastName {get; set;} = string.Empty;
    public string Role {get;private set;} = Roles.Client;
    public bool IsSuperAdmin {get; private set;} = false;
    public bool IsActive {get;set;} = false;
    public DateTime? LastLoginUtc {get; private set;}

    public string FullName { get => $"{Name} {LastName}";}

    private User() {}

    public static User Create(
        string email,
        string password,
        string? name,
        string? lastName,
        string? role
    )
    {
        if(!Roles.All.Any(x => x == role))
        {
            throw new InvalidUserRoleException(role ?? string.Empty, Roles.All);
        }

        var user = new User
        {
            Email = email,
            PasswordHash = string.Empty,
            Name = name ?? string.Empty,
            LastName = lastName ?? string.Empty,
            Role = role ?? Roles.Client,
            IsSuperAdmin = false,
            IsActive = false
        };
        user.SetPassword(password);
        return user;
    }

    public static User CreateSuperAdmin(
        string email,
        string password,
        string? name,
        string? lastName
    )
    {
        User superAdmin = User.Create(email, password, name, lastName, Roles.Admin);
        superAdmin.IsActive = true;
        superAdmin.IsSuperAdmin = true;

        return superAdmin;
    }

    public void SetPassword(string psw)
    {
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(psw);
    }

    public void SetEmail(string email)
    {
        if(!email.NormalizeEmail().IsValidEmail()) throw new InvalidEmailException(email.NormalizeEmail());
        Email = email.NormalizeEmail();
    }

    public void UpdateAnag(string? name, string lastName)
    {
        Name = name ?? Name;
        LastName = lastName ?? LastName;
    }

    public void UpdateRole(string role)
    {
        if(!Roles.All.Any(x => x == role))
        {
            throw new InvalidUserRoleException(role, Roles.All);
        }

        if(IsSuperAdmin && role != Roles.Admin)
        {
            throw new InvalidSuperAdminRoleException(Email);
        }

        Role = role;
    }

    public bool VerifyPassword(string plainPassword) => BCrypt.Net.BCrypt.Verify(plainPassword, PasswordHash);

    public void RecordLogin()
    {
        LastLoginUtc = DateTime.UtcNow;
    }
}