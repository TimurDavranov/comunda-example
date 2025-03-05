using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Camunda_TZ.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;

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
        var idToken = await HttpContext.GetTokenAsync(OpenIdConnectDefaults.AuthenticationScheme, "id_token");
        
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        var authUrl = configuration["Keycloak:Authority"]?.TrimEnd('/').TrimEnd();

        var logoutUrl = $"{authUrl}/protocol/openid-connect/logout";
        var postLogoutRedirectUri = Url.Action("Index", "Home", null, "http");

        var logoutRequest = $"{logoutUrl}?id_token_hint={idToken}&post_logout_redirect_uri={Uri.EscapeDataString(postLogoutRedirectUri)}";

        return Redirect(logoutRequest);
    }
}