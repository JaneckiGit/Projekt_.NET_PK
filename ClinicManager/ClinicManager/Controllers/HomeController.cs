using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ClinicManager.Models;
using Microsoft.AspNetCore.Authorization;

namespace ClinicManager.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (User.IsInRole(Roles.Admin)) return RedirectToAction("Index", "Admin");
        if (User.IsInRole(Roles.Lekarz)) return RedirectToAction("Index", "Doctor");
        if (User.IsInRole(Roles.Rejestratorka)) return RedirectToAction("Index", "Reception");

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}