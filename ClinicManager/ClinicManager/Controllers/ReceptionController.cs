using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Controllers;

[Authorize(Roles = "Rejestratorka,Admin")]
public class ReceptionController : Controller
{
    public IActionResult Index() => View();
}