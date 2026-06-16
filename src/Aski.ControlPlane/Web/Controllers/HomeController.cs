using Aski.ControlPlane.Web;
using Microsoft.AspNetCore.Mvc;

namespace Aski.ControlPlane.Web.Controllers;

/// <summary>Punto d'ingresso: smista l'utente loggato verso l'area corretta.</summary>
public sealed class HomeController : Controller
{
    public IActionResult Index()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return RedirectToAction("Login", "Account");

        return User.IsSuperAdmin()
            ? RedirectToAction("Index", "Dashboard")
            : RedirectToAction("Index", "Portal");
    }
}
