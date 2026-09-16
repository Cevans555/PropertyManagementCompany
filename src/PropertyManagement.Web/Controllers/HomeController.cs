using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Web.Models;
using PropertyManagement.Web.Queries;

namespace PropertyManagement.Web.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    private readonly HomeQueries _home;

    public HomeController(HomeQueries home)
    {
        _home = home;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await _home.DashboardAsync(User, cancellationToken));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
