using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheRewind.Models;
using TheRewind.Services;
using TheRewind.ViewModels;

namespace TheRewind.Controllers;

[Route("account")]
public class AccountController : Controller
{
    // Beginner note: This controller handles register, login, logout, and a protected profile page.
    private readonly ApplicationContext _context;
    private readonly IPasswordService _passwords;

    public AccountController(ApplicationContext context, IPasswordService passwords)
    {
        _context = context;
        _passwords = passwords;
    }

    [HttpGet("register")]
    public IActionResult Register()
    {
        // UX: already logged-in users go straight to /movies
        if (HttpContext.Session.GetInt32("userId") != null)
        {
            return RedirectToAction("Index", "Movies");
        }
        return View();
    }

    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        // Beginner note: Normalize input before validation (trim; lowercase email)
        vm.Username = (vm.Username ?? string.Empty).Trim();
        vm.Email = (vm.Email ?? string.Empty).Trim().ToLowerInvariant();
        vm.Password = (vm.Password ?? string.Empty).Trim();
        vm.ConfirmPassword = (vm.ConfirmPassword ?? string.Empty).Trim();

        if (!ModelState.IsValid)
            return View(vm);

        bool usernameTaken = await _context.Users.AnyAsync(u => u.Username == vm.Username);
        if (usernameTaken)
        {
            ModelState.AddModelError("Username", "Username already taken.");
            return View(vm);
        }

        var user = new User
        {
            Username = vm.Username,
            Email = vm.Email,
            PasswordHash = _passwords.HashPassword(vm.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        HttpContext.Session.SetInt32("userId", user.Id);
        HttpContext.Session.SetString("username", user.Username);
        TempData["Success"] = "Welcome to The Rewind! Your account has been created.";
        return RedirectToAction("Index", "Movies");
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        // If already logged in, go to All Movies
        if (HttpContext.Session.GetInt32("userId") != null)
        {
            return RedirectToAction("Index", "Movies");
        }
        return View();
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        // Beginner note: Normalize input before validation (trim fields)
        vm.Username = (vm.Username ?? string.Empty).Trim();
        vm.Password = (vm.Password ?? string.Empty).Trim();

        if (!ModelState.IsValid)
            return View(vm);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == vm.Username);
        if (user is null || !_passwords.VerifyPassword(vm.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(vm);
        }

        HttpContext.Session.SetInt32("userId", user.Id);
        HttpContext.Session.SetString("username", user.Username);
        TempData["Success"] = $"Welcome back, {user.Username}!";
        return RedirectToAction("Index", "Movies");
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        TempData["Success"] = "You have been logged out successfully.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        var userId = HttpContext.Session.GetInt32("userId");
        if (userId == null)
        {
            TempData["Error"] = "Please sign in to continue.";
            return RedirectToAction("Login");
        }

        var user = await _context
            .Users.Include(u => u.Movies)
            .Include(u => u.Ratings)
            .FirstOrDefaultAsync(u => u.Id == userId.Value);
        if (user is null)
            return NotFound();

        ViewBag.MoviesAdded = user.Movies?.Count ?? 0;
        ViewBag.RatingsAdded = user.Ratings?.Count ?? 0;
        ViewBag.RecentMovies = user.Movies?.OrderByDescending(m => m.CreatedAt).Take(5).ToList();
        return View(user);
    }
}
