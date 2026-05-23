using ClinicManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Controllers;

[Authorize(Roles = Roles.Admin)]
public class AdminController : Controller
{
    public IActionResult Index() => View();
}
