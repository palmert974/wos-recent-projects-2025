using System.Linq;
using AuthForge.Models;
using AuthForge.Services;
using AuthForge.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthForge.Controllers;

// Handles registration, login, logout, and the protected page
[Route("account")]
public class AccountController : Controller
{
    private readonly ApplicationContext _context;
    private readonly IPasswordService _passwords;

    // Single source of truth for our session key (avoid magic strings)
    private const string SessionUserId = "userId";

    public AccountController(ApplicationContext context, IPasswordService passwords)
    {
        _context = context;
        _passwords = passwords;
    }

    // ===== Register (GET) =====
    // Show the blank form
    [HttpGet("register")]
    public IActionResult RegisterForm()
    {
        return View(new RegisterFormViewModel());
    }

    // ===== Register (POST) =====
    // Normalize input, validate, hash password, save user, set session
    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public IActionResult ProcessRegisterForm(RegisterFormViewModel vm)
    {
        // Normalize inputs (trim, lowercase for email/username)
        vm.Username = (vm.Username ?? "").Trim().ToLowerInvariant();
        vm.Email = (vm.Email ?? "").Trim().ToLowerInvariant();
        vm.Password = (vm.Password ?? "").Trim();
        vm.ConfirmPassword = (vm.ConfirmPassword ?? "").Trim();

        // Server-side validation check
        if (!ModelState.IsValid)
        {
            return View(nameof(RegisterForm), vm);
        }

        // Unique email constraint
        bool emailExists = _context.Users.Any(u => u.Email == vm.Email);
        if (emailExists)
        {
            ModelState.AddModelError("Email", "That email is in use. Please login.");
            return View(nameof(RegisterForm), vm);
        }

        // Create and persist user
        var hashed = _passwords.Hash(vm.Password);
        var newUser = new User
        {
            Username = vm.Username,
            Email = vm.Email,
            PasswordHash = hashed,
        };
        _context.Users.Add(newUser);
        _context.SaveChanges();

        // Log in the new user by storing their Id in session
        HttpContext.Session.SetInt32(SessionUserId, newUser.Id);
        return RedirectToAction("Index", "Home");
    }

    // ===== Login (GET) =====
    // message is used for logout-successful banner; error is used for auth errors
    [HttpGet("login")]
    public IActionResult LoginForm(string? error, string? message)
    {
        var vm = new LoginFormViewModel { Error = error, Message = message };
        return View(vm);
    }

    // ===== Login (POST) - simple variant used earlier in the assignment =====
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public IActionResult ProcessLoginForm(LoginFormViewModel vm)
    {
        vm.Email = (vm.Email ?? "").Trim().ToLowerInvariant();
        vm.Password = (vm.Password ?? "").Trim();

        if (!ModelState.IsValid)
        {
            // Re-render login form on validation errors
            return View("LoginForm", vm);
        }

        var user = _context.Users.SingleOrDefault(u => u.Email == vm.Email);
        if (user is null || !_passwords.Verify(vm.Password, user.PasswordHash))
        {
            // Redirect with a short error code in the query string (teacher style)
            return RedirectToAction("LoginForm", new { error = "invalid-credentials" });
        }

        HttpContext.Session.SetInt32(SessionUserId, user.Id);
        return RedirectToAction("Index", "Home");
    }

    // ===== Login (POST) - teacher style: /account/login/process =====
    [HttpPost("login/process")]
    [ValidateAntiForgeryToken]
    public IActionResult ProcessLogin(LoginFormViewModel vm)
    {
        vm.Email = (vm.Email ?? "").Trim().ToLowerInvariant();
        vm.Password = (vm.Password ?? "").Trim();

        if (!ModelState.IsValid)
        {
            return View(nameof(LoginForm), vm);
        }

        if (!_context.Users.Any(u => u.Email == vm.Email))
        {
            ModelState.AddModelError("", "Invalid user credentials.");
            return View(nameof(LoginForm), vm);
        }

        var maybeUser = _context.Users.FirstOrDefault(u => u.Email == vm.Email);
        if (maybeUser is null || !_passwords.Verify(vm.Password, maybeUser.PasswordHash))
        {
            ModelState.AddModelError("", "Invalid user credentials.");
            return View(nameof(LoginForm), vm);
        }

        HttpContext.Session.SetInt32(SessionUserId, maybeUser.Id);
        // Assignment says redirect Home after login
        return RedirectToAction("Index", "Home");
    }

    // ===== Logout (GET) - two-step confirmation =====
    [HttpGet("logout")]
    public IActionResult ConfirmLogout()
    {
        var userId = HttpContext.Session.GetInt32(SessionUserId);
        if (userId is null)
        {
            // If not logged in, bounce back to login with a helpful alert
            return RedirectToAction("LoginForm", new { error = "not-authenticated" });
        }
        return View();
    }

    // ===== Logout (POST) - teacher style: /account/logout/process =====
    [HttpPost("logout/process")]
    [ValidateAntiForgeryToken]
    public IActionResult LogoutProcess()
    {
        HttpContext.Session.Clear();
        // Redirect back to login with a success banner
        return RedirectToAction(nameof(LoginForm), new { message = "logout-successful" });
    }

    // ===== Logout (POST) - assignment compatible (single step) =====
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    // ===== Protected (GET) =====
    // Guarded page: require session; load user; show a friendly greeting
    [HttpGet("protected")]
    public IActionResult ProtectedPage()
    {
        if (HttpContext.Session.GetInt32(SessionUserId) is null)
        {
            return RedirectToAction("LoginForm", new { error = "not-authenticated" });
        }

        int userId = HttpContext.Session.GetInt32(SessionUserId)!.Value;
        var user = _context.Users.SingleOrDefault(u => u.Id == userId);
        var email = user?.Email ?? "(unknown)";
        return View("ProtectedPage", email);
    }
}
