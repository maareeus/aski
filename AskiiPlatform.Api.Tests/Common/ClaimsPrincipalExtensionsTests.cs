using System.Security.Claims;
using Askii.Common.Extensions;

namespace Askii.Tests.Common;

public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal PrincipalWith(params Claim[] claims)
        => new(new ClaimsIdentity(claims, "Test"));

    [Fact]
    public void CurrentUserId_legge_il_claim_NameIdentifier()
    {
        var id = Guid.NewGuid();
        var principal = PrincipalWith(new Claim(ClaimTypes.NameIdentifier, id.ToString()));

        Assert.Equal(id, principal.CurrentUserId());
    }

    [Fact]
    public void CurrentUserEmail_legge_il_claim_Email()
    {
        var principal = PrincipalWith(new Claim(ClaimTypes.Email, "mario@example.com"));

        Assert.Equal("mario@example.com", principal.CurrentUserEmail());
    }

    [Fact]
    public void CurrentUserRole_legge_il_claim_Role()
    {
        var principal = PrincipalWith(new Claim(ClaimTypes.Role, "Admin"));

        Assert.Equal("Admin", principal.CurrentUserRole());
    }

    // --- comportamento in assenza dei claim: nessuna gestione graceful ---

    [Fact]
    public void CurrentUserId_senza_claim_solleva_ArgumentNullException()
    {
        var principal = PrincipalWith();

        Assert.Throws<ArgumentNullException>(() => principal.CurrentUserId());
    }

    [Fact]
    public void CurrentUserId_con_claim_non_guid_solleva_FormatException()
    {
        var principal = PrincipalWith(new Claim(ClaimTypes.NameIdentifier, "non-un-guid"));

        Assert.Throws<FormatException>(() => principal.CurrentUserId());
    }

    [Fact]
    public void CurrentUserEmail_senza_claim_ritorna_null_nonostante_il_tipo_non_nullable()
    {
        var principal = PrincipalWith();

        // Il metodo dichiara `string` ma il `!` sopprime il null: qui torna davvero null.
        Assert.Null(principal.CurrentUserEmail());
    }
}
