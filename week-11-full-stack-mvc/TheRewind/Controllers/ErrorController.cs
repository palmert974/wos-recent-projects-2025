using Microsoft.AspNetCore.Mvc;

namespace TheRewind.Controllers;

public class ErrorController : Controller
{
    // Beginner note: Maps status codes (404/401/403/500) to friendly pages.
    private readonly IHostEnvironment _env;

    public ErrorController(IHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet("error/{code}")]
    public IActionResult Handle(int code)
    {
        if (code == 404)
        {
            return View("PageNotFound");
        }
        else if (code == 401)
        {
            return View("Unauthorized");
        }
        else if (code == 403)
        {
            return View("Forbidden");
        }

        // Default to server error page
        return View("ServerError");
    }

    // Convenience routes to intentionally trigger status codes for testing (Development only)
    [HttpGet("error/not-found")]
    public IActionResult IntentionalNotFound()
    {
        if (!_env.IsDevelopment())
            return NotFound();
        return new StatusCodeResult(404);
    }

    [HttpGet("error/unauthorized")]
    public IActionResult IntentionalUnauthorized()
    {
        if (!_env.IsDevelopment())
            return NotFound();
        return new StatusCodeResult(401);
    }

    [HttpGet("error/forbidden")]
    public IActionResult IntentionalForbidden()
    {
        if (!_env.IsDevelopment())
            return NotFound();
        return new StatusCodeResult(403);
    }

    [HttpGet("error/boom")]
    public IActionResult Boom()
    {
        if (!_env.IsDevelopment())
            return NotFound();
        return new StatusCodeResult(500);
    }
}
