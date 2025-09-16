using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheVinylCountdown.Models;
using TheVinylCountdown.Services;
using TheVinylCountdown.ViewModels;

namespace TheVinylCountdown.Controllers;

[Route("account")]
public class AccountController : Controller
{
    private readonly ApplicationContext _context;
    private readonly IPasswordService _passwords;
    private const string SessionUserId = "userId";
    private const string SessionUsername = "username";

    public AccountController(ApplicationContext context, IPasswordService passwords)
    {
        _context = context;
        _passwords = passwords;
    }

    [HttpGet("/register")]
    public IActionResult RegisterForm()
    {
        return View();
    }

    [HttpPost("/register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessRegister(RegisterFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("RegisterForm", vm);

        // Check if email already exists
        if (await _context.Users.AnyAsync(u => u.Email == vm.Email))
        {
            ModelState.AddModelError("Email", "Email already registered");
            return View("RegisterForm", vm);
        }

        // Create new user
        var user = new User
        {
            Email = vm.Email,
            Username = vm.Username,
            PasswordHash = _passwords.HashPassword(vm.Password),
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Set session
        HttpContext.Session.SetInt32(SessionUserId, user.Id);
        HttpContext.Session.SetString(SessionUsername, user.Username);

        TempData["Success"] = $"Welcome to The Vinyl Countdown, {user.Username}!";
        return RedirectToAction("Index", "Albums");
    }

    [HttpGet("/login")]
    public IActionResult LoginForm()
    {
        return View();
    }

    [HttpPost("/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessLogin(LoginFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("LoginForm", vm);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == vm.Email);

        if (user == null || !_passwords.VerifyPassword(vm.Password, user.PasswordHash))
        {
            ModelState.AddModelError("", "Invalid email or password");
            return View("LoginForm", vm);
        }

        // Set session
        HttpContext.Session.SetInt32(SessionUserId, user.Id);
        HttpContext.Session.SetString(SessionUsername, user.Username);

        TempData["Success"] = $"Welcome back, {user.Username}!";
        return RedirectToAction("Index", "Albums");
    }

    [HttpGet("/logout")]
    public IActionResult ConfirmLogout()
    {
        var userId = HttpContext.Session.GetInt32(SessionUserId);

        if (userId == null)
        {
            return RedirectToAction("LoginForm");
        }

        return View();
    }

    [HttpPost("/logout")]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        TempData["Success"] = "You've been logged out successfully";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("/protected")]
    public IActionResult ProtectedPage()
    {
        var userId = HttpContext.Session.GetInt32(SessionUserId);
        if (userId == null)
        {
            return Unauthorized();
        }

        var username = HttpContext.Session.GetString(SessionUsername);
        return View("ProtectedPage", username);
    }
}
