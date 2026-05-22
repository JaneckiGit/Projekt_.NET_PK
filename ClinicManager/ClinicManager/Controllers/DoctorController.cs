using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Controllers;

[Authorize(Roles = "Lekarz,Admin")]
public class DoctorController : Controller
{
    public IActionResult Index() => View();
}