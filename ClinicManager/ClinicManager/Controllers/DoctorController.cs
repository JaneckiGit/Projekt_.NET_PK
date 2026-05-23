using ClinicManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Controllers;

[Authorize(Roles = Roles.Lekarz + "," + Roles.Admin)]
public class DoctorController : Controller
{
    public IActionResult Index() => View();
}
