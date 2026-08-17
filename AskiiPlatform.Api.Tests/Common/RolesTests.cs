using Askii.Common;

namespace Askii.Tests.Common;

public class RolesTests
{
    [Fact]
    public void All_contiene_esattamente_i_tre_ruoli_previsti()
        => Assert.Equal(new[] { "Admin", "Operator", "Client" }, Roles.All);

    [Fact]
    public void I_ruoli_sono_case_sensitive_come_scritti()
    {
        Assert.Contains(Roles.Admin, Roles.All);
        Assert.DoesNotContain("admin", Roles.All);
    }
}
