# AuthApp Authentication (ASP.NET Core MVC): Step-by-Step Guide

A complete, DIY session-based authentication flow for ASP.NET Core MVC using Entity Framework Core (MySQL via Pomelo) and BCrypt for password hashing. This guide is intentionally explicit and numbered so you can follow each step.

Contents
- Step 1: Create the MVC project
- Step 2: Install EF Core + MySQL provider packages
- Step 3: Create the User model
- Step 4: Create the ApplicationContext (DbContext)
- Step 5: Configure appsettings.json and Program.cs (Db + Session)
- Step 6: Add and apply EF Core migrations
- Step 7: Create the RegisterFormViewModel
- Step 8: Create the AccountController (+ GET /account/register)
- Step 9: Update _ViewImports for ViewModels
- Step 10: Create the register view
- Step 11: Add BCrypt.Net-Next
- Step 12: Create Password Service (interface + implementation)
- Step 13: Register the Password Service (DI)
- Step 14: Implement POST /account/register
- Step 15: Create the LoginFormViewModel
- Step 16: Implement GET /account/login
- Step 17: Create the login view
- Step 18: Implement POST /account/login
- Step 19: Implement POST /account/logout
- Step 20: Add a dynamic navbar partial + integrate in _Layout
- Step 21: Protect a route + protected view
- Step 22: Run and test the flow
- Step 23: Next steps: ASP.NET Core Identity
- Troubleshooting

Prerequisites
- .NET SDK installed (8.x preferred)
- MySQL server running locally
- VS Code (optional but recommended)
- Terminal (zsh on macOS)

Note on security and secrets:
- Avoid storing DB passwords in plain text. Prefer environment variables or dotnet user-secrets in development.

Quick concepts from the lecture (Impromptu Zoom):
- Auth vs Authz: Authentication verifies identity; Authorization decides what an authenticated user can do. “Auth” can refer to both in industry shorthand.
- Big picture: Users can register and log in; the server keeps only userId in session to remember login state; a protected page checks session; logout clears session.
- Vocabulary: Session (server-side state), Session key (string key like "userId"), Password hashing (store only a one-way hash, never plaintext).
- BCrypt details: The salt is embedded in the hash; common BCrypt hashes are ~60 chars.

---

Step 1: Create the MVC project
1. Open a terminal and navigate to your projects directory.
2. Create a new MVC project named AuthApp:

```bash
 dotnet new mvc -n AuthApp
```

3. Enter the project directory and open it in VS Code:

```bash
 cd AuthApp
 code .
```

---

Step 2: Install EF Core + MySQL provider packages
1. Install EF Core Design package (required for migrations):

```bash
 dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.17
```

2. Install Pomelo MySQL provider:

```bash
 dotnet add package Pomelo.EntityFrameworkCore.MySql --version 8.0.3
```

3. (If needed) Install the EF Core CLI tool globally to run migrations:

```bash
 dotnet tool install --global dotnet-ef
# If already installed:
# dotnet tool update --global dotnet-ef
```

---

Step 3: Create the User model
1. Create Models/User.cs:

```csharp
using System.ComponentModel.DataAnnotations;

namespace AuthApp.Models;

public class User
{
    [Key]
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

---

Step 4: Create the ApplicationContext (DbContext)
1. Create Models/ApplicationContext.cs:

```csharp
using Microsoft.EntityFrameworkCore;

namespace AuthApp.Models;

public class ApplicationContext : DbContext
{
    public DbSet<User> Users { get; set; }

    public ApplicationContext(DbContextOptions options)
        : base(options) { }
}
```

---

Step 5: Configure appsettings.json and Program.cs (Db + Session)
1. Open appsettings.json and add the connection string (update password!):

```json
{
  "ConnectionStrings": {
    "MySqlConnection": "Server=localhost;port=3306;userid=root;password=YOUR_PASSWORD;database=auth_db;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

Tip (safer): use dotnet user-secrets in development instead of checking passwords into source control:

```bash
 dotnet user-secrets init
 dotnet user-secrets set "ConnectionStrings:MySqlConnection" "Server=localhost;port=3306;userid=root;password={{MYSQL_PASSWORD}};database=auth_db;"
```

2. Update Program.cs to wire up MVC, Session, and DbContext:

```csharp
using Microsoft.EntityFrameworkCore;
using AuthApp.Models;
using AuthApp.Services; // used for password hashing service

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("MySqlConnection");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSession();

// Database (MySQL via Pomelo)
builder.Services.AddDbContext<ApplicationContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseHttpsRedirection();
app.UseSession();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

Note: AddDistributedMemoryCache is required for in-memory session storage.

---

Step 6: Add and apply EF Core migrations
1. Create the initial migration:

```bash
 dotnet ef migrations add FirstMigration
```

2. Apply migrations to create the database and Users table:

```bash
 dotnet ef database update
```

---

Step 7: Create the RegisterFormViewModel
1. Create a ViewModels folder.
2. Create ViewModels/RegisterFormViewModel.cs:

```csharp
using System.ComponentModel.DataAnnotations;

namespace AuthApp.ViewModels;

public class RegisterFormViewModel
{
    [Required(ErrorMessage = "Please enter your username.")]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters long.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email.")]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Required(ErrorMessage = "Please enter your password.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

---

Step 8: Create the AccountController (+ GET /account/register)
1. Create Controllers/AccountController.cs:

```csharp
using System.Linq;
using AuthApp.Models;
using AuthApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AuthApp.Controllers;

[Route("account/")]
public class AccountController : Controller
{
    private readonly ApplicationContext _context;
    private const string SessionUserId = "userId"; // shared key for session

    public AccountController(ApplicationContext context)
    {
        _context = context;
    }

    // ===== Register (GET) =====
    [HttpGet("register")]
    public IActionResult RegisterForm()
    {
        return View(new RegisterFormViewModel());
    }
}
```

---

Step 9: Update _ViewImports for ViewModels
1. Open Views/_ViewImports.cshtml and ensure it contains:

```cshtml
@using AuthApp
@using AuthApp.Models
@using AuthApp.ViewModels
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

---

Step 10: Create the register view
1. Create Views/Account/RegisterForm.cshtml:

```cshtml
@model RegistrationFormViewModel

@{
    ViewData["Title"] = "Register";
}

<h1 class="display-2">Register</h1>
<div asp-validation-summary="All" class="text-danger mb-2"></div>
<div class="card shadow">
    <div class="card-body">
<form asp-controller="Account" asp-action="ProcessRegister" method="post">
            <div class="mb-3">
                <label asp-for="Username" class="form-label">Username:</label>
                <input asp-for="Username" class="form-control" />
                <span class="form-text text-danger" asp-validation-for="Username"></span>
            </div>
            <div class="mb-3">
                <label asp-for="Email" class="form-label">Email:</label>
                <input asp-for="Email" class="form-control" />
                <span class="form-text text-danger" asp-validation-for="Email"></span>
            </div>
            <div class="mb-3">
                <label asp-for="Password" class="form-label">Password:</label>
                <input asp-for="Password" class="form-control" />
                <span class="form-text text-danger" asp-validation-for="Password"></span>
            </div>
            <div class="mb-3">
                <label asp-for="ConfirmPassword" class="form-label">Confirm Password:</label>
                <input asp-for="ConfirmPassword" class="form-control" />
                <span class="form-text text-danger" asp-validation-for="ConfirmPassword"></span>
            </div>
            <div class="text-end">
                <button type="submit" class="btn btn-primary">Register</button>
            </div>
        </form>
    </div>
    <div class="card-footer text-center">
        <p class="mb-0 text-muted">Already registered? <a href="/account/login">Log in here.</a></p>
    </div>
</div>
```

Note: The form tag helper will emit an antiforgery token automatically.

---

Step 11: Add BCrypt.Net-Next
1. Install the BCrypt package:

```bash
 dotnet add package BCrypt.Net-Next
```

---

Step 12: Create Password Service (interface + implementation)
1. Create Services/IPasswordService.cs:

```csharp
namespace AuthApp.Services;

public interface IPasswordService
{
    string Hash(string plainText);
    bool Verify(string plainText, string hash);
}
```

2. Create Services/BcryptPasswordService.cs:

```csharp
namespace AuthApp.Services;

public class BcryptPasswordService : IPasswordService
{
    public string Hash(string plainText)
    {
        return BCrypt.Net.BCrypt.HashPassword(plainText);
    }

    public bool Verify(string plainText, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(plainText, hash);
    }
}
```

---

Step 13: Register the Password Service (DI)
1. Update Program.cs to register the service:

```csharp
// ... other usings ...
using AuthApp.Services; // ensure this is present

// after builder initialization
builder.Services.AddScoped<IPasswordService, BcryptPasswordService>();
```

Full Program.cs example already shown in Step 5 will now include this AddScoped line.

---

Step 14: Implement POST /account/register
1. Update Controllers/AccountController.cs, adding the password service via DI and the POST action:

```csharp
using System.Linq;
using AuthApp.Models;
using AuthApp.Services; // add
using AuthApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AuthApp.Controllers;

[Route("account")]
public class AccountController : Controller
{
    private readonly ApplicationContext _context;
    private readonly IPasswordService _passwords; // add
    private const string SessionUserId = "userId";

    public AccountController(ApplicationContext context, IPasswordService passwords) // add parameter
    {
        _context = context;
        _passwords = passwords;
    }

    [HttpGet("register")]
    public IActionResult RegisterForm()
    {
        return View(new RegisterFormViewModel());
    }

[HttpPost("register/process")]
[ValidateAntiForgeryToken]
public IActionResult ProcessRegister(RegistrationFormViewModel vm)
    {
// 1) Normalize
        vm.Username = (vm.Username ?? "").Trim().ToLowerInvariant();
        vm.Email = (vm.Email ?? "").Trim().ToLowerInvariant();
        vm.Password = (vm.Password ?? "").Trim();
        vm.ConfirmPassword = (vm.ConfirmPassword ?? "").Trim();

        // 2) Validate annotations
        if (!ModelState.IsValid)
        {
            return View("RegisterForm", vm);
        }

        // 3) Email uniqueness
        bool emailExists = _context.Users.Any(u => u.Email == vm.Email);
        if (emailExists)
        {
ModelState.AddModelError("Email", "That email is in use. Please login.");
            return View("RegisterForm", vm);
        }

        // 4) Hash password
        var hashed = _passwords.Hash(vm.Password);

// 5) Create user
        var newUser = new User { Username = vm.Username, Email = vm.Email, PasswordHash = hashed };
        _context.Users.Add(newUser);
        _context.SaveChanges();

        // 6) Login via session
        HttpContext.Session.SetInt32(SessionUserId, newUser.Id);

// 7) PRG
// After successful registration, assignment requires redirecting to Home:
return RedirectToAction("Index", "Home");
// Teacher demo sometimes redirects to a protected page to show session-based protection:
// return RedirectToAction(nameof(ProtectedPage));
    }
}
```

---

Step 15: Create the LoginFormViewModel
1. Create ViewModels/LoginFormViewModel.cs:

```csharp
using System.ComponentModel.DataAnnotations;

namespace AuthApp.ViewModels;

public class LoginFormViewModel
{
    [Required(ErrorMessage = "Please enter email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Required(ErrorMessage = "Please enter password.")]
    public string Password { get; set; } = string.Empty;

    public string? Error { get; set; }
}
```

---

Step 16: Implement GET /account/login
1. In Controllers/AccountController.cs add the action:

```csharp
[HttpGet("login")]
public IActionResult LoginForm(string? error)
{
    var vm = new LoginFormViewModel { Error = error };
    return View(vm);
}
```

---

Step 17: Create the login view
1. Create Views/Account/LoginForm.cshtml:

```cshtml
@model LoginFormViewModel
@{
    ViewData["Title"] = "Login";
    var error = Model.Error;
}

<h1 class="display-2">Login</h1>

@if (error is not null)
{
    <div class="alert alert-warning alert-dismissible fade show" role="alert">
        @if (error == "invalid-credentials")
        {
            <strong>Invalid credentials. Please try again.</strong>
        }
        else if (error == "not-authenticated")
        {
            <strong>You must be logged in to view that page.</strong>
        }
        else
        {
            <strong>Oops! Something went wrong.</strong>
        }
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}

<div class="card shadow">
    <div class="card-body">
        <div asp-validation-summary="ModelOnly" class="text-danger"></div>
        <form asp-controller="Account" asp-action="ProcessLoginForm" method="post">
            <div class="mb-3">
                <label asp-for="Email" class="form-label">Email:</label>
                <input asp-for="Email" class="form-control" />
                <span class="form-text text-danger" asp-validation-for="Email"></span>
            </div>
            <div class="mb-3">
                <label asp-for="Password" class="form-label">Password:</label>
                <input asp-for="Password" class="form-control" />
                <span class="form-text text-danger" asp-validation-for="Password"></span>
            </div>
            <div class="text-end">
                <button type="submit" class="btn btn-primary">Login</button>
            </div>
        </form>
    </div>
    <div class="card-footer text-center">
        <p class="mb-0 text-muted">Need an account? <a href="/account/register">Register here.</a></p>
    </div>
</div>
```

---

Step 18: Implement POST /account/login
1. In Controllers/AccountController.cs, add the POST action:

```csharp
[HttpPost("login")]
[ValidateAntiForgeryToken]
public IActionResult ProcessLoginForm(LoginFormViewModel vm)
{
    // 1) Normalize
    vm.Email = (vm.Email ?? "").Trim().ToLowerInvariant();
    vm.Password = (vm.Password ?? "").Trim();

    // 2) Validate
    if (!ModelState.IsValid)
    {
        return View("LoginForm", vm);
    }

    // 3) Find user by email
    var user = _context.Users.SingleOrDefault(u => u.Email == vm.Email);

    // 4) Verify user exists and password matches
    if (user is null || !_passwords.Verify(vm.Password, user.PasswordHash))
    {
        // PRG with error message
        return RedirectToAction("LoginForm", new { error = "invalid-credentials" });
    }

    // 5) Store user id in session
    HttpContext.Session.SetInt32(SessionUserId, user.Id);

    // 6) Redirect to home
    return RedirectToAction("Index", "Home");
}
```

---

Step 19: Implement POST /account/logout
1. In Controllers/AccountController.cs add the action:

```csharp
[HttpPost("logout")]
[ValidateAntiForgeryToken]
public IActionResult Logout()
{
    HttpContext.Session.Clear();
    return RedirectToAction("Index", "Home");
}
```

---

Step 20: Add a dynamic navbar partial + integrate in _Layout
1. Create Views/Shared/_Navbar.cshtml:

```cshtml
@using Microsoft.AspNetCore.Http

<nav class="navbar navbar-expand-lg bg-body-tertiary">
    <div class="container">
        <a class="navbar-brand" href="/">AuthApp</a>
        <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarNav"
            aria-controls="navbarNav" aria-expanded="false" aria-label="Toggle navigation">
            <span class="navbar-toggler-icon"></span>
        </button>
        <div class="collapse navbar-collapse" id="navbarNav">
            <ul class="navbar-nav">
                <li class="nav-item">
                    <a class="nav-link" href="/">Home</a>
                </li>
            </ul>
        </div>
        @if (Context.Session.GetInt32("userId") is not null)
        {
            <form asp-controller="Account" asp-action="Logout" method="post" class="d-inline">
                <button type="submit" class="btn btn-sm btn-outline-secondary">Logout</button>
            </form>
        }
        else
        {
            <a class="btn btn-sm btn-primary me-2" href="/account/register">Register</a>
            <a class="btn btn-sm btn-primary" href="/account/login">Login</a>
        }
    </div>
</nav>
```

2. Open Views/Shared/_Layout.cshtml and render the partial inside the header (replace any existing nav markup):

```cshtml
<header>
    @await Html.PartialAsync("_Navbar")
</header>
```

---

Step 21: Protect a route + protected view
1. In Controllers/AccountController.cs add a protected route:

```csharp
[HttpGet("protected")]
public IActionResult Protected()
{
    if (HttpContext.Session.GetInt32(SessionUserId) is null)
    {
        return RedirectToAction("LoginForm", new { error = "not-authenticated" });
    }

    int userId = HttpContext.Session.GetInt32(SessionUserId)!.Value;
    var user = _context.Users.SingleOrDefault(u => u.Id == userId);

    ViewBag.UserEmail = user?.Email ?? "(unknown)";
    return View();
}
```

2. Create Views/Account/Protected.cshtml:

```cshtml
@{
    ViewData["Title"] = "Protected Page";
}

<h1>Protected Page</h1>
<p>Congratulations, you are logged in!</p>
<p>Your email is: <strong>@ViewBag.UserEmail</strong></p>
```

---

Step 22: Run and test the flow
1. Run the app:

```bash
 dotnet watch run
```

2. Register a user: visit /account/register and submit a valid email/password (>= 8 chars).
3. Confirm you’re redirected to Home and the navbar shows “Logout”.
4. Logout via the navbar button.
5. Visit /account/protected while logged out — you should be redirected to /account/login?error=not-authenticated and see an alert.
6. Login at /account/login with the previously registered credentials — you should be redirected to Home and logged in.

---

Step 23: Next steps: ASP.NET Core Identity
This tutorial implements a DIY session-based auth to teach fundamentals. For production-grade features (2FA, password reset, email confirmation, lockouts, external providers), adopt ASP.NET Core Identity, which integrates cleanly with EF Core and provides hardened defaults.

---

Troubleshooting
- MySQL connection fails
  - Ensure MySQL is running locally and credentials are correct.
  - Confirm your connection string is correct; for example: Server=localhost;port=3306;userid=root;password=...;database=auth_db;
  - If using user-secrets, verify: dotnet user-secrets list

- IDistributedCache missing
  - Ensure Program.cs includes builder.Services.AddDistributedMemoryCache(); before AddSession().

- Antiforgery token errors
  - Using the form tag helper (form asp-controller asp-action) automatically emits the token. Ensure [ValidateAntiForgeryToken] is on POST actions, and that your form uses the tag helper.

- Validation messages not showing
  - Confirm your view includes asp-validation-summary and asp-validation-for spans, and that the ViewModel has DataAnnotations.

- Migrations not generating
  - Ensure the project builds (dotnet build) and that Microsoft.EntityFrameworkCore.Design is installed.
  - Confirm dotnet-ef is installed globally.

- Duplicate email allowed
  - Double-check the uniqueness check in ProcessRegisterForm and that you normalized (trim/lowercase) the email before comparison. Consider adding a unique index on Email in the Users table for defense-in-depth.

- Session not persisting
  - Confirm app.UseSession() is in the pipeline and cookies are enabled in your browser.

Notes
- Authentication vs Authorization: Authentication proves identity; Authorization decides access. This guide focuses on authentication with sessions.
- Session key: This guide consistently uses "userId" as the session key. Keep it consistent across views and controllers.
- Passwords: Never store plain-text passwords. Always hash (BCrypt) and verify using a well-tested library.

