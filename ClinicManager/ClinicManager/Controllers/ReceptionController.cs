using ClinicManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Controllers;

[Authorize(Roles = Roles.Rejestratorka + "," + Roles.Admin)]
public class ReceptionController : Controller
{
    public IActionResult Index() => View();
}
