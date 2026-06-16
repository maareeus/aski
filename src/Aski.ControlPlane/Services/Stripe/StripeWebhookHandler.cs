using Aski.ControlPlane.Data;
using Aski.ControlPlane.Entities;
using Aski.ControlPlane.Services.Provisioning;
using Aski.Shared;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
// Disambigua dall'omonima entità Stripe.Subscription (quella di Stripe è usata via global::Stripe).
using Subscription = Aski.ControlPlane.Entities.Subscription;

namespace Aski.ControlPlane.Services.Stripe;

/// <summary>
/// Macchina a stati degli abbonamenti pilotata dagli eventi Stripe.
/// Traduce gli eventi nel <see cref="SubscriptionStatus"/> interno e, quando uno
/// stato cambia, invoca <see cref="IProvisioningCoordinator"/> per pilotare i
/// container del progetto collegato.
///
/// L'idempotenza (evento già processato) è gestita a monte dal controller.
/// </summary>
public sealed class StripeWebhookHandler
{
    private readonly ControlPlaneDbContext _db;
    private readonly IProvisioningCoordinator _coordinator;
    private readonly ILogger<StripeWebhookHandler> _log;

    public StripeWebhookHandler(
        ControlPlaneDbContext db,
        IProvisioningCoordinator coordinator,
        ILogger<StripeWebhookHandler> log)
    {
        _db = db;
        _coordinator = coordinator;
        _log = log;
    }

    /// <summary>Smista un evento Stripe già verificato sul gestore appropriato.</summary>
    public async Task HandleAsync(Event stripeEvent, CancellationToken ct = default)
    {
        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                await OnCheckoutCompletedAsync((Session)stripeEvent.Data.Object, ct);
                break;

            case "invoice.paid":
                await OnInvoicePaidAsync((Invoice)stripeEvent.Data.Object, ct);
                break;

            case "customer.subscription.created":
            case "customer.subscription.updated":
                await OnSubscriptionUpsertAsync((global::Stripe.Subscription)stripeEvent.Data.Object, ct);
                break;

            case "customer.subscription.deleted":
                await OnSubscriptionDeletedAsync((global::Stripe.Subscription)stripeEvent.Data.Object, ct);
                break;

            default:
                _log.LogDebug("Evento Stripe ignorato: {Type}", stripeEvent.Type);
                break;
        }
    }

    /// <summary>
    /// Checkout completato: crea/collega la Subscription interna a partire dai
    /// metadata (tenantId, planId) e dall'id subscription di Stripe.
    /// </summary>
    private async Task OnCheckoutCompletedAsync(Session session, CancellationToken ct)
    {
        if (session.Mode != "subscription" || string.IsNullOrWhiteSpace(session.SubscriptionId))
            return;

        session.Metadata.TryGetValue("tenantId", out var tenantIdRaw);
        session.Metadata.TryGetValue("planId", out var planIdRaw);
        int.TryParse(tenantIdRaw, out var tenantId);
        int.TryParse(planIdRaw, out var planId);

        var sub = await GetOrCreateSubscriptionAsync(session.SubscriptionId, session.CustomerId, tenantId, planId, ct);
        if (sub is null) return;

        _log.LogInformation("Checkout completato: subscription interna {Id} <- stripe {Stripe}",
            sub.Id, session.SubscriptionId);
        // Lo stato effettivo arriva con invoice.paid / subscription.created.
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Fattura pagata: attiva l'abbonamento e avvia il provisioning.</summary>
    private async Task OnInvoicePaidAsync(Invoice invoice, CancellationToken ct)
    {
        // In Stripe.net 52 l'id subscription della fattura è sotto Parent.SubscriptionDetails.
        var stripeSubId = invoice.Parent?.SubscriptionDetails?.SubscriptionId;
        if (string.IsNullOrWhiteSpace(stripeSubId)) return;

        var sub = await LoadByStripeIdAsync(stripeSubId, ct);
        if (sub is null)
        {
            _log.LogWarning("invoice.paid per subscription sconosciuta {Stripe}", stripeSubId);
            return;
        }

        await ApplyStatusAsync(sub, SubscriptionStatus.Active, ct);
    }

    /// <summary>Subscription creata/aggiornata: sincronizza stato, periodo, disdetta.</summary>
    private async Task OnSubscriptionUpsertAsync(global::Stripe.Subscription stripeSub, CancellationToken ct)
    {
        stripeSub.Metadata.TryGetValue("tenantId", out var tenantIdRaw);
        stripeSub.Metadata.TryGetValue("planId", out var planIdRaw);
        int.TryParse(tenantIdRaw, out var tenantId);
        int.TryParse(planIdRaw, out var planId);

        var sub = await GetOrCreateSubscriptionAsync(stripeSub.Id, stripeSub.CustomerId, tenantId, planId, ct);
        if (sub is null) return;

        sub.CancelAtPeriodEnd = stripeSub.CancelAtPeriodEnd;
        sub.CurrentPeriodEndUtc = stripeSub.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd;

        await ApplyStatusAsync(sub, MapStatus(stripeSub.Status), ct);
    }

    /// <summary>Subscription cancellata: stato Canceled e stop container (dati conservati).</summary>
    private async Task OnSubscriptionDeletedAsync(global::Stripe.Subscription stripeSub, CancellationToken ct)
    {
        var sub = await LoadByStripeIdAsync(stripeSub.Id, ct);
        if (sub is null) return;
        await ApplyStatusAsync(sub, SubscriptionStatus.Canceled, ct);
    }

    /// <summary>
    /// Applica una transizione di stato e, se cambia, pilota l'infrastruttura del
    /// progetto collegato tramite il coordinatore.
    /// </summary>
    private async Task ApplyStatusAsync(Subscription sub, SubscriptionStatus newStatus, CancellationToken ct)
    {
        var previous = sub.Status;
        sub.Status = newStatus;
        sub.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        if (previous == newStatus)
            return;

        var projectId = sub.Project?.Id ?? await _db.Projects
            .Where(p => p.SubscriptionId == sub.Id)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(ct);

        if (projectId is null)
        {
            _log.LogInformation("Subscription {Id}: {Prev}->{Next}, nessun progetto collegato (nessuna azione infra)",
                sub.Id, previous, newStatus);
            return;
        }

        _log.LogInformation("Subscription {Id}: {Prev}->{Next} -> pilota progetto {ProjectId}",
            sub.Id, previous, newStatus, projectId);

        switch (newStatus)
        {
            case SubscriptionStatus.Active:
                // Da sospeso si riavvia, altrimenti provisioning completo.
                if (previous is SubscriptionStatus.PastDue or SubscriptionStatus.Suspended)
                    await _coordinator.ResumeAsync(projectId.Value, ct);
                else
                    await _coordinator.ProvisionAndStartAsync(projectId.Value, ct);
                break;

            case SubscriptionStatus.PastDue:
            case SubscriptionStatus.Suspended:
                await _coordinator.SuspendAsync(projectId.Value, ct);
                break;

            case SubscriptionStatus.Canceled:
                await _coordinator.StopAsync(projectId.Value, ct);
                break;
        }
    }

    // --- helper ---

    private Task<Subscription?> LoadByStripeIdAsync(string stripeSubId, CancellationToken ct) =>
        _db.Subscriptions.Include(s => s.Project)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubId, ct);

    /// <summary>
    /// Recupera la subscription per id Stripe o la crea se assente (i metadata
    /// forniscono tenant e piano). Ritorna null se non si riesce a correlare.
    /// </summary>
    private async Task<Subscription?> GetOrCreateSubscriptionAsync(
        string stripeSubId, string? stripeCustomerId, int tenantId, int planId, CancellationToken ct)
    {
        var sub = await LoadByStripeIdAsync(stripeSubId, ct);
        if (sub is not null)
        {
            if (sub.StripeCustomerId is null && stripeCustomerId is not null)
                sub.StripeCustomerId = stripeCustomerId;
            return sub;
        }

        if (tenantId == 0 || planId == 0)
        {
            _log.LogWarning("Impossibile creare subscription {Stripe}: metadata tenant/plan mancanti", stripeSubId);
            return null;
        }

        sub = new Subscription
        {
            TenantId = tenantId,
            PlanId = planId,
            StripeSubscriptionId = stripeSubId,
            StripeCustomerId = stripeCustomerId,
            Status = SubscriptionStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        _db.Subscriptions.Add(sub);
        await _db.SaveChangesAsync(ct);
        return sub;
    }

    /// <summary>Mappa lo stato testuale di Stripe sullo stato interno.</summary>
    private static SubscriptionStatus MapStatus(string stripeStatus) => stripeStatus switch
    {
        "active" or "trialing" => SubscriptionStatus.Active,
        "past_due" => SubscriptionStatus.PastDue,
        "unpaid" => SubscriptionStatus.Suspended,
        "canceled" or "incomplete_expired" => SubscriptionStatus.Canceled,
        _ => SubscriptionStatus.Pending // incomplete
    };
}
