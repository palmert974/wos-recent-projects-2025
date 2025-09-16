# The Rewind - Build Guide

## Build Order

### Part A: Project Setup
1. Create project and install packages
2. Configure Bootstrap with LibMan
3. Test project runs

### Part B: Database Setup  
4. Create Models (User, Movie, Rating)
5. Create ApplicationContext
6. Configure connection string
7. Configure Program.cs
8. Run migrations

### Part C: Features (Controllers → Views)
9. Authentication (AccountController → Account views)
10. Movies (MoviesController → Movie views)
11. Error handling
12. Testing

---


## Part A: Project Setup

### Prerequisites
- .NET SDK 8.0
- MySQL (password: `rootroot`)
- VS Code

### Step 1: Create Project
```bash
# Create and enter project
cd ~/2025-wos/week-11-full-stack-mvc
dotnet new mvc -n TheRewind
cd TheRewind

# Open in VS Code
code .
```


### Step 2: Install Packages
```bash
dotnet add package Pomelo.EntityFrameworkCore.MySql
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package BCrypt.Net-Next
dotnet tool install --global dotnet-ef
```

### Step 3: Test Project Runs
```bash
dotnet build
dotnet run
```

### Step 3.5: Styling — Bootstrap Dark Theme (Teacher Pattern)
- In Views/Shared/_Layout.cshtml set: <html lang="en" data-bs-theme="dark">
- Use ONLY local minified assets under wwwroot/lib (copied by LibMan or from teacher project):
  - ~/lib/bootstrap/dist/css/bootstrap.min.css
  - ~/lib/bootstrap/dist/js/bootstrap.bundle.min.js
  - ~/lib/jquery/dist/jquery.min.js
- Keep wwwroot/css/site.css minimal; do not hardcode dark backgrounds or text colors. Let Bootstrap handle it.
- In views, use standard Bootstrap classes (card, table table-hover, btn btn-primary, form-control).
- Do NOT add bg-dark/border-dark to cards or tables.

Verification checklist
- Home, Movies, Details, Account pages all render with dark backgrounds and correct button styles
- Text inside selects and tables is clearly visible
- No 404 errors for css/js in the browser DevTools
- A hard refresh (Cmd+Shift+R) shows updated styling if caching interferes

---

Appendix A — Requirements Checklist (Merged)
- Authentication
  - BCrypt password hashing; never store plaintext
  - Session configured; protected routes redirect to login
  - Logout confirmation
  - Input normalization on auth POSTs (trim + lowercase email)
- CRUD and Authorization
  - Create/Read/Update/Delete Movies
  - Owner-only edit/delete with Forbid() on violations
  - Delete uses confirmation page
- Ratings
  - One rating per user per movie (server check + DB unique index)
  - Average rating displayed to 1 decimal; rating form hidden after rating
- Validation
  - Data annotations on User and Movie (min lengths, required, email)
  - Validation summaries and field errors on all forms
- Error handling
  - Custom 404/500/general pages
  - Try/catch around DB saves and NotFound checks
  - Dev-only error demo routes hidden in Production
- Security
  - [ValidateAntiForgeryToken] on all POST actions
  - Async EF Core calls; AsNoTracking for read-only queries
- UI/UX
  - Dark theme via data-bs-theme="dark" on <html>, local Bootstrap, minimal site.css
  - Conditional navbar, TempData alerts (via _Flash partial), responsive layout
  - Details shows Added/Updated timestamps; Profile shows recent movies
- Logging
  - ILogger used in MoviesController for key actions

Appendix B — Assessment Readiness Summary (Merged)
- All required features implemented and verified (auth, CRUD, ratings, profile, validation, errors)
- Best practices followed (async EF, CSRF, AsNoTracking, ViewModels, auth checks)
- Testing checklist
  - Auth: register/login/logout with validation and session
  - Movies: cannot create/edit/delete without login; owner checks enforce 403
  - Ratings: cannot rate twice; averages correct; form hidden after rating
  - Errors: 404 for invalid IDs; CSRF tokens present on POSTs
- Run commands
  - dotnet build
  - dotnet watch
  - Browse http://localhost:5187

Appendix C — Dark Theme Quick Fixes (Merged)
- Ensure <html data-bs-theme="dark"> (not on body)
- Use only local assets under wwwroot/lib; no CDN duplicates
- Keep site.css minimal; remove forced bg-dark/text-light overrides
- Use standard Bootstrap classes (card, table table-hover, btn btn-primary)
- Hard refresh the browser (Cmd+Shift+R) after CSS changes

---

## Quick Migration Update (timestamps)

We added timestamps after the initial migration. Run these once to update your database:

```bash path=null start=null
# add columns: Users.CreatedAt/UpdatedAt, Ratings.UpdatedAt
dotnet ef migrations add AddTimestampsToUserAndRating
# apply schema changes
dotnet ef database update
```

---

## Part B: Database Setup

### Step 4: Create Models

Flash messages partial (reusable)

Create Views/Shared/_Flash.cshtml and include it in _Layout below the navbar.

```cshtml path=/Users/tamarapalmer/2025-wos/week-11-full-stack-mvc/TheRewind/Views/Shared/_Flash.cshtml start=1
@*
  Reusable flash messages partial. Shows TempData["Success"], TempData["Error"], or TempData["Info"].
*@
@if (TempData["Success"] is string ok)
{
  <div class="alert alert-success alert-dismissible fade show mt-3" role="alert">
    @ok
    <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
  </div>
}
@if (TempData["Error"] is string err)
{
  <div class="alert alert-danger alert-dismissible fade show mt-3" role="alert">
    @err
    <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
  </div>
}
@if (TempData["Info"] is string info)
{
  <div class="alert alert-info alert-dismissible fade show mt-3" role="alert">
    @info
    <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
  </div>
}
```

In _Layout.cshtml, right under the header/container, add:

```cshtml path=/Users/tamarapalmer/2025-wos/week-11-full-stack-mvc/TheRewind/Views/Shared/_Layout.cshtml start=15
<div class="container">
  @await Html.PartialAsync("_Flash")
  <main role="main" class="pb-3">
    @RenderBody()
  </main>
</div>
```

Create these files in the `Models` folder:
- `User.cs`
- `Movie.cs`  
- `Rating.cs`
- `ApplicationContext.cs`

1) Models/User.cs
```csharp path=null start=null
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TheRewind.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, StringLength(32, MinimumLength = 2)]
        public string Username { get; set; } = string.Empty;

        // Store hashed password only (use BCrypt)
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // Navigation: One User -> Many Movies
        public List<Movie> Movies { get; set; } = new();

        // Navigation: One User -> Many Ratings
        public List<Rating> Ratings { get; set; } = new();
    }
}
```

2) Models/Movie.cs
```csharp path=null start=null
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TheRewind.Models
{
    public class Movie
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(1888, 2100)]
        public int? ReleaseYear { get; set; }

        // Foreign Key to the User who added it
        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        // Ratings for this movie
        public List<Rating> Ratings { get; set; } = new();

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
```

3) Models/Rating.cs
```csharp path=null start=null
using System;
using System.ComponentModel.DataAnnotations;

namespace TheRewind.Models
{
    public class Rating
    {
        public int Id { get; set; }

        // 1-5 inclusive
        [Range(1, 5)]
        public int Value { get; set; }

        // FK: which user rated
        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        // FK: which movie was rated
        [Required]
        public int MovieId { get; set; }
        public Movie? Movie { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
```

4) Models/ApplicationContext.cs
```csharp path=null start=null
using Microsoft.EntityFrameworkCore;

namespace TheRewind.Models
{
    public class ApplicationContext : DbContext
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Movie> Movies => Set<Movie>();
        public DbSet<Rating> Ratings => Set<Rating>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // One User -> Many Movies (cascade delete movies when user is deleted)
            modelBuilder.Entity<Movie>()
                .HasOne(m => m.User)
                .WithMany(u => u.Movies)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // One User -> Many Ratings (cascade delete ratings when user deleted)
            modelBuilder.Entity<Rating>()
                .HasOne(r => r.User)
                .WithMany(u => u.Ratings)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // One Movie -> Many Ratings (cascade delete ratings when movie deleted)
            modelBuilder.Entity<Rating>()
                .HasOne(r => r.Movie)
                .WithMany(m => m.Ratings)
                .HasForeignKey(r => r.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            // Enforce: a user can rate a given movie at most once
            modelBuilder.Entity<Rating>()
                .HasIndex(r => new { r.UserId, r.MovieId })
                .IsUnique();
        }
    }
}
```

If you accidentally created Views early
- In VS Code Explorer, right‑click the Views/Movies or Views/Account folders you created too early → Delete.
- You’ll recreate them in Steps 11–13 at the right time.

Build checkpoint
```bash
# Make sure models compile before moving on
 dotnet build
```
Next → Step 4: Configure appsettings.json (connection string)
If you see errors, double‑check:
- File names match exactly (ApplicationContext.cs, not application.cs)
- Namespaces match (namespace TheRewind.Models)
- All using statements are present (e.g., using Microsoft.EntityFrameworkCore;)

Why this matters
- Models define your database shape. Creating them FIRST makes migrations accurate.
- The unique index on (UserId, MovieId) enforces “one rating per movie per user” at the database level.
- Cascade deletes ensure related data (movies/ratings) are cleaned up when a user/movie is removed.

Checklist before migrations
- All three models exist with required properties and navigation props
- ApplicationContext has DbSets and OnModelCreating configured
- appsettings.json has MySqlConnection set
- Program.cs will be configured next to register ApplicationContext

Snippets to use here (optional, to move faster)
- modelval → Start a model with validation attributes
- dbset → Quickly add DbSet<T> to ApplicationContext
- navprop → Add a foreign key + navigation pair
- navcol → Add a one-to-many collection navigation

Why these snippets: They standardize boilerplate (properties, FKs, navigation) so you can focus on your specific fields and relationships, not syntax.

### 🔧 Step 4: Configure Database Connection
Update `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "MySqlConnection": "Server=localhost;Database=therewinddb;User=root;Password=rootroot;"
  }
}
```

Next → Step 5: Install EF packages

### 📦 Step 5: Install Database Packages
```bash
# Install MySQL and Entity Framework packages (now that models are in place)
 dotnet add package Pomelo.EntityFrameworkCore.MySql
 dotnet add package Microsoft.EntityFrameworkCore.Design
```

Why these packages
- Pomelo.EntityFrameworkCore.MySql: EF Core provider that knows how to talk to MySQL.
- Microsoft.EntityFrameworkCore.Design: lets EF generate migrations from your models.
- We install these after models exist so EF can reflect over them immediately.

Next → Step 6: Wire DbContext in Program.cs

### 🔨 Step 6: Configure Program.cs

What "wire the DB" means
- Tell the app how to construct ApplicationContext using the MySQL connection string, so EF Core can talk to your database.

Prereqs
- Step 3 done: Models + ApplicationContext compile
- Step 4 done: appsettings.json has ConnectionStrings:MySqlConnection
- Step 5 done: Packages installed in THIS project: Pomelo.EntityFrameworkCore.MySql, Microsoft.EntityFrameworkCore.Design

Add to Program.cs
- Add these usings at the top:
```csharp path=null start=null
using Microsoft.EntityFrameworkCore;
using TheRewind.Models; // where ApplicationContext lives
```

Add database context and session configuration (in the builder section):

Merge vs Replace (important)
- Do NOT replace Program.cs wholesale. Merge the additions below into your existing file.
- Why: The template already includes useful defaults; merging avoids breaking environment-specific code.
- Prefer to add exactly these three things:
  1) Usings at the top: Microsoft.EntityFrameworkCore and TheRewind.Models
  2) In builder.Services: AddSession + AddDbContext<ApplicationContext>(UseMySql)
  3) In the app pipeline: UseExceptionHandler/StatusCodePages, UseSession before Authorization, keep the default MapControllerRoute

Use this Program.cs content as a starting point:
```csharp path=null start=null
using Microsoft.EntityFrameworkCore;
using TheRewind.Models;
using TheRewind.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Session (for manual auth)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// EF Core + MySQL
var connectionString = builder.Configuration.GetConnectionString("MySqlConnection");
builder.Services.AddDbContext<ApplicationContext>(opt =>
    opt.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// Password hashing service (registered now, used after you add auth)
// You can add this now OR after Step 9 once Services exist
builder.Services.AddScoped<IPasswordService, BcryptService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error/500");
    app.UseStatusCodePagesWithReExecute("/error/{0}");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

Alternative: Full Program.cs (copy-paste)
If merging is confusing, replace the entire contents of your Program.cs with this file:

```csharp path=null start=null
using Microsoft.EntityFrameworkCore;
using TheRewind.Models;
// using TheRewind.Services; // uncomment after Step 9 when Services exist

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Session (for manual auth)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// EF Core + MySQL
// Make sure appsettings.json has:
// "ConnectionStrings": { "MySqlConnection": "Server=localhost;Database=therewinddb;User=root;Password=rootroot;" }
var connectionString = builder.Configuration.GetConnectionString("MySqlConnection");
builder.Services.AddDbContext<ApplicationContext>(opt =>
    opt.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// Password hashing service (uncomment after Step 9)
// builder.Services.AddScoped<IPasswordService, BcryptService>();

var app = builder.Build();

// Error pipeline for prod-like runs
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error/500");                 // friendly 500
    app.UseStatusCodePagesWithReExecute("/error/{0}");     // friendly 404/401/403
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();       // read/write session in controllers
app.UseAuthorization();

// Keep default conventional route. Attribute routes on controllers also work.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Optional seed hook (only if you created a Seed class)
// using (var scope = app.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
//     // TheRewind.Data.Seed.Run(db);
// }

app.Run();
```

Build checkpoint
```bash
# Verify Program.cs compiles with your models and settings (using ConnectionStrings:MySqlConnection)
 dotnet build
```
If build fails on IPasswordService/BcryptService and you have not created Services yet (Step 9), comment out the AddScoped line temporarily, then build again. After Step 9, uncomment it and build.

Routes (do I need to change them?)
- Keep the default route:
  - MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}")
- Your controllers also use attribute routes for clean URLs:
  - Account: /account/register, /account/login, /account/logout
  - Movies: /movies, /movies/{id}, /movies/create, /movies/{id}/edit, etc.

When would I replace the whole file?
- Only if your Program.cs is already broken or very different. Otherwise, merging the lines above is safer.

Next → Step 7: Run first migration

Exactly what to run after Step 6 (from the project folder)
```bash
# 1) Make sure the project builds
 dotnet build

# 2) Create your first migration (creates Migrations/ folder)
 dotnet ef migrations add InitialCreate

# 3) Apply the migration to create the database/tables
 dotnet ef database update

# 4) Run the app and verify it starts
 dotnet run
```
If you see “Unable to create an object of type 'ApplicationContext'”, fix Program.cs/usings/connection string so the app can construct your DbContext, then re-run the commands above.

What the important lines do (quick)
- AddControllersWithViews(): turns on MVC (controllers + views).
- AddSession(): enables server‑side session so you can keep track of logged‑in users.
- AddDbContext<ApplicationContext>(UseMySql): registers your DbContext and the connection to MySQL.
- UseExceptionHandler + UseStatusCodePagesWithReExecute: sends errors to your ErrorController views (friendly 404/500).
- app.UseStaticFiles(): serves Bootstrap, site.css, and images from wwwroot.
- app.MapControllerRoute(): wires default MVC routing; we also added attribute routes on controllers for clean URLs.

Common errors (and fixes)
- The type or namespace ‘ApplicationContext’ could not be found
  - Add `using TheRewind.Models;` at the top (or fix the namespace in ApplicationContext.cs)
- GetConnectionString returns null
- appsettings.json must contain ConnectionStrings:MySqlConnection with your MySQL string
- MySQL provider not found
  - Ensure Pomelo.EntityFrameworkCore.MySql is installed in THIS project (`dotnet add package ...`)
- Design-time error during migration
  - Program.cs must compile; ApplicationContext must have the exact constructor:
    `public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options) { }`

### Step 7: Run First Migration
```bash
# Creates the database and tables
dotnet ef migrations add InitialCreate
dotnet ef database update
```


---

## CONTROLLER PLAN: What you will build and why

Controllers you will create and how each connects to the next steps:

- AccountController (authentication)
  - Purpose: Register, Login, Logout. Sets Session values (userId, username).
  - Connects to: Services/IPasswordService (hash/verify), Views/Account pages.
  - Why first: You need login state to protect movie create/edit/delete.

- MoviesController (CRUD + ratings entry point)
  - Purpose: List, create, edit, delete, details with ratings summary.
  - Connects to: ApplicationContext (EF Core), Views/Movies, Session (ownership checks).
  - Why now: Core domain feature after auth exists.

- ErrorController (custom error pages)
  - Purpose: Friendly 404/401/403/500 pages.
  - Connects to: Program.cs pipeline (UseExceptionHandler, UseStatusCodePagesWithReExecute).
  - Why: Required for UX and assessment rubric.

Data flow in a request (example)
- User logs in (AccountController) → Session stores userId
- User navigates to Movies/Index (MoviesController) → reads Session for conditional UI
- User opens Details → controller fetches Movie + Ratings (Include) and computes whether to show rating form (has user rated?)
- Errors or missing IDs → ErrorController renders friendly pages

Use these snippets as you build controllers
- actionauth → MVC action with auth check
- asyncaction → Async action that queries a DbSet
- asynccreate / asyncedit / asyncdelete → Full CRUD patterns
- loginaction / registeraction / logoutaction → Complete auth handlers
- trylog → try/catch with ILogger

## PART 3: AUTHENTICATION SETUP

### Build Workflow: Controllers → Views
1. Create controller with actions and routes
2. Build and verify controller compiles and routes work  
3. Create views with strongly-typed models
4. Add validation scripts and test the complete flow

### 📦 Step 8: Install BCrypt Package

What this does (in plain English)
- Adds a library (BCrypt) that securely hashes passwords with salt so we never store plain text.

Why this step
- We never store plain-text passwords. BCrypt provides a strong one-way hash with salt.

Snippets to use
- ctorpass → Controller constructor with IPasswordService
- loginaction / registeraction / logoutaction → Complete typical flows
```bash
# Install password hashing package
dotnet add package BCrypt.Net-Next
```

### 🔐 Step 9: Create Password Service
In VS Code Explorer:
- Right-click on project root → New Folder → name it `Services`
- Right-click on `Services` folder → New File → create:
  - `IPasswordService.cs`
  - `BcryptService.cs`

Paste this code (namespace must be TheRewind.Services):

IPasswordService.cs
```csharp path=null start=null
namespace TheRewind.Services
{
    public interface IPasswordService
    {
        string HashPassword(string rawPassword);
        bool VerifyPassword(string rawPassword, string hash);
    }
}
```

BcryptService.cs
```csharp path=null start=null
using BCrypt.Net;

namespace TheRewind.Services
{
    public class BcryptService : IPasswordService
    {
        public string HashPassword(string rawPassword)
            => BCrypt.Net.BCrypt.HashPassword(rawPassword);

        public bool VerifyPassword(string rawPassword, string hash)
            => BCrypt.Net.BCrypt.Verify(rawPassword, hash);
    }
}
```

Build checkpoint
```bash
# Now uncomment the AddScoped line in Program.cs if you commented it earlier,
# then verify the project builds
 dotnet build
```

Why this step
- Keeps hashing logic separate and testable; controllers depend on an interface, not an implementation.

How it connects
- Program.cs registers IPasswordService in DI.
- AccountController receives IPasswordService via constructor and uses it to hash/verify passwords.

### 📋 Step 10: Create ViewModels
In VS Code Explorer:
- Right-click on project root → New Folder → name it `ViewModels`
- Right-click on `ViewModels` folder → New File → create:
  - `LoginViewModel.cs`
  - `RegisterViewModel.cs`

Paste this code:

LoginViewModel.cs
```csharp path=null start=null
using System.ComponentModel.DataAnnotations;

namespace TheRewind.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
```

RegisterViewModel.cs
```csharp path=null start=null
using System.ComponentModel.DataAnnotations;

namespace TheRewind.ViewModels
{
    public class RegisterViewModel
    {
        [Required, StringLength(32, MinimumLength = 2)]
        public string Username { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Compare("Password", ErrorMessage = "Passwords must match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
```

Why this step
- ViewModels shape exactly what the form needs, separate from database entities, improving security and UX.

How it connects
- AccountController actions accept these ViewModels as parameters.
- Account views (Login.cshtml, Register.cshtml) use these types for strong typing and validation.

Snippets to use
- modelval → Quickly scaffold a ViewModel with validation attributes

### 🎮 Step 11: Create AccountController

Global view setup (do once)
- Open Views/Shared/_ViewImports.cshtml and ensure it has:
```cshtml path=null start=null
@using TheRewind
@using TheRewind.Models
@using TheRewind.ViewModels
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```
- Make sure _ValidationScriptsPartial.cshtml exists in Views/Shared. If not, create it with:
```cshtml path=null start=null
<script src="~/lib/jquery/jquery.min.js"></script>
<script src="~/lib/jquery-validation/dist/jquery.validate.min.js"></script>
<script src="~/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js"></script>
```
(Add @section Scripts { <partial name="_ValidationScriptsPartial" /> } to each form view as shown below.)
In VS Code Explorer:
- Right-click on `Controllers` folder → New File → `AccountController.cs`
- Right-click on `Views` folder → New Folder → name it `Account`
- Right-click on `Views/Account` folder → New File → create:
  - `Login.cshtml`
  - `Register.cshtml`

Paste this controller:
```csharp path=null start=null
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheRewind.Models;
using TheRewind.Services;
using TheRewind.ViewModels;

namespace TheRewind.Controllers
{
    [Route("account")]
    public class AccountController : Controller
    {
        private readonly ApplicationContext _context;
        private readonly IPasswordService _passwords;

        public AccountController(ApplicationContext context, IPasswordService passwords)
        {
            _context = context;
            _passwords = passwords;
        }

[HttpGet("register")]
        public IActionResult Register() => View();

[HttpPost("register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            bool usernameTaken = await _context.Users.AnyAsync(u => u.Username == vm.Username);
            if (usernameTaken)
            {
                ModelState.AddModelError("Username", "Username already taken.");
                return View(vm);
            }

            var user = new User
            {
                Username = vm.Username,
                PasswordHash = _passwords.HashPassword(vm.Password)
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetInt32("userId", user.Id);
            HttpContext.Session.SetString("username", user.Username);
            return RedirectToAction("Index", "Home");
        }

[HttpGet("login")]
        public IActionResult Login() => View();

[HttpPost("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == vm.Username);
            if (user is null || !_passwords.VerifyPassword(vm.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(vm);
            }

            HttpContext.Session.SetInt32("userId", user.Id);
            HttpContext.Session.SetString("username", user.Username);
            return RedirectToAction("Index", "Home");
        }

[HttpPost("logout")]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
```

Login.cshtml (matches "Sign In" wireframe)
```cshtml path=null start=null
@model TheRewind.ViewModels.LoginViewModel
<h2>Login</h2>
<form asp-action="Login" method="post">
    <div class="mb-3">
        <label asp-for="Username" class="form-label"></label>
        <input asp-for="Username" class="form-control" />
        <span asp-validation-for="Username" class="text-danger"></span>
    </div>
    <div class="mb-3">
        <label asp-for="Password" class="form-label"></label>
        <input asp-for="Password" class="form-control" />
        <span asp-validation-for="Password" class="text-danger"></span>
    </div>
    <button type="submit" class="btn btn-primary">Login</button>
</form>

@section Scripts { <partial name="_ValidationScriptsPartial" /> }
```

Register.cshtml (matches "Sign Up" wireframe)
```cshtml path=null start=null
@model TheRewind.ViewModels.RegisterViewModel
<h2>Register</h2>
<form asp-action="Register" method="post">
    <div class="mb-3">
        <label asp-for="Username" class="form-label"></label>
        <input asp-for="Username" class="form-control" />
        <span asp-validation-for="Username" class="text-danger"></span>
    </div>
    <div class="mb-3">
        <label asp-for="Password" class="form-label"></label>
        <input asp-for="Password" class="form-control" />
        <span asp-validation-for="Password" class="text-danger"></span>
    </div>
    <div class="mb-3">
        <label asp-for="ConfirmPassword" class="form-label"></label>
        <input asp-for="ConfirmPassword" class="form-control" />
        <span asp-validation-for="ConfirmPassword" class="text-danger"></span>
    </div>
    <button type="submit" class="btn btn-success">Create Account</button>
</form>

@section Scripts { <partial name="_ValidationScriptsPartial" /> }
```

Build checkpoint
```bash
# After adding AccountController and views, make sure it compiles and runs
 dotnet build
 dotnet run
```
Test login/register flows and confirm session sets username in navbar.

Next → PART 4: Movies Feature

Why this step
- Provides the entry points for authentication and sets Session (userId, username) used throughout the app.

What this does (in plain English)
- Register creates a user with a hashed password, logs them in (Session), and redirects home.
- Login checks credentials, logs in (Session) on success, or returns validation errors.
- Logout clears the Session.
- Uses [ValidateAntiForgeryToken] on POSTs to prevent CSRF.

Routes
- GET /account/register, POST /account/register
- GET /account/login, POST /account/login
- POST /account/logout

Snippets to use
- ctorpass, loginaction, registeraction, logoutaction, sessioncheck

Optional: Profile page (matches wireframe)
Add this action to AccountController and create Views/Account/Profile.cshtml.

AccountController.cs (append)
```csharp path=null start=null
[HttpGet("profile")]
public async Task<IActionResult> Profile()
{
    var userId = HttpContext.Session.GetInt32("userId");
    if (userId == null) return RedirectToAction("Login");

    var user = await _context.Users
        .Include(u => u.Movies)
        .Include(u => u.Ratings)
        .FirstOrDefaultAsync(u => u.Id == userId.Value);
    if (user is null) return NotFound();

    ViewBag.MoviesAdded = user.Movies?.Count ?? 0;
    ViewBag.RatingsAdded = user.Ratings?.Count ?? 0;
    return View(user);
}
```

Views/Account/Profile.cshtml
```cshtml path=null start=null
@model TheRewind.Models.User
<h2>Profile</h2>
<div class="card">
  <div class="card-body">
    <p><strong>Username:</strong> @Model.Username</p>
    <p><strong>Movies added:</strong> @ViewBag.MoviesAdded</p>
    <p><strong>Movies rated:</strong> @ViewBag.RatingsAdded</p>
  </div>
</div>
```

---

## Data loading choices for Details (important)

Recommended default for this project: Use Include + ThenInclude (simple, readable). Projection is optional/advanced if you want maximum efficiency.

Two correct ways to load related data for Movie Details. Pick one and be consistent.

1) Eager loading with Include/ThenInclude (simple and readable)
```csharp path=null start=null
var movie = await _context.Movies
    .AsNoTracking()                               // read-only → less overhead
    .Include(m => m.User)                         // who uploaded
    .Include(m => m.Ratings)                      // list of ratings
    .ThenInclude(r => r.User)                     // user who rated (second hop)
    .FirstOrDefaultAsync(m => m.Id == id);
```
- Fixes “Object reference not set…” when a navigation wasn’t loaded
- Use ThenInclude for the second hop (Ratings → User)

2) Projection (optional, advanced — more efficient for large graphs)
```csharp path=null start=null
var vm = await _context.Movies
    .AsNoTracking()
    .Where(m => m.Id == id)
    .Select(m => new MovieDetailsViewModel
    {
        Id = m.Id,
        Title = m.Title,
        Description = m.Description,
        ReleaseYear = m.ReleaseYear,
        Uploader = m.User.Username,
        Average = m.Ratings.Count == 0 ? 0 : m.Ratings.Average(r => r.Value),
        TotalRatings = m.Ratings.Count,
        Likers = m.Ratings.Select(r => r.User.Username).ToList()
    })
    .FirstOrDefaultAsync();
```
- Only selects columns you actually need
- No Include/ThenInclude necessary

Common gotcha
- If you access m.User.Username or r.User.Username without loading those users (Include/ThenInclude or via projection), you’ll get a null/“object reference” error.

---

## PART 4: MOVIES FEATURE

Protected route guards (Index/Details)

```csharp path=null start=null
if (!IsLoggedIn)
{
    TempData["Error"] = "Please sign in to continue.";
    return RedirectToAction("Login", "Account");
}
```

Auth input normalization (teacher style)

```csharp path=null start=null
// Register POST normalization
vm.Username = (vm.Username ?? string.Empty).Trim();
vm.Email = (vm.Email ?? string.Empty).Trim().ToLowerInvariant();
vm.Password = (vm.Password ?? string.Empty).Trim();
vm.ConfirmPassword = (vm.ConfirmPassword ?? string.Empty).Trim();

// Login POST normalization
vm.Username = (vm.Username ?? string.Empty).Trim();
vm.Password = (vm.Password ?? string.Empty).Trim();
```

ILogger usage (MoviesController)

```csharp path=null start=null
public class MoviesController : Controller
{
  private readonly ILogger<MoviesController> _logger;
  public MoviesController(ApplicationContext ctx, ILogger<MoviesController> logger)
  {
    _context = ctx; _logger = logger;
  }
  public async Task<IActionResult> Index()
  {
    _logger.LogInformation("Movies/Index requested by user {UserId} at {Time}", CurrentUserId, DateTime.UtcNow);
    ...
  }
}
```

Profile: recent movies

```csharp path=/Users/tamarapalmer/2025-wos/week-11-full-stack-mvc/TheRewind/Controllers/AccountController.cs start=120
ViewBag.RecentMovies = user.Movies?
  .OrderByDescending(m => m.CreatedAt)
  .Take(5)
  .ToList();
```

```cshtml path=/Users/tamarapalmer/2025-wos/week-11-full-stack-mvc/TheRewind/Views/Account/Profile.cshtml start=11
@{ var recent = ViewBag.RecentMovies as List<TheRewind.Models.Movie>; }
<h3 class="mt-4">Recent Movies</h3>
@if (recent != null && recent.Count > 0)
{
  <table class="table table-hover align-middle">
    <thead><tr><th>Title</th><th>Genre</th><th>Added On</th></tr></thead>
    <tbody>
      @foreach (var m in recent)
      { <tr><td>@m.Title</td><td>@m.Genre</td><td>@m.CreatedAt.ToString("MMM d, yyyy")</td></tr> }
    </tbody>
  </table>
}
else { <p class="text-muted">No recent movies.</p> }
```

Rating route + form alignment

```csharp path=/Users/tamarapalmer/2025-wos/week-11-full-stack-mvc/TheRewind/Controllers/MoviesController.cs start=197
[HttpPost("{id:int}/rate")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Rate(int id, int value) { ... }
```

```cshtml path=/Users/tamarapalmer/2025-wos/week-11-full-stack-mvc/TheRewind/Views/Movies/Details.cshtml start=63
<form asp-action="Rate" asp-route-id="@Model.Id" method="post">
  <select name="value" class="form-select"> ... </select>
</form>
```

Dev-only error demo routes

```csharp path=/Users/tamarapalmer/2025-wos/week-11-full-stack-mvc/TheRewind/Controllers/ErrorController.cs start=27
[HttpGet("error/not-found")] public IActionResult IntentionalNotFound() => _env.IsDevelopment() ? new StatusCodeResult(404) : NotFound();
[HttpGet("error/unauthorized")] public IActionResult IntentionalUnauthorized() => _env.IsDevelopment() ? new StatusCodeResult(401) : NotFound();
[HttpGet("error/forbidden")] public IActionResult IntentionalForbidden() => _env.IsDevelopment() ? new StatusCodeResult(403) : NotFound();
[HttpGet("error/boom")] public IActionResult Boom() => _env.IsDevelopment() ? new StatusCodeResult(500) : NotFound();
```

MINIMAL FLOW
1) Step 12: Create MoviesController
2) Step 13: Create Movie views (Index, Create, Edit, Details, Delete)
Next → PART 5: Ratings, Routes & Seed Data

### 🎬 Step 12: Create MoviesController

Async pattern (recommended)
Convert controller actions to async and await EF calls. Use AnyAsync, ToListAsync, FirstOrDefaultAsync, AddAsync, SaveChangesAsync.

PRG pattern (Post → Redirect → Get)
All POST actions should redirect to a GET (never return a view directly after a POST) to avoid duplicate form submissions.

Server-side authorization checks (Edit/Delete)
Always verify the logged-in user owns the movie before allowing edits or deletes.
```csharp path=null start=null
if (movie.UserId != CurrentUserId) return Forbid(); // 403 Forbidden
```
In VS Code Explorer:
- Right-click on `Controllers` folder → New File → `MoviesController.cs`

Paste this controller (minimal CRUD + rating post):
```csharp path=null start=null
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheRewind.Models;

namespace TheRewind.Controllers
{
    [Route("movies")]
    public class MoviesController : Controller
    {
        private readonly ApplicationContext _context;
        public MoviesController(ApplicationContext context) => _context = context;

        private int? CurrentUserId => HttpContext.Session.GetInt32("userId");
        private bool IsLoggedIn => CurrentUserId.HasValue;

// GET: /movies
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var movies = await _context.Movies
                .AsNoTracking()
                .Include(m => m.User)
                .ToListAsync();
            ViewBag.IsLoggedIn = IsLoggedIn;
            return View(movies);
        }

// GET: /movies/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();
            var movie = await _context.Movies
                .Include(m => m.User)
                .Include(m => m.Ratings)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie is null) return NotFound();

            var hasRated = IsLoggedIn && await _context.Ratings
                .AnyAsync(r => r.MovieId == movie.Id && r.UserId == CurrentUserId);
            ViewBag.HasRated = hasRated;
            ViewBag.Average = movie.Ratings.Count == 0 ? 0 : movie.Ratings.Average(r => r.Value);
            return View(movie);
        }

// GET: /movies/create
        [HttpGet("create")]
        public IActionResult Create()
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Account");
            return View(new Movie());
        }

// POST: /movies/create
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Movie movie)
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid) return View(movie);
            movie.UserId = CurrentUserId!.Value;
            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

// GET: /movies/{id}/edit
        [HttpGet("{id:int}/edit")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Account");
            if (id is null) return NotFound();
            var movie = await _context.Movies.FindAsync(id);
            if (movie is null) return NotFound();
            if (movie.UserId != CurrentUserId) return Forbid();
            return View(movie);
        }

// POST: /movies/{id}/edit
        [HttpPost("{id:int}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Movie input)
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Account");
            if (id != input.Id) return NotFound();
            var existing = await _context.Movies.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (existing is null) return NotFound();
            if (existing.UserId != CurrentUserId) return Forbid();

            input.UserId = existing.UserId; // preserve owner
            _context.Update(input);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

// GET: /movies/{id}/delete
        [HttpGet("{id:int}/delete")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Account");
            if (id is null) return NotFound();
            var movie = await _context.Movies
                .AsNoTracking()
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie is null) return NotFound();
            if (movie.UserId != CurrentUserId) return Forbid();
            return View(movie);
        }

// POST: /movies/{id}/delete
        [HttpPost("{id:int}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Account");
            var movie = await _context.Movies.FindAsync(id);
            if (movie is not null && movie.UserId == CurrentUserId)
            {
                _context.Movies.Remove(movie);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

// POST: /movies/rate
        [HttpPost("rate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rate(int movieId, int value)
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Account");
            if (value < 1 || value > 5) return BadRequest();

            bool exists = await _context.Ratings.AnyAsync(r => r.MovieId == movieId && r.UserId == CurrentUserId);
            if (exists)
            {
                TempData["Error"] = "You already rated this movie.";
                return RedirectToAction(nameof(Details), new { id = movieId });
            }

            _context.Ratings.Add(new Rating
            {
                MovieId = movieId,
                UserId = CurrentUserId!.Value,
                Value = value
            });
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = movieId });
        }
    }
}
```

Why this step
- Central place for domain CRUD. Also computes rating summary and whether to show the rating form.

What this does (in plain English)
- Index lists movies (with owner names). Details shows a movie and its ratings.
- Create/Edit/Delete are protected: only logged-in users can create; only the owner can edit/delete.
- Rate adds one rating per user per movie (enforced in code and by DB unique index).

Implementation tips
- Protect create/edit/delete: only logged-in users can perform, and only the owner can edit/delete their movie.
- Details action: Include(m => m.User) and Include(m => m.Ratings); compute average and whether current user has rated.

Snippets to use
- actionauth, asyncaction, asynccreate, asyncedit, asyncdelete, trylog

### 🖼️ Step 13: Create Movie Views
In VS Code Explorer:
- Right-click on `Views` folder → New Folder → name it `Movies`
- Right-click on `Views/Movies` folder → New File → create:
  - `Index.cshtml`
  - `Create.cshtml`
  - `Edit.cshtml`
  - `Details.cshtml`
  - `Delete.cshtml`

Paste these minimal views:

Views/Movies/Index.cshtml
```cshtml path=null start=null
@model IEnumerable<TheRewind.Models.Movie>
<h2>Movies</h2>
<p>
@if (ViewBag.IsLoggedIn == true)
{
    <a class="btn btn-primary" asp-action="Create">Add Movie</a>
}
</p>
<table class="table table-striped">
    <thead><tr><th>Title</th><th>Added By</th><th></th></tr></thead>
    <tbody>
    @foreach (var m in Model)
    {
        <tr>
            <td>@m.Title</td>
            <td>@m.User?.Username</td>
            <td><a asp-action="Details" asp-route-id="@m.Id">Details</a></td>
        </tr>
    }
    </tbody>
</table>
```

Views/Movies/Create.cshtml
```cshtml path=null start=null
@model TheRewind.Models.Movie
<h2>New Movie</h2>
<form asp-action="Create" method="post">
    <div class="mb-3">
        <label asp-for="Title" class="form-label"></label>
        <input asp-for="Title" class="form-control" />
        <span asp-validation-for="Title" class="text-danger"></span>
    </div>
    <div class="mb-3">
        <label asp-for="Description" class="form-label"></label>
        <textarea asp-for="Description" class="form-control"></textarea>
    </div>
    <div class="mb-3">
        <label asp-for="ReleaseYear" class="form-label"></label>
        <input asp-for="ReleaseYear" class="form-control" />
    </div>
    <button class="btn btn-success" type="submit">Save</button>
</form>

@section Scripts { <partial name="_ValidationScriptsPartial" /> }
```

Views/Movies/Edit.cshtml
```cshtml path=null start=null
@model TheRewind.Models.Movie
<h2>Edit Movie</h2>
<form asp-action="Edit" method="post">
    <input type="hidden" asp-for="Id" />
    <div class="mb-3">
        <label asp-for="Title" class="form-label"></label>
        <input asp-for="Title" class="form-control" />
        <span asp-validation-for="Title" class="text-danger"></span>
    </div>
    <div class="mb-3">
        <label asp-for="Description" class="form-label"></label>
        <textarea asp-for="Description" class="form-control"></textarea>
    </div>
    <div class="mb-3">
        <label asp-for="ReleaseYear" class="form-label"></label>
        <input asp-for="ReleaseYear" class="form-control" />
    </div>
    <button class="btn btn-primary" type="submit">Update</button>
</form>

@section Scripts { <partial name="_ValidationScriptsPartial" /> }
```

Views/Movies/Details.cshtml (two-column layout with stats + rate form)
```cshtml path=null start=null
@model TheRewind.Models.Movie

@if (TempData["Error"] is string err)
{
  <div class="alert alert-danger">@err</div>
}

<div class="row g-4">
  <div class="col-md-8">
    <h2 class="mb-3">@Model.Title</h2>
    <dl class="row">
      <dt class="col-sm-3">Description</dt>
      <dd class="col-sm-9">@Model.Description</dd>
      <dt class="col-sm-3">Release Year</dt>
      <dd class="col-sm-9">@Model.ReleaseYear</dd>
      <dt class="col-sm-3">Uploaded by</dt>
      <dd class="col-sm-9">@Model.User?.Username</dd>
    </dl>

    @if ((Model.Ratings?.Count ?? 0) > 0)
    {
      <p class="card-text mb-2">Rated by:</p>
      <div class="d-flex flex-wrap gap-2 mb-3">
        @foreach (var r in Model.Ratings)
        {
          <span class="badge rounded-pill text-bg-primary">@r.User?.Username</span>
        }
      </div>
    }

    <div class="mt-3">
      <a class="btn btn-secondary" asp-action="Index">Back</a>
      @if (Context.Session.GetInt32("userId") == Model.UserId)
      {
        <a class="btn btn-primary" asp-action="Edit" asp-route-id="@Model.Id">Edit</a>
        <a class="btn btn-danger" asp-action="Delete" asp-route-id="@Model.Id">Delete</a>
      }
    </div>
  </div>

  <div class="col-md-4">
    <div class="card bg-dark border-secondary">
      <div class="card-body">
        <h5 class="card-title">Stats</h5>
        <p class="mb-2">
          <strong>Average:</strong>
          @((Model.Ratings?.Count ?? 0) == 0 ? "No ratings" : Model.Ratings.Average(r => r.Value).ToString("0.0"))
        </p>
        <p class="mb-3"><strong>Total Ratings:</strong> @(Model.Ratings?.Count ?? 0)</p>

        @if (ViewBag.HasRated == false)
        {
          <form asp-action="Rate" method="post" class="d-flex align-items-end gap-2" data-disable-on-submit>
            <input type="hidden" name="movieId" value="@Model.Id" />
            <div class="flex-grow-1">
              <label class="form-label">Rate</label>
              <select name="value" class="form-select">
                <option>1</option>
                <option>2</option>
                <option>3</option>
                <option>4</option>
                <option>5</option>
              </select>
            </div>
            <button type="submit" class="btn btn-outline-primary">
              <i class="bi bi-star"></i> Submit
            </button>
          </form>
        }
        else
        {
          <div class="text-success">You have already rated this movie.</div>
        }
      </div>
    </div>
  </div>
</div>
```

Optional: prevent rapid repeat clicks on submit (client-side)
Add this to wwwroot/js/site.js
```js path=null start=null
// Disable any form with data-disable-on-submit once submitted
window.addEventListener('DOMContentLoaded', () => {
  document.querySelectorAll('form[data-disable-on-submit]')
    .forEach(form => {
      form.addEventListener('submit', () => {
        const btn = form.querySelector('button[type="submit"]');
        if (btn) { btn.disabled = true; btn.classList.add('disabled'); }
      });
    });
});
```
Include site.js in _Layout if not already present:
```cshtml path=null start=null
<script src="~/js/site.js" asp-append-version="true"></script>
```

Views/Movies/Delete.cshtml
```cshtml path=null start=null
@model TheRewind.Models.Movie
<h2>Confirm Delete</h2>
<p>Are you sure you want to delete "@Model.Title"? This action cannot be undone.</p>
<form asp-action="Delete" method="post">
    <input type="hidden" asp-for="Id" />
    <button type="submit" class="btn btn-danger">Yes, delete</button>
    <a asp-action="Details" asp-route-id="@Model.Id" class="btn btn-secondary">Cancel</a>
</form>
```

Build checkpoint
```bash
# After adding MoviesController + views, verify everything builds and routes work
 dotnet build
 dotnet run
```
Try these URLs:
- /movies
- /movies/create (requires login)
- /movies/{id}

Next → PART 5: Ratings, Routes & Seed Data

Why this step
- Surfaces CRUD and rating UX. Details page conditionally hides rating form if the user already rated.

What this does (in plain English)
- These Razor files render the forms and tables. They use tag helpers (asp-for, asp-action) to generate correct HTML and include anti-forgery tokens automatically.

Snippets to use
- Razor: use strongly typed models (e.g., @model Movie or a DetailsViewModel)
- Use anti-forgery helpers in forms (implicit in Razor form tag helper)

---

## PART 5: RATINGS, ROUTES & SEED DATA

MINIMAL FLOW
1) Step 14: Ratings logic + routes
2) Step 15: Navbar partial + layout
3) Step 16: Error pages (404/500/general)
4) Step 17: Optional: seed data
5) Step 18: Final testing

### ⭐ Step 14: Add Ratings Feature (with Routes)
Update models and controllers for ratings.

Why this step
- Meets rubric: one rating per movie per user enforced in DB (unique index) and in UI (hide form when rated).

Implementation tips
- POST action should check if a rating already exists for (userId, movieId) before Insert.
- In Details, compute average rating and render only if there are any ratings.

Snippets to use
- asyncaction for fetching with Include
- asynccreate for POST rating with ModelState validation

Routes summary you now have
- GET /movies → list
- GET /movies/{id} → details
- GET /movies/create → show form
- POST /movies/create → create
- GET /movies/{id}/edit → edit form
- POST /movies/{id}/edit → update
- GET /movies/{id}/delete → confirm
- POST /movies/{id}/delete → delete
- POST /movies/rate → add rating

### 🎨 Step 15: Update Layout & Navigation (Navbar partial after auth)

Wireframe → Bootstrap mapping (we will apply our own Bootstrap styles)
- Sign Up / Sign In: use a card with form elements (form-control) and a primary button
- All Movies: table.table.table-striped with an Actions column (view/edit/delete links)
- Movie Details: two-column layout (details on left, ratings/stats on right using a small card)
- Add/Edit Movie: simple stacked form using labels + inputs (form-control)
- Delete Movie: confirmation card with a danger button and a secondary cancel link
- Profile: card with username and counts (movies added, movies rated)

Remember: wireframes show structure; your Bootstrap theme (site.css) controls colors and spacing.
Add navbar, dark theme, and polish UI.

When to implement styling/partials
- What this does: provides a consistent top bar with links that change based on login state.
- Start lightweight styling early for readability (base dark theme, container spacing).
- Build the reusable _Navbar partial AFTER authentication is working, so you can conditionally render links for logged-in vs logged-out users.
- Defer heavy polish (custom CSS, animations) until core features and ratings work.

Why this step
- Improves UX and meets conditional nav requirement: show different menu items based on login state.

Implementation tips
- Partial _Navbar.cshtml reads Session (or ViewBag set from controller) to toggle menu items.
- Add Confirm dialogs for logout and delete actions.
- Use layout (_Layout.cshtml) to include Bootstrap CSS/JS and custom CSS.

Create Views/Shared/_Navbar.cshtml
```cshtml path=null start=null
@{
    var isLoggedIn = Context.Session.GetInt32("userId") != null;
    var username = Context.Session.GetString("username");
}
<nav class="navbar navbar-expand-lg navbar-dark bg-dark">
  <div class="container">
    <a class="navbar-brand" asp-controller="Home" asp-action="Index">The Rewind</a>
    <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#mainNav">
      <span class="navbar-toggler-icon"></span>
    </button>
    <div class="collapse navbar-collapse" id="mainNav">
      <ul class="navbar-nav me-auto mb-2 mb-lg-0">
        <li class="nav-item">
          <a class="nav-link" asp-controller="Home" asp-action="Index">Home</a>
        </li>
        <li class="nav-item">
          <a class="nav-link" asp-controller="Movies" asp-action="Index">Movies</a>
        </li>
        @if (isLoggedIn)
        {
          <li class="nav-item">
            <a class="nav-link" asp-controller="Movies" asp-action="Create">Add Movie</a>
          </li>
          <li class="nav-item">
            <a class="nav-link" asp-controller="Account" asp-action="Profile">Profile</a>
          </li>
        }
      </ul>
      <ul class="navbar-nav">
        @if (!isLoggedIn)
        {
          <li class="nav-item"><a class="nav-link" asp-controller="Account" asp-action="Login">Login</a></li>
          <li class="nav-item"><a class="nav-link" asp-controller="Account" asp-action="Register">Register</a></li>
        }
        else
        {
          <li class="nav-item">
            <span class="navbar-text me-2">Hi, @username!</span>
          </li>
          <li class="nav-item">
            <form asp-controller="Account" asp-action="Logout" method="post" class="d-inline">
              <button type="submit" class="btn btn-sm btn-outline-light"
                      onclick="return confirm('Are you sure you want to log out?');">
                Logout
              </button>
            </form>
          </li>
        }
      </ul>
    </div>
  </div>
</nav>
```

Update Views/Shared/_Layout.cshtml
```cshtml path=null start=null
<!DOCTYPE html>
<html lang="en" data-bs-theme="dark">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - The Rewind</title>
<link rel="stylesheet" href="~/lib/bootstrap/css/bootstrap.min.css" />
    <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
    <!-- Optional: Bootstrap Icons -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
</head>
<body>
    <partial name="_Navbar" />
    <main role="main" class="container py-4">
        @RenderBody()
    </main>

<script src="~/lib/bootstrap/js/bootstrap.bundle.min.js"></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

Optional icon usage example (replace text buttons with icons if you like)
```cshtml path=null start=null
<button type="submit" class="btn btn-outline-primary">
  <i class="bi bi-star"></i> Rate
</button>
```

Optional minimal wwwroot/css/site.css (copy-paste)
```css path=null start=null
:root { --gap: 1rem; }
body { background-color: #0b0e14; color: #e6e6e6; }
a { color: #8ab4f8; }
.container { max-width: 960px; }
.navbar-brand { font-weight: 700; }
.table { background: #121826; }
.btn { border-radius: 6px; }
.text-danger { color: #ff6b6b !important; }
```

Build checkpoint
```bash
# After wiring the navbar and layout, run and confirm nav changes with login state
 dotnet build
 dotnet run
```

Optional: add confirmation for delete buttons
- Use a button with onclick confirm, or add data attributes and a small JS helper if you prefer unobtrusive JS.

### 🛡️ Step 16: Add Error Handling (custom 404/500 + general)

Launch profiles (test friendly error pages)
Add a prod-like profile to Properties/launchSettings.json so you can test friendly 404/500 pages.
```json path=null start=null
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "TheRewind (Dev)": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "applicationUrl": "http://localhost:5005",
      "environmentVariables": { "ASPNETCORE_ENVIRONMENT": "Development" }
    },
    "TheRewind (ProdLike)": {
      "commandName": "Project",
      "applicationUrl": "http://localhost:5007;https://localhost:7074",
      "environmentVariables": { "ASPNETCORE_ENVIRONMENT": "Production" }
    }
  }
}
```
Run with:
```bash
dotnet run --launch-profile "TheRewind (ProdLike)"
```

Optional reusable confirm partial
Create Views/Shared/_ConfirmPartial.cshtml
```cshtml path=null start=null
@model (string Action, string Controller, int Id, string Message, string ButtonText)
<div class="card">
  <div class="card-body">
    <p class="mb-3">@Model.Message</p>
    <form asp-action="@Model.Action" asp-controller="@Model.Controller" method="post" class="d-inline">
      <input type="hidden" name="id" value="@Model.Id" />
      <button type="submit" class="btn btn-danger">@Model.ButtonText</button>
    </form>
    <a asp-action="Details" asp-route-id="@Model.Id" class="btn btn-secondary ms-2">Cancel</a>
  </div>
</div>
```
Usage example in Movies/Delete.cshtml
```cshtml path=null start=null
@model TheRewind.Models.Movie
<partial name="_ConfirmPartial" model="(("Delete","Movies", Model.Id, $\"Are you sure you want to delete \"{Model.Title}\"? This cannot be undone.\", "Yes, delete"))" />
```
In VS Code Explorer:
- Right-click on `Controllers` folder → New File → `ErrorController.cs`
- Right-click on `Views` folder → New Folder → name it `Error`
- Right-click on `Views/Error` folder → New File → create:
  - `404.cshtml`
  - `500.cshtml`
  - `General.cshtml`

Paste this controller:
```csharp path=null start=null
using Microsoft.AspNetCore.Mvc;

namespace TheRewind.Controllers
{
    [Route("error")]
    public class ErrorController : Controller
    {
        [Route("{code}")]
        public IActionResult HttpStatusCodeHandler(int code)
        {
            return code switch
            {
                404 => View("404"),
                401 => View("General"),
                403 => View("General"),
                500 => View("500"),
                _ => View("General")
            };
        }

        [Route("500")]
        public IActionResult Error() => View("500");
    }
}
```

Build checkpoint
```bash
# After adding ErrorController + views, cause a 404 (visit /movies/999999)
# and confirm your custom 404 page renders
 dotnet build
 dotnet run
```

Example Error Views (create simple pages with headings):
```cshtml path=null start=null
@* Views/Error/404.cshtml *@
<h2>Page Not Found</h2>
<p>The resource you requested could not be found.</p>

@* Views/Error/500.cshtml *@
<h2>Something went wrong</h2>
<p>An internal error occurred. Please try again later.</p>

@* Views/Error/General.cshtml *@
<h2>Oops</h2>
<p>There was a problem processing your request.</p>
```

Why this step
- Required by rubric; provides friendly feedback for missing IDs (404), unauthorized (401), forbidden (403), and server errors (500).

What this does (in plain English)
- Routes /error/{code} to friendly pages instead of raw error messages.
- Users get clear feedback when URLs are wrong or actions are not allowed.

Connect in Program.cs
- app.UseExceptionHandler("/error/500") and app.UseStatusCodePagesWithReExecute("/error/{0}")

### 🌱 Step 17: Seed Data from Movie API (optional)

User secrets (optional, recommended for real DB passwords)
```bash
# Initialize secrets for this project
 dotnet user-secrets init
# Store MySQL connection string safely (matching MySqlConnection key)
 dotnet user-secrets set "ConnectionStrings:MySqlConnection" "Server=localhost;Database=therewinddb;User=root;Password={{YOUR_PASSWORD}};"
```
Update Program.cs to read from configuration (already shown). The value from user-secrets overrides appsettings.json during development.
In VS Code Explorer:
- Right-click project root → New Folder → Data
- Right-click Data → New File → Seed.cs

Paste this simple seeder (runs at startup if DB empty):
```csharp path=null start=null
using TheRewind.Models;

namespace TheRewind.Data
{
    public static class Seed
    {
        public static void Run(ApplicationContext db)
        {
            if (db.Movies.Any()) return; // already seeded
            db.Movies.AddRange(
                new Movie { Title = "The Shawshank Redemption", Description = "Hope and friendship.", ReleaseYear = 1994, UserId = 1 },
                new Movie { Title = "The Godfather", Description = "Mafia family saga.", ReleaseYear = 1972, UserId = 1 },
                new Movie { Title = "Inception", Description = "Dream within a dream.", ReleaseYear = 2010, UserId = 1 }
            );
            db.SaveChanges();
        }
    }
}
```

Hook it up in Program.cs (after app build):
```csharp path=null start=null
using TheRewind.Data;

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
    Seed.Run(db);
}
```

Use data from your previous MovieApi project
- Copy a few titles/descriptions you like from that project into the seeder above.
- Or export to JSON and build a quick import routine later.

### ✅ Step 18: Final Testing

Teacher assignment compliance checklist (MovieApi + course rubric)
- RESTful routes using attribute routing (Movies + Account) → Steps 12, 11
- Validation on models (StringLength, Range, Required) → Step 3 models code
- Async EF calls (ToListAsync, FirstOrDefaultAsync, SaveChangesAsync) → MoviesController
- AsNoTracking for read-only queries → MoviesController Index/Details
- CSRF protection via [ValidateAntiForgeryToken] on POST actions → Account + Movies controllers
- Conditional navigation (logged-in vs logged-out) → _Navbar partial, Step 15
- Confirmation required before sensitive actions (logout, delete) → Logout confirm, Delete view / optional _ConfirmPartial
- Custom error pages for 404/500 and general errors → ErrorController + views, Step 16
- One rating per movie per user (UI + DB) → Unique index in ApplicationContext, check in MoviesController.Rate
- Use of ViewModels for forms (Login, Register) → Step 10
- Optional seeding or reuse of MovieApi data → Step 17

Logging (recommended)
Add ILogger to controllers to help debug and to record important events.

Example in MoviesController
```csharp path=null start=null
using Microsoft.Extensions.Logging;

public class MoviesController : Controller
{
    private readonly ApplicationContext _context;
    private readonly ILogger<MoviesController> _logger;

    public MoviesController(ApplicationContext context, ILogger<MoviesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        _logger.LogInformation("Movies/Index requested at {Time}", DateTime.UtcNow);
        var movies = await _context.Movies.AsNoTracking().Include(m => m.User).ToListAsync();
        return View(movies);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Movie movie)
    {
        try
        {
            if (!ModelState.IsValid) return View(movie);
            movie.UserId = HttpContext.Session.GetInt32("userId")!.Value;
            _context.Add(movie);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Movie {Title} created by user {UserId}", movie.Title, movie.UserId);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating movie {Title}", movie.Title);
            return StatusCode(500);
        }
    }
}
```

Blurb (what this app delivers)
- A secure, Bootstrap-styled MVC app where users register/login, create their own movies, and rate others' movies (1–5). The app enforces one rating per user per movie, shows average ratings, protects all POSTs with anti-forgery tokens, provides friendly error pages, and offers confirmation prompts for destructive actions—meeting the course rubric and the teacher’s MovieApi patterns.
Test all features end-to-end.

---

## Project Creation & Bootstrap Setup

### Step 1: Create the Project

> 💡 **TIP**: Open Terminal in VS Code with `Ctrl+` ` (backtick) or from menu: Terminal > New Terminal

```bash
# Navigate to your working directory
cd ~/2025-wos/week-11-full-stack-mvc

# Create new MVC project
dotnet new mvc -n TheRewind

# Navigate into project
cd TheRewind

# Open in VS Code
code .
```

> ✅ **CHECK**: You should now see TheRewind folder in VS Code's Explorer panel

### Step 2: Install Required NuGet Packages

```bash
# Entity Framework Core for MySQL
dotnet add package Pomelo.EntityFrameworkCore.MySql

# BCrypt for password hashing
dotnet add package BCrypt.Net-Next

# Tools for migrations
dotnet tool install --global dotnet-ef
```

### Step 3: Set Up Bootstrap with LibMan

Create a `libman.json` file in your project root:

```json
{
  "version": "1.0",
  "defaultProvider": "cdnjs",
  "libraries": [
    {
      "library": "bootstrap@5.3.0",
      "destination": "wwwroot/lib/bootstrap/",
      "files": [
        "css/bootstrap.min.css",
        "css/bootstrap.min.css.map",
        "js/bootstrap.bundle.min.js",
        "js/bootstrap.bundle.min.js.map"
      ]
    },
    {
      "library": "jquery@3.7.0",
      "destination": "wwwroot/lib/jquery/",
      "files": [
        "jquery.min.js",
        "jquery.min.map"
      ]
    },
    {
      "library": "jquery-validate@1.19.5",
      "destination": "wwwroot/lib/jquery-validation/",
      "files": [
        "dist/jquery.validate.min.js",
        "dist/jquery.validate.js"
      ]
    },
    {
      "library": "jquery-validation-unobtrusive@4.0.0",
      "destination": "wwwroot/lib/jquery-validation-unobtrusive/",
      "files": [
        "dist/jquery.validate.unobtrusive.min.js",
        "dist/jquery.validate.unobtrusive.js"
      ]
    }
  ]
}
```

Then restore the libraries:

```bash
# Install libman CLI tool
dotnet tool install -g Microsoft.Web.LibraryManager.Cli

# Restore client-side libraries
libman restore
```

### Step 4: Project Structure Overview

Your project should now have this structure:

```
TheRewind/
├── Controllers/          # MVC Controllers
├── Models/              # Entity Models
├── Services/            # Business logic services
├── ViewModels/          # ViewModels for data shaping
├── Views/               # Razor Views
│   ├── Shared/          # Layout and partials
│   ├── Home/            # Home controller views
│   └── _ViewImports.cshtml
├── wwwroot/             # Static files
│   ├── css/             # Custom CSS
│   │   └── site.css
│   ├── js/              # Custom JavaScript
│   │   └── site.js
│   └── lib/             # Client libraries (Bootstrap, jQuery)
│       ├── bootstrap/   # Bootstrap files
│       │   ├── css/
│       │   │   ├── bootstrap.min.css
│       │   │   └── bootstrap.min.css.map
│       │   └── js/
│       │       ├── bootstrap.bundle.min.js
│       │       └── bootstrap.bundle.min.js.map
│       ├── jquery/      # jQuery files
│       │   ├── jquery.min.js
│       │   └── jquery.min.map
│       ├── jquery-validation/
│       │   └── dist/
│       │       ├── jquery.validate.min.js
│       │       └── jquery.validate.js
│       └── jquery-validation-unobtrusive/
│           └── dist/
│               ├── jquery.validate.unobtrusive.min.js
│               └── jquery.validate.unobtrusive.js
├── Properties/          # Launch settings
├── appsettings.json     # Configuration
└── Program.cs           # Application entry point
```

---

## Database Configuration

### Step 1: Configure appsettings.json

```json
{
  "ConnectionStrings": {
"MySqlConnection": "Server=localhost;Database=therewinddb;User=root;Password=rootroot;"
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

**Security Note:** For production, use user-secrets instead of hardcoding passwords:
```bash
# Initialize user secrets
dotnet user-secrets init

# Set the database password
dotnet user-secrets set "DbPassword" 'rootroot'
```

Then in Program.cs, use:
```csharp
var dbPassword = builder.Configuration["DbPassword"] ?? "rootroot";
var connectionString = $"Server=localhost;Database=therewinddb;User=root;Password={dbPassword};";
```

### Step 2: Update Program.cs

```csharp
using Microsoft.EntityFrameworkCore;
using TheRewind.Models;
using TheRewind.Services;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// SERVICE CONFIGURATION
// ==========================================

// Add MVC services
builder.Services.AddControllersWithViews();

// Configure MySQL Database
var connectionString = builder.Configuration.GetConnectionString("MySqlConnection");
builder.Services.AddDbContext<ApplicationContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Configure Session (for authentication)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register Password Service
builder.Services.AddScoped<IPasswordService, BcryptService>();

// Add HttpContextAccessor for session access in services
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ==========================================
// MIDDLEWARE PIPELINE
// ==========================================

// Configure error handling for production
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error/500");
    app.UseStatusCodePagesWithReExecute("/error/{0}");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession(); // Must come before UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

### Step 3: Configure Launch Profiles

Create or update `Properties/launchSettings.json`:

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "TheRewind": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "https://localhost:7056;http://localhost:5002",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "prod-like": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "applicationUrl": "https://localhost:7056;http://localhost:5002",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Production"
      }
    },
    "IIS Express": {
      "commandName": "IISExpress",
      "launchBrowser": true,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

**Why Launch Profiles Matter:**
- **Development**: Shows detailed error pages, verbose logging
- **Production (prod-like)**: Tests custom error pages, optimized for performance
- Use `dotnet run --launch-profile "prod-like"` to test production behavior

---

## Models & Relationships

> 📁 **WHERE DO FILES GO?**
> - All Model files go in the `Models` folder
> - Each model gets its own file (User.cs, Movie.cs, Rating.cs)
> - ApplicationContext.cs also goes in `Models` folder

### Step 1: Create User Model

```csharp
// Models/User.cs
using System.ComponentModel.DataAnnotations;

namespace TheRewind.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Username is required")]
    [MinLength(2, ErrorMessage = "Username must be at least 2 characters")]
    [MaxLength(50)]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = "";

    [Required]
    public string PasswordHash { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation Properties
    public List<Movie> MoviesCreated { get; set; } = new();
    public List<Rating> Ratings { get; set; } = new();
}
```

### Step 2: Create Movie Model

```csharp
// Models/Movie.cs
using System.ComponentModel.DataAnnotations;

namespace TheRewind.Models;

public class Movie
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [MinLength(2, ErrorMessage = "Title must be at least 2 characters")]
    [MaxLength(100)]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Genre is required")]
    [MinLength(2, ErrorMessage = "Genre must be at least 2 characters")]
    [MaxLength(50)]
    public string Genre { get; set; } = "";

    [Required(ErrorMessage = "Release date is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Release Date")]
    public DateTime ReleaseDate { get; set; }

    [Required(ErrorMessage = "Description is required")]
    [MinLength(10, ErrorMessage = "Description must be at least 10 characters")]
    [MaxLength(1000)]
    public string Description { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Foreign Key - One-to-Many with User
    public int UserId { get; set; }
    public User? User { get; set; }

    // Navigation Property - Many-to-Many through Rating
    public List<Rating> Ratings { get; set; } = new();
}
```

### Step 3: Create Rating Model (Join Table)

```csharp
// Models/Rating.cs
using System.ComponentModel.DataAnnotations;

namespace TheRewind.Models;

public class Rating
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Stars { get; set; }

    [MaxLength(500)]
    public string? Review { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Foreign Keys for Many-to-Many
    public int UserId { get; set; }
    public User? User { get; set; }

    public int MovieId { get; set; }
    public Movie? Movie { get; set; }
}
```

> 🎆 **CHECKPOINT**: You've created all 3 models! Next, we'll set up the database connection.

---

## Database Context & Migrations

### Step 1: Create ApplicationContext

```csharp
// Models/ApplicationContext.cs
using Microsoft.EntityFrameworkCore;

namespace TheRewind.Models;

public class ApplicationContext : DbContext
{
    public ApplicationContext(DbContextOptions<ApplicationContext> options) 
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<Rating> Ratings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure User-Movie relationship (One-to-Many)
        modelBuilder.Entity<Movie>()
            .HasOne(m => m.User)
            .WithMany(u => u.MoviesCreated)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Rating relationships (Many-to-Many)
        modelBuilder.Entity<Rating>()
            .HasOne(r => r.User)
            .WithMany(u => u.Ratings)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Rating>()
            .HasOne(r => r.Movie)
            .WithMany(m => m.Ratings)
            .HasForeignKey(r => r.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ensure one rating per user per movie
        modelBuilder.Entity<Rating>()
            .HasIndex(r => new { r.UserId, r.MovieId })
            .IsUnique();
    }
}
```

### Step 2: Run Migrations

```bash
# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migration to database
dotnet ef database update
```

> 🔧 **TROUBLESHOOTING**: 
> - If you get "dotnet ef not found", run: `dotnet tool install --global dotnet-ef`
> - If you get "Cannot connect to MySQL", make sure MySQL is running and password is correct
> - You should see "Done" message and a new `Migrations` folder created

---

## Authentication System

### Step 1: Create Password Service

> ⚠️ **IMPORTANT**: Create a new folder called `Services` in your project root first!

```csharp
// Services/IPasswordService.cs
namespace TheRewind.Services;

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
```

```csharp
// Services/BcryptService.cs
namespace TheRewind.Services;

public class BcryptService : IPasswordService
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
```

### Step 2: Create ViewModels

> ⚠️ **IMPORTANT**: Create a new folder called `ViewModels` in your project root first!

```csharp
// ViewModels/RegisterViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace TheRewind.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Username is required")]
    [MinLength(2, ErrorMessage = "Username must be at least 2 characters")]
    [MaxLength(50)]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Please confirm your password")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = "";
}
```

```csharp
// ViewModels/LoginViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace TheRewind.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
}
```

### Step 3: Create AccountController

```csharp
// Controllers/AccountController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheRewind.Models;
using TheRewind.Services;
using TheRewind.ViewModels;

namespace TheRewind.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        ApplicationContext context, 
        IPasswordService passwordService,
        ILogger<AccountController> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _logger = logger;
    }

    [HttpGet("register")]
    public IActionResult Register()
    {
        // Redirect if already logged in
        if (HttpContext.Session.GetInt32("UserId") != null)
        {
            return RedirectToAction("Index", "Movies");
        }
        return View();
    }

    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Check if email already exists
        var existingUser = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == model.Email);

        if (existingUser)
        {
            ModelState.AddModelError("Email", "Email already registered");
            return View(model);
        }

        // Create new user
        var user = new User
        {
            Username = model.Username,
            Email = model.Email,
            PasswordHash = _passwordService.HashPassword(model.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Log the user in
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("Username", user.Username);

        _logger.LogInformation("New user registered: {Username}", user.Username);
        
        TempData["Success"] = "Registration successful! Welcome to The Rewind!";
        return RedirectToAction("Index", "Movies");
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        // Redirect if already logged in
        if (HttpContext.Session.GetInt32("UserId") != null)
        {
            return RedirectToAction("Index", "Movies");
        }
        return View();
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user == null || !_passwordService.VerifyPassword(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError("", "Invalid email or password");
            return View(model);
        }

        // Set session
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("Username", user.Username);

        _logger.LogInformation("User logged in: {Username}", user.Username);
        
        TempData["Success"] = $"Welcome back, {user.Username}!";
        return RedirectToAction("Index", "Movies");
    }

    [HttpGet("logout")]
    public IActionResult ConfirmLogout()
    {
        if (HttpContext.Session.GetInt32("UserId") == null)
        {
            return RedirectToAction("Login");
        }
        return View();
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        TempData["Success"] = "You have been logged out successfully";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        if (HttpContext.Session.GetInt32("UserId") is not int userId)
        {
            return RedirectToAction("Login");
        }

        var profile = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new ProfileViewModel
            {
                Username = u.Username,
                Email = u.Email,
                JoinedDate = u.CreatedAt,
                MoviesAdded = u.MoviesCreated.Count,
                MoviesRated = u.Ratings.Count,
                RecentMovies = u.MoviesCreated
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(5)
                    .Select(m => new MovieSummaryViewModel
                    {
                        Id = m.Id,
                        Title = m.Title,
                        Genre = m.Genre,
                        AddedOn = m.CreatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (profile == null)
        {
            return NotFound();
        }

        return View(profile);
    }
}
```

---

## Controllers Implementation

### Step 1: Create MoviesController

```csharp
// Controllers/MoviesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheRewind.Models;
using TheRewind.ViewModels;

namespace TheRewind.Controllers;

public class MoviesController : Controller
{
    private readonly ApplicationContext _context;
    private readonly ILogger<MoviesController> _logger;

    public MoviesController(ApplicationContext context, ILogger<MoviesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("movies")]
    public async Task<IActionResult> Index()
    {
        if (HttpContext.Session.GetInt32("UserId") == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var movies = await _context.Movies
            .AsNoTracking()
            .Select(m => new MovieListViewModel
            {
                Id = m.Id,
                Title = m.Title,
                Genre = m.Genre,
                ReleaseDate = m.ReleaseDate,
                AddedBy = m.User!.Username,
                AverageRating = m.Ratings.Any() 
                    ? Math.Round(m.Ratings.Average(r => r.Stars), 1)
                    : 0,
                RatingCount = m.Ratings.Count
            })
            .OrderByDescending(m => m.ReleaseDate)
            .ToListAsync();

        return View(movies);
    }

    [HttpGet("movies/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        if (HttpContext.Session.GetInt32("UserId") is not int userId)
        {
            return RedirectToAction("Login", "Account");
        }

        var movie = await _context.Movies
            .AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new MovieDetailsViewModel
            {
                Id = m.Id,
                Title = m.Title,
                Genre = m.Genre,
                ReleaseDate = m.ReleaseDate,
                Description = m.Description,
                AddedBy = m.User!.Username,
                AddedById = m.UserId,
                AverageRating = m.Ratings.Any() 
                    ? Math.Round(m.Ratings.Average(r => r.Stars), 1)
                    : 0,
                Ratings = m.Ratings
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new RatingViewModel
                    {
                        Username = r.User!.Username,
                        Stars = r.Stars,
                        Review = r.Review,
                        RatedOn = r.CreatedAt
                    })
                    .ToList(),
                UserHasRated = m.Ratings.Any(r => r.UserId == userId),
                CanEdit = m.UserId == userId
            })
            .FirstOrDefaultAsync();

        if (movie == null)
        {
            return NotFound();
        }

        return View(movie);
    }

    [HttpGet("movies/create")]
    public IActionResult Create()
    {
        if (HttpContext.Session.GetInt32("UserId") == null)
        {
            return RedirectToAction("Login", "Account");
        }

        return View(new MovieFormViewModel());
    }

    [HttpPost("movies/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MovieFormViewModel model)
    {
        if (HttpContext.Session.GetInt32("UserId") is not int userId)
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var movie = new Movie
        {
            Title = model.Title,
            Genre = model.Genre,
            ReleaseDate = model.ReleaseDate,
            Description = model.Description,
            UserId = userId
        };

        _context.Movies.Add(movie);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Movie created: {Title} by UserId: {UserId}", movie.Title, userId);
        
        TempData["Success"] = "Movie added successfully!";
        return RedirectToAction("Details", new { id = movie.Id });
    }

    [HttpGet("movies/{id}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        if (HttpContext.Session.GetInt32("UserId") is not int userId)
        {
            return RedirectToAction("Login", "Account");
        }

        var movie = await _context.Movies
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movie == null)
        {
            return NotFound();
        }

        // Authorization check
        if (movie.UserId != userId)
        {
            return Forbid();
        }

        var model = new MovieFormViewModel
        {
            Title = movie.Title,
            Genre = movie.Genre,
            ReleaseDate = movie.ReleaseDate,
            Description = movie.Description
        };

        return View(model);
    }

    [HttpPost("movies/{id}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MovieFormViewModel model)
    {
        if (HttpContext.Session.GetInt32("UserId") is not int userId)
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var movie = await _context.Movies.FindAsync(id);
        
        if (movie == null)
        {
            return NotFound();
        }

        // Authorization check
        if (movie.UserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to edit movie {MovieId} owned by {OwnerId}", 
                userId, id, movie.UserId);
            return Forbid();
        }

        movie.Title = model.Title;
        movie.Genre = model.Genre;
        movie.ReleaseDate = model.ReleaseDate;
        movie.Description = model.Description;
        movie.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Movie updated: {MovieId} by UserId: {UserId}", id, userId);
        
        TempData["Success"] = "Movie updated successfully!";
        return RedirectToAction("Details", new { id });
    }

    [HttpGet("movies/{id}/delete")]
    public async Task<IActionResult> ConfirmDelete(int id)
    {
        if (HttpContext.Session.GetInt32("UserId") is not int userId)
        {
            return RedirectToAction("Login", "Account");
        }

        var movie = await _context.Movies
            .AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new { m.Id, m.Title, m.UserId })
            .FirstOrDefaultAsync();

        if (movie == null)
        {
            return NotFound();
        }

        if (movie.UserId != userId)
        {
            return Forbid();
        }

        ViewBag.MovieTitle = movie.Title;
        ViewBag.MovieId = movie.Id;
        return View();
    }

    [HttpPost("movies/{id}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (HttpContext.Session.GetInt32("UserId") is not int userId)
        {
            return Unauthorized();
        }

        var movie = await _context.Movies.FindAsync(id);
        
        if (movie == null)
        {
            return NotFound();
        }

        // Authorization check
        if (movie.UserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to delete movie {MovieId} owned by {OwnerId}", 
                userId, id, movie.UserId);
            return Forbid();
        }

        _context.Movies.Remove(movie);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Movie deleted: {MovieId} by UserId: {UserId}", id, userId);
        
        TempData["Success"] = "Movie deleted successfully!";
        return RedirectToAction("Index");
    }

    [HttpPost("movies/{id}/rate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rate(int id, RatingFormViewModel model)
    {
        if (HttpContext.Session.GetInt32("UserId") is not int userId)
        {
            return Unauthorized();
        }

        // Check if movie exists
        var movieExists = await _context.Movies.AnyAsync(m => m.Id == id);
        if (!movieExists)
        {
            return NotFound();
        }

        // Check if user has already rated this movie
        var existingRating = await _context.Ratings
            .AnyAsync(r => r.MovieId == id && r.UserId == userId);

        if (existingRating)
        {
            TempData["Error"] = "You have already rated this movie";
            return RedirectToAction("Details", new { id });
        }

        if (!ModelState.IsValid)
        {
            return RedirectToAction("Details", new { id });
        }

        var rating = new Rating
        {
            MovieId = id,
            UserId = userId,
            Stars = model.Stars,
            Review = model.Review
        };

        _context.Ratings.Add(rating);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Rating added: MovieId {MovieId} by UserId {UserId}, Stars: {Stars}", 
            id, userId, model.Stars);
        
        TempData["Success"] = "Your rating has been added!";
        return RedirectToAction("Details", new { id });
    }
}
```

### Step 2: Create Additional ViewModels

```csharp
// ViewModels/MovieViewModels.cs
using System.ComponentModel.DataAnnotations;

namespace TheRewind.ViewModels;

public class MovieListViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Genre { get; set; } = "";
    public DateTime ReleaseDate { get; set; }
    public string AddedBy { get; set; } = "";
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
}

public class MovieDetailsViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Genre { get; set; } = "";
    public DateTime ReleaseDate { get; set; }
    public string Description { get; set; } = "";
    public string AddedBy { get; set; } = "";
    public int AddedById { get; set; }
    public double AverageRating { get; set; }
    public List<RatingViewModel> Ratings { get; set; } = new();
    public bool UserHasRated { get; set; }
    public bool CanEdit { get; set; }
}

public class MovieFormViewModel
{
    [Required(ErrorMessage = "Title is required")]
    [MinLength(2, ErrorMessage = "Title must be at least 2 characters")]
    [MaxLength(100)]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Genre is required")]
    [MinLength(2, ErrorMessage = "Genre must be at least 2 characters")]
    [MaxLength(50)]
    public string Genre { get; set; } = "";

    [Required(ErrorMessage = "Release date is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Release Date")]
    public DateTime ReleaseDate { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "Description is required")]
    [MinLength(10, ErrorMessage = "Description must be at least 10 characters")]
    [MaxLength(1000)]
    public string Description { get; set; } = "";
}

public class RatingViewModel
{
    public string Username { get; set; } = "";
    public int Stars { get; set; }
    public string? Review { get; set; }
    public DateTime RatedOn { get; set; }
}

public class RatingFormViewModel
{
    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Stars { get; set; }

    [MaxLength(500)]
    public string? Review { get; set; }
}

public class ProfileViewModel
{
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime JoinedDate { get; set; }
    public int MoviesAdded { get; set; }
    public int MoviesRated { get; set; }
    public List<MovieSummaryViewModel> RecentMovies { get; set; } = new();
}

public class MovieSummaryViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Genre { get; set; } = "";
    public DateTime AddedOn { get; set; }
}
```

---

## Views & Bootstrap UI

### Step 1: Configure _ViewImports.cshtml

```csharp
@* Views/_ViewImports.cshtml *@
@using TheRewind
@using TheRewind.Models
@using TheRewind.ViewModels
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

### Step 2: Create _Navbar Partial View

```html
@* Views/Shared/_Navbar.cshtml *@
<nav class="navbar navbar-expand-sm navbar-dark bg-dark border-bottom box-shadow mb-3">
    <div class="container-fluid">
        <a class="navbar-brand" asp-controller="Home" asp-action="Index">
            <i class="bi bi-film"></i> The Rewind
        </a>
        <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target=".navbar-collapse">
            <span class="navbar-toggler-icon"></span>
        </button>
        <div class="navbar-collapse collapse d-sm-inline-flex justify-content-between">
            <ul class="navbar-nav flex-grow-1">
                @if (Context.Session.GetInt32("UserId") != null)
                {
                    <li class="nav-item">
                        <a class="nav-link" asp-controller="Movies" asp-action="Index">
                            <i class="bi bi-collection-play"></i> All Movies
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" asp-controller="Movies" asp-action="Create">
                            <i class="bi bi-plus-circle"></i> Add Movie
                        </a>
                    </li>
                }
            </ul>
            <ul class="navbar-nav">
                @if (Context.Session.GetInt32("UserId") != null)
                {
                    <li class="nav-item dropdown">
                        <a class="nav-link dropdown-toggle" href="#" id="userDropdown" role="button" 
                           data-bs-toggle="dropdown" aria-expanded="false">
                            <i class="bi bi-person-circle"></i> @Context.Session.GetString("Username")
                        </a>
                        <ul class="dropdown-menu dropdown-menu-end" aria-labelledby="userDropdown">
                            <li>
                                <a class="dropdown-item" asp-controller="Account" asp-action="Profile">
                                    <i class="bi bi-person"></i> Profile
                                </a>
                            </li>
                            <li><hr class="dropdown-divider"></li>
                            <li>
                                <a class="dropdown-item" asp-controller="Account" asp-action="ConfirmLogout">
                                    <i class="bi bi-box-arrow-right"></i> Logout
                                </a>
                            </li>
                        </ul>
                    </li>
                }
                else
                {
                    <li class="nav-item">
                        <a class="nav-link" asp-controller="Account" asp-action="Login">
                            <i class="bi bi-box-arrow-in-right"></i> Login
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" asp-controller="Account" asp-action="Register">
                            <i class="bi bi-person-plus"></i> Register
                        </a>
                    </li>
                }
            </ul>
        </div>
    </div>
</nav>
```

### Step 3: Update _Layout.cshtml

```html
@* Views/Shared/_Layout.cshtml *@
<!DOCTYPE html>
<html lang="en" data-bs-theme="dark">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - The Rewind</title>
    <link rel="stylesheet" href="~/lib/bootstrap/css/bootstrap.min.css" />
    <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.0/font/bootstrap-icons.css">
</head>
<body>
    <header>
        @await Html.PartialAsync("_Navbar")
    </header>
    
    <div class="container">
        @if (TempData["Success"] != null)
        {
            <div class="alert alert-success alert-dismissible fade show" role="alert">
                <i class="bi bi-check-circle"></i> @TempData["Success"]
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        }
        @if (TempData["Error"] != null)
        {
            <div class="alert alert-danger alert-dismissible fade show" role="alert">
                <i class="bi bi-exclamation-triangle"></i> @TempData["Error"]
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        }
        
        <main role="main" class="pb-3">
            @RenderBody()
        </main>
    </div>

    <footer class="border-top footer text-muted mt-5">
        <div class="container text-center py-3">
            &copy; @DateTime.Now.Year - The Rewind - Your Movie Rating Community
        </div>
    </footer>
    
    <script src="~/lib/jquery/jquery.min.js"></script>
    <script src="~/lib/bootstrap/js/bootstrap.bundle.min.js"></script>
    <script src="~/lib/jquery-validation/jquery.validate.min.js"></script>
    <script src="~/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js"></script>
    <script src="~/js/site.js" asp-append-version="true"></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

### Step 2: Create Authentication Views

```html
@* Views/Account/Register.cshtml *@
@model RegisterViewModel
@{
    ViewData["Title"] = "Register";
}

<div class="row justify-content-center">
    <div class="col-md-6 col-lg-5">
        <div class="card shadow">
            <div class="card-body">
                <h2 class="card-title text-center mb-4">
                    <i class="bi bi-person-plus-fill"></i> Create Account
                </h2>
                
                <form asp-action="Register" method="post">
                    <div asp-validation-summary="ModelOnly" class="alert alert-danger" role="alert"></div>
                    
                    <div class="mb-3">
                        <label asp-for="Username" class="form-label"></label>
                        <div class="input-group">
                            <span class="input-group-text"><i class="bi bi-person"></i></span>
                            <input asp-for="Username" class="form-control" placeholder="Choose a username" />
                        </div>
                        <span asp-validation-for="Username" class="text-danger small"></span>
                    </div>
                    
                    <div class="mb-3">
                        <label asp-for="Email" class="form-label"></label>
                        <div class="input-group">
                            <span class="input-group-text"><i class="bi bi-envelope"></i></span>
                            <input asp-for="Email" class="form-control" placeholder="your@email.com" />
                        </div>
                        <span asp-validation-for="Email" class="text-danger small"></span>
                    </div>
                    
                    <div class="mb-3">
                        <label asp-for="Password" class="form-label"></label>
                        <div class="input-group">
                            <span class="input-group-text"><i class="bi bi-lock"></i></span>
                            <input asp-for="Password" class="form-control" placeholder="Min 8 characters" />
                        </div>
                        <span asp-validation-for="Password" class="text-danger small"></span>
                    </div>
                    
                    <div class="mb-4">
                        <label asp-for="ConfirmPassword" class="form-label"></label>
                        <div class="input-group">
                            <span class="input-group-text"><i class="bi bi-lock-fill"></i></span>
                            <input asp-for="ConfirmPassword" class="form-control" placeholder="Confirm your password" />
                        </div>
                        <span asp-validation-for="ConfirmPassword" class="text-danger small"></span>
                    </div>
                    
                    <div class="d-grid">
                        <button type="submit" class="btn btn-primary btn-lg">
                            <i class="bi bi-person-plus"></i> Register
                        </button>
                    </div>
                </form>
                
                <hr class="my-4">
                
                <p class="text-center mb-0">
                    Already have an account? 
                    <a asp-controller="Account" asp-action="Login">Login here</a>
                </p>
            </div>
        </div>
    </div>
</div>
```

```html
@* Views/Account/Login.cshtml *@
@model LoginViewModel
@{
    ViewData["Title"] = "Login";
}

<div class="row justify-content-center">
    <div class="col-md-6 col-lg-5">
        <div class="card shadow">
            <div class="card-body">
                <h2 class="card-title text-center mb-4">
                    <i class="bi bi-box-arrow-in-right"></i> Welcome Back
                </h2>
                
                <form asp-action="Login" method="post">
                    <div asp-validation-summary="All" class="alert alert-danger" role="alert"></div>
                    
                    <div class="mb-3">
                        <label asp-for="Email" class="form-label"></label>
                        <div class="input-group">
                            <span class="input-group-text"><i class="bi bi-envelope"></i></span>
                            <input asp-for="Email" class="form-control" placeholder="your@email.com" />
                        </div>
                        <span asp-validation-for="Email" class="text-danger small"></span>
                    </div>
                    
                    <div class="mb-4">
                        <label asp-for="Password" class="form-label"></label>
                        <div class="input-group">
                            <span class="input-group-text"><i class="bi bi-lock"></i></span>
                            <input asp-for="Password" class="form-control" placeholder="Your password" />
                        </div>
                        <span asp-validation-for="Password" class="text-danger small"></span>
                    </div>
                    
                    <div class="d-grid">
                        <button type="submit" class="btn btn-primary btn-lg">
                            <i class="bi bi-box-arrow-in-right"></i> Login
                        </button>
                    </div>
                </form>
                
                <hr class="my-4">
                
                <p class="text-center mb-0">
                    Don't have an account? 
                    <a asp-controller="Account" asp-action="Register">Register here</a>
                </p>
            </div>
        </div>
    </div>
</div>
```

### Step 4: Create Custom CSS for Dark Theme

```css
/* wwwroot/css/site.css */

/* Dark Theme Enhancements */
html[data-bs-theme="dark"] {
    --bs-body-bg: #1a1a1a;
    --bs-dark: #212529;
}

/* Custom scrollbar for dark theme */
::-webkit-scrollbar {
    width: 10px;
}

::-webkit-scrollbar-track {
    background: #2b2b2b;
}

::-webkit-scrollbar-thumb {
    background: #555;
    border-radius: 5px;
}

::-webkit-scrollbar-thumb:hover {
    background: #777;
}

/* Footer positioning */
html {
    position: relative;
    min-height: 100%;
}

body {
    margin-bottom: 60px;
}

.footer {
    position: absolute;
    bottom: 0;
    width: 100%;
    white-space: nowrap;
    line-height: 60px;
}

/* Card hover effects */
.card {
    transition: transform 0.2s;
}

.card:hover {
    transform: translateY(-5px);
}

/* Custom star rating display */
.rating .bi-star-fill {
    color: #ffc107;
}

/* Alert custom styling */
.alert {
    border: none;
    border-radius: 10px;
}

/* Button hover effects */
.btn {
    transition: all 0.3s ease;
}

.btn:hover {
    transform: translateY(-2px);
    box-shadow: 0 5px 15px rgba(0,0,0,0.3);
}
```

### Step 5: Create Movie Views

```html
@* Views/Movies/Index.cshtml *@
@model List<MovieListViewModel>
@{
    ViewData["Title"] = "All Movies";
}

<div class="d-flex justify-content-between align-items-center mb-4">
    <h1><i class="bi bi-collection-play"></i> All Movies</h1>
    <a asp-action="Create" class="btn btn-success">
        <i class="bi bi-plus-circle"></i> Add New Movie
    </a>
</div>

@if (!Model.Any())
{
    <div class="alert alert-info text-center">
        <i class="bi bi-info-circle"></i> No movies yet. Be the first to add one!
    </div>
}
else
{
    <div class="row">
        @foreach (var movie in Model)
        {
            <div class="col-md-6 col-lg-4 mb-4">
                <div class="card h-100 shadow-sm">
                    <div class="card-body">
                        <h5 class="card-title">
                            <a asp-action="Details" asp-route-id="@movie.Id" class="text-decoration-none">
                                @movie.Title
                            </a>
                        </h5>
                        <p class="card-text">
                            <small class="text-muted">
                                <i class="bi bi-tag"></i> @movie.Genre<br>
                                <i class="bi bi-calendar"></i> @movie.ReleaseDate.ToString("MMM yyyy")<br>
                                <i class="bi bi-person"></i> Added by @movie.AddedBy
                            </small>
                        </p>
                        <div class="d-flex justify-content-between align-items-center">
                            <div class="rating">
                                @if (movie.AverageRating > 0)
                                {
                                    <span class="badge bg-warning text-dark">
                                        <i class="bi bi-star-fill"></i> @movie.AverageRating
                                    </span>
                                    <small class="text-muted">(@movie.RatingCount ratings)</small>
                                }
                                else
                                {
                                    <span class="text-muted">No ratings yet</span>
                                }
                            </div>
                            <a asp-action="Details" asp-route-id="@movie.Id" class="btn btn-sm btn-outline-primary">
                                View Details
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        }
    </div>
}
```

```html
@* Views/Movies/Details.cshtml *@
@model MovieDetailsViewModel
@{
    ViewData["Title"] = Model.Title;
}

<div class="row">
    <div class="col-lg-8">
        <div class="card shadow mb-4">
            <div class="card-body">
                <h1 class="card-title">@Model.Title</h1>
                
                <div class="mb-3">
                    <span class="badge bg-secondary">@Model.Genre</span>
                    <span class="badge bg-info text-dark">
                        <i class="bi bi-calendar"></i> @Model.ReleaseDate.ToString("MMMM d, yyyy")
                    </span>
                    @if (Model.AverageRating > 0)
                    {
                        <span class="badge bg-warning text-dark">
                            <i class="bi bi-star-fill"></i> @Model.AverageRating / 5
                        </span>
                    }
                </div>
                
                <p class="lead">@Model.Description</p>
                
                <p class="text-muted">
                    <i class="bi bi-person"></i> Added by @Model.AddedBy
                </p>
                
                @if (Model.CanEdit)
                {
                    <div class="btn-group" role="group">
                        <a asp-action="Edit" asp-route-id="@Model.Id" class="btn btn-warning">
                            <i class="bi bi-pencil"></i> Edit
                        </a>
                        <a asp-action="ConfirmDelete" asp-route-id="@Model.Id" class="btn btn-danger">
                            <i class="bi bi-trash"></i> Delete
                        </a>
                    </div>
                }
            </div>
        </div>
        
        <div class="card shadow">
            <div class="card-header">
                <h4><i class="bi bi-star"></i> Ratings & Reviews</h4>
            </div>
            <div class="card-body">
                @if (!Model.UserHasRated)
                {
                    <form asp-action="Rate" asp-route-id="@Model.Id" method="post" class="mb-4">
                        <h5>Add Your Rating</h5>
                        <div class="row">
                            <div class="col-md-3 mb-3">
                                <label class="form-label">Stars</label>
                                <select name="Stars" class="form-select" required>
                                    <option value="">Choose...</option>
                                    <option value="5">⭐⭐⭐⭐⭐ (5)</option>
                                    <option value="4">⭐⭐⭐⭐ (4)</option>
                                    <option value="3">⭐⭐⭐ (3)</option>
                                    <option value="2">⭐⭐ (2)</option>
                                    <option value="1">⭐ (1)</option>
                                </select>
                            </div>
                            <div class="col-md-9 mb-3">
                                <label class="form-label">Review (Optional)</label>
                                <textarea name="Review" class="form-control" rows="2" maxlength="500"></textarea>
                            </div>
                        </div>
                        <button type="submit" class="btn btn-primary">
                            <i class="bi bi-star"></i> Submit Rating
                        </button>
                    </form>
                }
                
                @if (Model.Ratings.Any())
                {
                    @foreach (var rating in Model.Ratings)
                    {
                        <div class="border-bottom pb-3 mb-3">
                            <div class="d-flex justify-content-between">
                                <div>
                                    <strong>@rating.Username</strong>
                                    <span class="badge bg-warning text-dark ms-2">
                                        @for (int i = 0; i < rating.Stars; i++)
                                        {
                                            <i class="bi bi-star-fill"></i>
                                        }
                                    </span>
                                </div>
                                <small class="text-muted">
                                    @rating.RatedOn.ToString("MMM d, yyyy")
                                </small>
                            </div>
                            @if (!string.IsNullOrWhiteSpace(rating.Review))
                            {
                                <p class="mt-2 mb-0">@rating.Review</p>
                            }
                        </div>
                    }
                }
                else
                {
                    <p class="text-muted">No ratings yet. Be the first to rate this movie!</p>
                }
            </div>
        </div>
    </div>
    
    <div class="col-lg-4">
        <div class="card shadow">
            <div class="card-body">
                <h5>Quick Actions</h5>
                <div class="d-grid gap-2">
                    <a asp-action="Index" class="btn btn-outline-secondary">
                        <i class="bi bi-arrow-left"></i> Back to Movies
                    </a>
                    <a asp-action="Create" class="btn btn-outline-success">
                        <i class="bi bi-plus-circle"></i> Add New Movie
                    </a>
                </div>
            </div>
        </div>
    </div>
</div>
```

### Step 4: Create Movie Form Views

```html
@* Views/Movies/Create.cshtml *@
@model MovieFormViewModel
@{
    ViewData["Title"] = "Add Movie";
}

<div class="row justify-content-center">
    <div class="col-md-8">
        <div class="card shadow">
            <div class="card-header">
                <h3><i class="bi bi-plus-circle"></i> Add New Movie</h3>
            </div>
            <div class="card-body">
                <form asp-action="Create" method="post">
                    <div asp-validation-summary="All" class="alert alert-danger" role="alert"></div>
                    
                    <div class="mb-3">
                        <label asp-for="Title" class="form-label"></label>
                        <input asp-for="Title" class="form-control" />
                        <span asp-validation-for="Title" class="text-danger"></span>
                    </div>
                    
                    <div class="row">
                        <div class="col-md-6 mb-3">
                            <label asp-for="Genre" class="form-label"></label>
                            <input asp-for="Genre" class="form-control" />
                            <span asp-validation-for="Genre" class="text-danger"></span>
                        </div>
                        <div class="col-md-6 mb-3">
                            <label asp-for="ReleaseDate" class="form-label"></label>
                            <input asp-for="ReleaseDate" class="form-control" />
                            <span asp-validation-for="ReleaseDate" class="text-danger"></span>
                        </div>
                    </div>
                    
                    <div class="mb-3">
                        <label asp-for="Description" class="form-label"></label>
                        <textarea asp-for="Description" class="form-control" rows="4"></textarea>
                        <span asp-validation-for="Description" class="text-danger"></span>
                    </div>
                    
                    <div class="d-flex justify-content-between">
                        <a asp-action="Index" class="btn btn-secondary">
                            <i class="bi bi-arrow-left"></i> Cancel
                        </a>
                        <button type="submit" class="btn btn-success">
                            <i class="bi bi-check-circle"></i> Add Movie
                        </button>
                    </div>
                </form>
            </div>
        </div>
    </div>
</div>
```

```html
@* Views/Movies/Edit.cshtml *@
@model MovieFormViewModel
@{
    ViewData["Title"] = "Edit Movie";
}

<div class="row justify-content-center">
    <div class="col-md-8">
        <div class="card shadow">
            <div class="card-header">
                <h3><i class="bi bi-pencil"></i> Edit Movie</h3>
            </div>
            <div class="card-body">
                <form asp-action="Edit" method="post">
                    <div asp-validation-summary="All" class="alert alert-danger" role="alert"></div>
                    
                    <div class="mb-3">
                        <label asp-for="Title" class="form-label"></label>
                        <input asp-for="Title" class="form-control" />
                        <span asp-validation-for="Title" class="text-danger"></span>
                    </div>
                    
                    <div class="row">
                        <div class="col-md-6 mb-3">
                            <label asp-for="Genre" class="form-label"></label>
                            <input asp-for="Genre" class="form-control" />
                            <span asp-validation-for="Genre" class="text-danger"></span>
                        </div>
                        <div class="col-md-6 mb-3">
                            <label asp-for="ReleaseDate" class="form-label"></label>
                            <input asp-for="ReleaseDate" class="form-control" />
                            <span asp-validation-for="ReleaseDate" class="text-danger"></span>
                        </div>
                    </div>
                    
                    <div class="mb-3">
                        <label asp-for="Description" class="form-label"></label>
                        <textarea asp-for="Description" class="form-control" rows="4"></textarea>
                        <span asp-validation-for="Description" class="text-danger"></span>
                    </div>
                    
                    <div class="d-flex justify-content-between">
                        <a asp-action="Details" asp-route-id="@ViewContext.RouteData.Values["id"]" class="btn btn-secondary">
                            <i class="bi bi-arrow-left"></i> Cancel
                        </a>
                        <button type="submit" class="btn btn-warning">
                            <i class="bi bi-save"></i> Save Changes
                        </button>
                    </div>
                </form>
            </div>
        </div>
    </div>
</div>
```

```html
@* Views/Movies/ConfirmDelete.cshtml *@
@{
    ViewData["Title"] = "Confirm Delete";
}

<div class="row justify-content-center">
    <div class="col-md-6">
        <div class="card shadow border-danger">
            <div class="card-header bg-danger text-white">
                <h3><i class="bi bi-exclamation-triangle"></i> Confirm Deletion</h3>
            </div>
            <div class="card-body text-center">
                <p class="lead">Are you sure you want to delete this movie?</p>
                <h4 class="text-danger">@ViewBag.MovieTitle</h4>
                <p class="text-muted">This action cannot be undone.</p>
                
                <form asp-action="Delete" asp-route-id="@ViewBag.MovieId" method="post" class="d-inline">
                    @Html.AntiForgeryToken()
                    <button type="submit" class="btn btn-danger">
                        <i class="bi bi-trash"></i> Yes, Delete
                    </button>
                </form>
                
                <a asp-action="Details" asp-route-id="@ViewBag.MovieId" class="btn btn-secondary">
                    <i class="bi bi-x-circle"></i> Cancel
                </a>
            </div>
        </div>
    </div>
</div>
```

```html
@* Views/Account/ConfirmLogout.cshtml *@
@{
    ViewData["Title"] = "Logout";
}

<div class="row justify-content-center">
    <div class="col-md-6">
        <div class="card shadow">
            <div class="card-body text-center">
                <h3><i class="bi bi-box-arrow-right"></i> Logout</h3>
                <p class="lead">Are you sure you want to log out?</p>
                
                <form asp-action="Logout" method="post" class="d-inline">
                    @Html.AntiForgeryToken()
                    <button type="submit" class="btn btn-danger">
                        <i class="bi bi-box-arrow-right"></i> Yes, Logout
                    </button>
                </form>
                
                <a asp-action="Index" asp-controller="Movies" class="btn btn-secondary">
                    <i class="bi bi-x-circle"></i> Cancel
                </a>
            </div>
        </div>
    </div>
</div>
```

---

## Home Page Setup

### Create HomeController

```csharp
// Controllers/HomeController.cs
using Microsoft.AspNetCore.Mvc;

namespace TheRewind.Controllers;

public class HomeController : Controller
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        // If logged in, redirect to movies
        if (HttpContext.Session.GetInt32("UserId") != null)
        {
            return RedirectToAction("Index", "Movies");
        }
        
        // Otherwise show landing page
        return View();
    }

    [HttpGet("privacy")]
    public IActionResult Privacy()
    {
        return View();
    }
}
```

### Create Home Views

```html
@* Views/Home/Index.cshtml *@
@{
    ViewData["Title"] = "Welcome";
}

<div class="text-center">
    <h1 class="display-4">Welcome to The Rewind</h1>
    <p class="lead">Your community movie rating platform</p>
    
    <div class="row mt-5">
        <div class="col-md-6 offset-md-3">
            <div class="card shadow">
                <div class="card-body">
                    <h3>Get Started</h3>
                    <p>Join our community to rate and review movies!</p>
                    <div class="d-grid gap-2">
                        <a asp-controller="Account" asp-action="Register" class="btn btn-primary btn-lg">
                            <i class="bi bi-person-plus"></i> Create Account
                        </a>
                        <a asp-controller="Account" asp-action="Login" class="btn btn-outline-secondary">
                            <i class="bi bi-box-arrow-in-right"></i> Already have an account? Login
                        </a>
                    </div>
                </div>
            </div>
        </div>
    </div>
    
    <div class="row mt-5">
        <div class="col-md-4">
            <div class="card">
                <div class="card-body">
                    <i class="bi bi-film display-4 text-primary"></i>
                    <h4>Discover Movies</h4>
                    <p>Browse our extensive collection of movies</p>
                </div>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card">
                <div class="card-body">
                    <i class="bi bi-star-fill display-4 text-warning"></i>
                    <h4>Rate & Review</h4>
                    <p>Share your opinions with the community</p>
                </div>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card">
                <div class="card-body">
                    <i class="bi bi-people-fill display-4 text-success"></i>
                    <h4>Join Community</h4>
                    <p>Connect with fellow movie enthusiasts</p>
                </div>
            </div>
        </div>
    </div>
</div>
```

---

## Error Handling

### Step 1: Create ErrorController

```csharp
// Controllers/ErrorController.cs
using Microsoft.AspNetCore.Mvc;

namespace TheRewind.Controllers;

public class ErrorController : Controller
{
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(ILogger<ErrorController> logger)
    {
        _logger = logger;
    }

    [HttpGet("error/{code}")]
    public IActionResult Handle(int code)
    {
        _logger.LogWarning("Error {StatusCode} occurred", code);

        var viewName = code switch
        {
            404 => "NotFound",
            401 => "Unauthorized",
            403 => "Forbidden",
            500 => "ServerError",
            _ => "GenericError"
        };

        Response.StatusCode = code;
        ViewBag.StatusCode = code;
        return View(viewName);
    }
}
```

### Step 2: Create Error Views

```html
@* Views/Error/NotFound.cshtml *@
@{
    ViewData["Title"] = "Page Not Found";
}

<div class="text-center">
    <h1 class="display-1">404</h1>
    <p class="fs-3"><span class="text-danger">Oops!</span> Page not found.</p>
    <p class="lead">The page you're looking for doesn't exist.</p>
    <a asp-controller="Home" asp-action="Index" class="btn btn-primary">
        <i class="bi bi-house"></i> Go Home
    </a>
</div>
```

```html
@* Views/Error/ServerError.cshtml *@
@{
    ViewData["Title"] = "Server Error";
}

<div class="text-center">
    <h1 class="display-1">500</h1>
    <p class="fs-3"><span class="text-danger">Oops!</span> Something went wrong.</p>
    <p class="lead">We're experiencing some technical difficulties. Please try again later.</p>
    <a asp-controller="Home" asp-action="Index" class="btn btn-primary">
        <i class="bi bi-house"></i> Go Home
    </a>
</div>
```

---

## Testing & Final Checklist

### Running the Application

```bash
# Run in development mode
dotnet run

# Run in production-like mode for testing error pages
dotnet run --launch-profile "prod-like"
```

### Testing Checklist

#### Authentication
- [ ] Register new user with valid data
- [ ] Register fails with invalid data (validation messages shown)
- [ ] Login with correct credentials
- [ ] Login fails with incorrect credentials
- [ ] Logout functionality works
- [ ] Session persists across page refreshes

#### Movies CRUD
- [ ] Create new movie (redirects to details)
- [ ] View all movies (shows average ratings)
- [ ] View movie details
- [ ] Edit own movie
- [ ] Cannot edit another user's movie (returns 403)
- [ ] Delete own movie (with confirmation)
- [ ] Cannot delete another user's movie (returns 403)

#### Ratings
- [ ] Rate a movie (1-5 stars)
- [ ] Cannot rate the same movie twice
- [ ] Average rating calculates correctly
- [ ] Ratings display on movie list and details

#### Error Handling
- [ ] 404 page for non-existent routes
- [ ] 401 for unauthenticated access
- [ ] 403 for unauthorized actions
- [ ] Custom error pages display correctly

#### Security
- [ ] All POST forms have CSRF protection
- [ ] Passwords are hashed with BCrypt
- [ ] Authorization checks on edit/delete
- [ ] XSS prevention (try injecting scripts)

#### Performance
- [ ] Using async/await for all database calls
- [ ] AsNoTracking() on read-only queries
- [ ] Proper use of Include() vs Select()

### Common Issues & Solutions

**Issue**: Bootstrap not loading
**Solution**: Check libman.json and ensure `libman restore` was run

**Issue**: Session not persisting
**Solution**: Ensure `app.UseSession()` comes before `app.UseAuthorization()` in Program.cs

**Issue**: Migrations fail
**Solution**: Check connection string in appsettings.json, ensure MySQL is running

**Issue**: Authorization not working
**Solution**: Verify session is set on login and checked in controllers

---

## Final Notes

### Best Practices Implemented
- ✅ Async/await for all database operations
- ✅ AsNoTracking() for read-only queries
- ✅ ViewModels for data shaping
- ✅ CSRF protection on all forms
- ✅ Proper error handling with custom pages
- ✅ Authorization checks for edit/delete
- ✅ Password hashing with BCrypt
- ✅ Bootstrap for responsive UI
- ✅ Confirmation dialogs for destructive actions
- ✅ Logging with ILogger
- ✅ Environment-aware configuration

### Project Structure Benefits
- **Separation of Concerns**: ViewModels separate presentation from domain models
- **Security**: Multiple layers of authentication and authorization
- **User Experience**: Bootstrap provides professional, responsive UI
- **Maintainability**: Clean code structure with proper async patterns
- **Performance**: Optimized queries with projection and AsNoTracking

This guide provides everything needed to build "The Rewind" application from scratch, following all best practices and requirements for the assessment.