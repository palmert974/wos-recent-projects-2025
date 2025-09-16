using Microsoft.AspNetCore.Mvc;

namespace TheVinylCountdownLikes.Controllers;

public class ErrorController : Controller
{
    [HttpGet("error/{code}")]
    public IActionResult Handle(int code)
    {
        if (code == 404)
        {
            // Serve a custom view for 404 errors
            return View("PageNotFound");
        }

        // Handle other error codes
        return View("ServerError");
    }

    [HttpGet("error/boom")]
    public IActionResult Boom()
    {
        // This is a test method that will intentionally throw a 500 error.
        // It's a useful way to test our custom error page without introducing a bug.
        return new StatusCodeResult(500);
    }
}
