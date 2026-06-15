using ClinicManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Controllers;

[Authorize(Roles = Roles.Rejestratorka + "," + Roles.Admin)]
public class ReceptionController : Controller
{
    private readonly ILogger<ReceptionController> _logger;

    public ReceptionController(ILogger<ReceptionController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index() => View();
}
