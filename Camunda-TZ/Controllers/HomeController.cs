using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Camunda_TZ.Models;
using Microsoft.AspNetCore.Authentication;

namespace Camunda_TZ.Controllers;

//[Authorize]
public class HomeController(IConfiguration configuration) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("Cookies");

        return SignOut("oidc");
    }
}