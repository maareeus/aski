using Aski.ControlPlane.Entities;
using Aski.Shared;

namespace Aski.ControlPlane.Web.ViewModels;

/// <summary>Dati della dashboard Super Admin.</summary>
public sealed class DashboardViewModel
{
    public bool StripeConfigured { get; set; }
    public bool IsTestMode { get; set; }
    public int PlanCount { get; set; }
    public int ServerCount { get; set; }
    public int TenantCount { get; set; }
    public int ProjectCount { get; set; }
    public int ActiveSubscriptions { get; set; }
    public List<Project> RecentProjects { get; set; } = new();
}

/// <summary>Form impostazioni Stripe. I segreti si reinseriscono solo per cambiarli.</summary>
public sealed class StripeSettingsViewModel
{
    public bool Configured { get; set; }
    public bool IsTestMode { get; set; } = true;

    public string? TestPublishableKey { get; set; }
    public bool TestSecretKeySet { get; set; }
    public bool TestWebhookSecretSet { get; set; }

    public string? LivePublishableKey { get; set; }
    public bool LiveSecretKeySet { get; set; }
    public bool LiveWebhookSecretSet { get; set; }

    // Campi di input (solo scrittura): vuoti = lascia invariato.
    public string? NewTestSecretKey { get; set; }
    public string? NewTestWebhookSecret { get; set; }
    public string? NewLiveSecretKey { get; set; }
    public string? NewLiveWebhookSecret { get; set; }
}

/// <summary>Vista piani + form di creazione.</summary>
public sealed class PlansViewModel
{
    public List<Plan> Plans { get; set; } = new();
    public bool StripeConfigured { get; set; }

    // form
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "eur";
    public BillingPeriod Period { get; set; } = BillingPeriod.Monthly;
}

/// <summary>Vista server + form di creazione.</summary>
public sealed class ServersViewModel
{
    public List<Server> Servers { get; set; } = new();

    // form
    public string Name { get; set; } = "";
    public string Region { get; set; } = "";
    public ServerType Type { get; set; } = ServerType.VpsDocker;
    public string? Hostname { get; set; }
    public string? ConfigJson { get; set; }
    public int MaxProjectsPerDbContainer { get; set; } = 10;
    public bool IsEnabled { get; set; } = true;
}

/// <summary>Lista tenant + form registrazione.</summary>
public sealed class PortalIndexViewModel
{
    public List<Tenant> Tenants { get; set; } = new();
    public string CompanyName { get; set; } = "";
    public string BillingEmail { get; set; } = "";
}

/// <summary>Dashboard di un singolo tenant: progetti, abbonamenti, acquisto.</summary>
public sealed class TenantDetailViewModel
{
    public Tenant Tenant { get; set; } = null!;
    public List<Plan> AvailablePlans { get; set; } = new();
    public List<Server> EnabledServers { get; set; } = new();

    // form nuovo progetto
    public string ProjectName { get; set; } = "";
    public int ServerId { get; set; }
    public string? Subdomain { get; set; }
    public string? CustomDomain { get; set; }
}
