# Global Full-Stack MVC Notes — Project-Agnostic Template

Use this as a universal, step-by-step checklist for any ASP.NET Core MVC + EF Core + MySQL project.

## Conventions (replace with your names)
- {PROJECT}: your project name (e.g., BacklogBoss)
- {ENTITY}: singular domain type (e.g., Game)
- {ENTITIES}: plural route/name (e.g., games)
- {CONTEXT}: DbContext name (e.g., ApplicationContext)
- {CONTROLLER}: MVC controller name (e.g., GameController)

## Build Order Overview

### Project-specific: The Rewind — Quick Notes (Dark Theme + EF/Auth Patterns)
- Use data-bs-theme="dark" on the <html> element in Views/Shared/_Layout.cshtml
- Reference ONLY local minified assets under wwwroot/lib (no CDN duplicates)
  - ~/lib/bootstrap/dist/css/bootstrap.min.css
  - ~/lib/bootstrap/dist/js/bootstrap.bundle.min.js
  - ~/lib/jquery/dist/jquery.min.js
- Keep wwwroot/css/site.css minimal; do not force bg-dark/text-light on common elements
- In views, use standard Bootstrap classes (card, table table-hover, btn btn-primary, form-control)
- Remove bg-dark/border-* overrides from cards/tables; let Bootstrap's dark theme style them
- Hard refresh after CSS changes (Cmd+Shift+R)

Core controller patterns to enforce rubric requirements:
```csharp
// Authorization: owner-only edit/delete
if (movie.UserId != CurrentUserId) return Forbid();

// CSRF on ALL POST
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Movie movie) { ... }

// Async EF Core everywhere
var rows = await _context.Movies.AsNoTracking().ToListAsync();

// One rating per user per movie (UI + DB)
bool exists = await _context.Ratings.AnyAsync(r => r.MovieId == id && r.UserId == CurrentUserId);
if (exists) { TempData["Error"] = "You already rated this movie."; return RedirectToAction(nameof(Details), new { id }); }
```

Database unique index to back it up:
```csharp
// Models/ApplicationContext.cs
modelBuilder.Entity<Rating>()
    .HasIndex(r => new { r.UserId, r.MovieId })
    .IsUnique();
```

Navigation flow (PRG assumed):
- Register/Login → /movies (redirect if already logged in)
- Create → /movies/{id}
- Update → /movies/{id}
- Delete → /movies

Production-readiness checks added (The Rewind)
- Protected routes for Movies Index/Details; flash message on redirect
- Error pages (404/401/403/500) with dev-only test routes hidden in Production
- Reusable flash partial (_Flash) under _Layout
- ILogger in MoviesController for Index/Details/Create/Edit/Delete/Rate
- Timestamps (CreatedAt/UpdatedAt) on User/Movie/Rating; updated on edit; shown in UI
- Input normalization on auth POSTs (trim + lowercase email)
- Rating form posts to /movies/{id}/rate with asp-route-id

Packaging for submission (zip correctly)
- From the parent directory of your project, use a relative path so the zip doesn’t include your full filesystem path.
```bash path=null start=null
# Example for The Rewind (top-level folder = TheRewind/)
cd ~/2025-wos/week-11-full-stack-mvc
zip -r TheRewind-submission.zip TheRewind -x "TheRewind/bin/*" "TheRewind/obj/*" "**/.git/**" "**/.DS_Store"

# Example for BacklogBoss (top-level folder = backlog/BacklogBoss/)
cd ~/2025-wos/course-materials/wos-can-code-2025/5-dotnet/full-stack-mvc
zip -r BacklogBoss-submission.zip backlog/BacklogBoss \
  -x "backlog/BacklogBoss/bin/*" "backlog/BacklogBoss/obj/*" "**/.git/**" "**/.DS_Store"

# Verify the zip structure shows the correct top-level folder
unzip -l BacklogBoss-submission.zip | head -n 10
# First entry should look like: "backlog/BacklogBoss/"
```

### Part A: Project Setup
1. Create project and test
2. Install packages
3. Setup Bootstrap with LibMan

### Part B: Database Setup
4. Create Models
5. Create ApplicationContext
6. Configure connection string
7. Wire up Program.cs
8. Run migrations

### Part C: Features (Controllers → Views)
For each feature:
1. Create controller with actions and routes
2. Build and verify controller compiles
3. Create ViewModels
4. Create views with strongly-typed models
5. Add validation and test

## Part A: Project Setup

### Step 1: Create Project and Test
```bash
# Create MVC project
dotnet new mvc -n {PROJECT}
cd {PROJECT}
code .

# Test it runs (in VS Code terminal)
dotnet build
dotnet run
# Verify default page loads, then Ctrl+C
```

### Step 2: Install Packages
```bash
# Database provider and EF design-time services
dotnet add package Pomelo.EntityFrameworkCore.MySql
dotnet add package Microsoft.EntityFrameworkCore.Design

# If adding authentication:
dotnet add package BCrypt.Net-Next

# One-time global install (if not already):
dotnet tool install --global dotnet-ef
```

### Step 3: Setup Bootstrap with LibMan
LibMan setup (libman.json):
```json
{
  "version": "1.0",
  "defaultProvider": "cdnjs",
  "libraries": [
    {
      "library": "bootstrap@5.3.0",
      "destination": "wwwroot/lib/bootstrap/",
      "files": ["css/bootstrap.min.css", "css/bootstrap.min.css.map", 
                "js/bootstrap.bundle.min.js", "js/bootstrap.bundle.min.js.map"]
    },
    {
      "library": "jquery@3.7.0",
      "destination": "wwwroot/lib/jquery/",
      "files": ["jquery.min.js", "jquery.min.map"]
    },
    {
      "library": "jquery-validate@1.19.5",
      "destination": "wwwroot/lib/jquery-validation/",
      "files": ["dist/jquery.validate.min.js", "dist/jquery.validate.js"]
    },
    {
      "library": "jquery-validation-unobtrusive@4.0.0",
      "destination": "wwwroot/lib/jquery-validation-unobtrusive/",
      "files": ["dist/jquery.validate.unobtrusive.min.js", "dist/jquery.validate.unobtrusive.js"]
    }
  ]
}
```
Restore: `libman restore`

_Layout.cshtml:
```html
<html data-bs-theme="dark">
<link rel="stylesheet" href="~/lib/bootstrap/css/bootstrap.min.css" />
<script src="~/lib/jquery/jquery.min.js"></script>
<script src="~/lib/bootstrap/js/bootstrap.bundle.min.js"></script>
```

_ValidationScriptsPartial.cshtml:
```html
<script src="~/lib/jquery/jquery.min.js"></script>
<script src="~/lib/jquery-validation/dist/jquery.validate.min.js"></script>
<script src="~/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js"></script>
```

## Part B: Database Setup

### Step 4: Create Models

**Create Model files**
**In VS Code: Right-click Models folder → New File → {ENTITY}.cs**
```csharp path=null start=null
using System.ComponentModel.DataAnnotations;
namespace {PROJECT}.Models;
public class {ENTITY}
{
  [Key] public int Id { get; set; }
  [Required, MinLength(2)] public string Title { get; set; } = string.Empty;
  [Required, MinLength(2)] public string Platform { get; set; } = string.Empty;
  public bool IsComplete { get; set; } = false;
  [DataType(DataType.MultilineText)] public string Notes { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
  
  // Foreign Key (if tracking owner)
  // public int UserId { get; set; }
  // public User? User { get; set; }
  
  // Navigation for one-to-many
  // public List<Rating> Ratings { get; set; } = new();
}
```

### Relationship Patterns
```csharp path=null start=null
// One-to-Many (User owns many Items)
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public List<{ENTITY}> {ENTITIES} { get; set; } = new();
}

public class {ENTITY}
{
    public int UserId { get; set; }  // Foreign Key
    public User? User { get; set; }  // Navigation Property
}

// Many-to-Many (through join table)
public class Rating
{
    public int Id { get; set; }
    public int Value { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int {ENTITY}Id { get; set; }
    public {ENTITY}? {ENTITY} { get; set; }
}

// In ApplicationContext.OnModelCreating:
modelBuilder.Entity<Rating>()
    .HasIndex(r => new { r.UserId, r.{ENTITY}Id })
    .IsUnique(); // One rating per user per item
```

### Step 5: Create ApplicationContext
**In VS Code: Right-click Models folder → New File → {CONTEXT}.cs**
```csharp path=null start=null
using Microsoft.EntityFrameworkCore;
namespace {PROJECT}.Models;
public class {CONTEXT} : DbContext
{
  public {CONTEXT}(DbContextOptions<{CONTEXT}> options) : base(options) { }
  public DbSet<{ENTITY}> {ENTITIES} { get; set; } = null!;
  // Add more DbSets as needed:
  // public DbSet<User> Users { get; set; }
  // public DbSet<Rating> Ratings { get; set; }
}
```

### Step 6: Configure Connection String
**In VS Code: Open existing appsettings.json and add:**
```json
{
  "ConnectionStrings": {
    "MySqlConnection": "Server=localhost;Database={PROJECT}db;User=root;Password=rootroot;"
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

### Step 7: Configure Program.cs

### Step 8: Run Migrations
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Part C: Features Implementation

### Launch Profiles (optional)
**Create/update Properties/launchSettings.json:**
```json
{
  "profiles": {
    "{PROJECT}": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "prod-like": {
      "commandName": "Project",
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Production"
      }
    }
  }
}
```

**In VS Code: Open existing Program.cs and update:**
```csharp path=null start=null
using Microsoft.EntityFrameworkCore;
using {PROJECT}.Models;
// using {PROJECT}.Services; // if using authentication

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Session (for authentication)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// EF Core + MySQL
var connectionString = builder.Configuration.GetConnectionString("MySqlConnection");
builder.Services.AddDbContext<{CONTEXT}>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Password service (if using authentication)
// builder.Services.AddScoped<IPasswordService, BcryptService>();

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
app.UseSession(); // Must come before UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```


### Step 9: Create Controllers (Build controllers first!)
**In VS Code: Right-click Controllers folder → New File → {CONTROLLER}.cs**
```csharp path=null start=null
[Route("{ENTITIES}")]
public class {CONTROLLER} : Controller
{
  private readonly {CONTEXT} _context;
  public {CONTROLLER}({CONTEXT} context) => _context = context;

  [HttpGet("")]                      // GET /{ENTITIES}
  public async Task<IActionResult> Index()
  {
    var rows = await _context.{ENTITIES}.AsNoTracking().ToListAsync();
    return View("Index", rows);
  }

  [HttpGet("new")]                  // GET /{ENTITIES}/new
  public IActionResult New() => View("New", new {ENTITY}ViewModel());

  [HttpPost("create")]              // POST /{ENTITIES}/create
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Create({ENTITY}ViewModel vm)
  {
    vm.Title = (vm.Title ?? "").Trim();
    vm.Platform = (vm.Platform ?? "").Trim();
    vm.Notes = (vm.Notes ?? "").Trim();
    if (!ModelState.IsValid) return View("New", vm);
    var e = new {ENTITY} { Title = vm.Title, Platform = vm.Platform, IsComplete = vm.IsComplete, Notes = vm.Notes };
    _context.{ENTITIES}.Add(e); 
    await _context.SaveChangesAsync();
    return RedirectToAction(nameof(Index)); // PRG
  }

  [HttpGet("{id}")]                 // GET /{ENTITIES}/{id}
  public async Task<IActionResult> Details(int id)
  {
    var e = await _context.{ENTITIES}.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    if (e is null) return NotFound();
    return View("Details", e);
  }

  [HttpGet("{id}/edit")]            // GET /{ENTITIES}/{id}/edit
  public async Task<IActionResult> Edit(int id)
  {
    var e = await _context.{ENTITIES}.FirstOrDefaultAsync(x => x.Id == id);
    if (e is null) return NotFound();
    var vm = new {ENTITY}ViewModel { Title = e.Title, Platform = e.Platform, IsComplete = e.IsComplete, Notes = e.Notes };
    ViewBag.Id = id; return View("Edit", vm);
  }

  [HttpPost("{id}/update")]         // POST /{ENTITIES}/{id}/update
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Update(int id, {ENTITY}ViewModel vm)
  {
    vm.Title = (vm.Title ?? "").Trim(); vm.Platform = (vm.Platform ?? "").Trim(); vm.Notes = (vm.Notes ?? "").Trim();
    if (!ModelState.IsValid) { ViewBag.Id = id; return View("Edit", vm); }
    var e = await _context.{ENTITIES}.FirstOrDefaultAsync(x => x.Id == id); 
    if (e is null) return NotFound();
    e.Title = vm.Title; e.Platform = vm.Platform; e.IsComplete = vm.IsComplete; e.Notes = vm.Notes; e.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync(); 
    return RedirectToAction(nameof(Details), new { id });
  }

  [HttpGet("{id}/delete")]          // GET /{ENTITIES}/{id}/delete
  public async Task<IActionResult> ConfirmDelete(int id)
  {
    var e = await _context.{ENTITIES}.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    if (e is null) return NotFound();
    return View("ConfirmDelete", new ConfirmDeleteViewModel { Id = e.Id, Title = e.Title });
  }

  [HttpPost("{id}/delete")]         // POST /{ENTITIES}/{id}/delete
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Delete(int id, ConfirmDeleteViewModel vm)
  {
    if (id != vm.Id) return BadRequest();
    var e = await _context.{ENTITIES}.FirstOrDefaultAsync(x => x.Id == id); 
    if (e is null) return NotFound();
    _context.{ENTITIES}.Remove(e); 
    await _context.SaveChangesAsync();
    return RedirectToAction(nameof(Index));
  }
}
```

## Authentication Pattern (if needed)

### Password Service
Create `Services` folder:
```csharp path=null start=null
// Services/IPasswordService.cs
namespace {PROJECT}.Services;
public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

// Services/BcryptService.cs  
using BCrypt.Net;
namespace {PROJECT}.Services;
public class BcryptService : IPasswordService
{
    public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    public bool VerifyPassword(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
```

### Authentication ViewModels
```csharp path=null start=null
// ViewModels/LoginViewModel.cs
using System.ComponentModel.DataAnnotations;
namespace {PROJECT}.ViewModels;
public class LoginViewModel
{
    [Required] public string Username { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
}

// ViewModels/RegisterViewModel.cs
public class RegisterViewModel
{
    [Required, StringLength(32, MinimumLength = 2)] public string Username { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Compare("Password")] public string ConfirmPassword { get; set; } = string.Empty;
}
```

### Session Check Pattern
```csharp path=null start=null
// In controller actions
if (HttpContext.Session.GetInt32("userId") == null)
{
    return RedirectToAction("Login", "Account");
}

// Set session on login
HttpContext.Session.SetInt32("userId", user.Id);
HttpContext.Session.SetString("username", user.Username);

// Clear on logout
HttpContext.Session.Clear();
```

## View Examples

### Navbar Partial (_Navbar.cshtml)
```html
@{
    var isLoggedIn = Context.Session.GetInt32("userId") != null;
    var username = Context.Session.GetString("username");
}
<nav class="navbar navbar-expand-lg navbar-dark bg-dark">
  <div class="container">
    <a class="navbar-brand" asp-controller="Home" asp-action="Index">{PROJECT}</a>
    <ul class="navbar-nav me-auto">
      <li class="nav-item"><a class="nav-link" asp-controller="{CONTROLLER}" asp-action="Index">{ENTITIES}</a></li>
      @if (isLoggedIn) {
        <li class="nav-item"><a class="nav-link" asp-controller="{CONTROLLER}" asp-action="New">Add {ENTITY}</a></li>
      }
    </ul>
    <ul class="navbar-nav">
      @if (!isLoggedIn) {
        <li class="nav-item"><a class="nav-link" asp-controller="Account" asp-action="Login">Login</a></li>
        <li class="nav-item"><a class="nav-link" asp-controller="Account" asp-action="Register">Register</a></li>
      } else {
        <li class="nav-item"><span class="navbar-text me-2">Hi, @username!</span></li>
        <li class="nav-item">
          <form asp-controller="Account" asp-action="Logout" method="post" class="d-inline">
            <button type="submit" class="btn btn-sm btn-outline-light">Logout</button>
          </form>
        </li>
      }
    </ul>
  </div>
</nav>
```

### Form View Example (New.cshtml)
```html
@model {PROJECT}.ViewModels.{ENTITY}ViewModel
<h2>New {ENTITY}</h2>
<form asp-action="Create" method="post">
    <div asp-validation-summary="All" class="text-danger"></div>
    <div class="mb-3">
        <label asp-for="Title" class="form-label"></label>
        <input asp-for="Title" class="form-control" />
        <span asp-validation-for="Title" class="text-danger"></span>
    </div>
    <button type="submit" class="btn btn-primary">Save</button>
</form>
@section Scripts { <partial name="_ValidationScriptsPartial" /> }
```

## Error Handling

### ErrorController
```csharp path=null start=null
[Route("error")]
public class ErrorController : Controller
{
    [Route("{code}")]
    public IActionResult HttpStatusCodeHandler(int code)
    {
        return code switch
        {
            404 => View("404"),
            500 => View("500"),
            _ => View("General")
        };
    }
}
```

### Step 10: Create ViewModels
**In VS Code: Right-click project root → New Folder → ViewModels**
**Then right-click ViewModels folder → New File → create your ViewModels:**
```csharp path=null start=null
using System.ComponentModel.DataAnnotations;
public class {ENTITY}ViewModel : IValidatableObject
{
  [Required, MinLength(2)] public string Title { get; set; } = string.Empty;
  [Required, MinLength(2)] public string Platform { get; set; } = string.Empty;
  public bool IsComplete { get; set; }
  [DataType(DataType.MultilineText)] public string Notes { get; set; } = string.Empty; // optional; enforce length if provided
  public IEnumerable<ValidationResult> Validate(ValidationContext _) {
    if (!string.IsNullOrWhiteSpace(Notes) && Notes.Trim().Length < 5)
      yield return new ValidationResult("Notes must be at least 5 characters when provided", new[]{nameof(Notes)});
  }
}
public class ConfirmDeleteViewModel { public int Id { get; set; } public string Title { get; set; } = string.Empty; }
```

13) PRG + anti-forgery + normalization
- Each POST action uses [ValidateAntiForgeryToken] and redirects (PRG) on success.
- Forms rendered by Razor include the hidden __RequestVerificationToken automatically; keep the default form tag-helpers.
- Normalize (Trim) strings before ModelState validation.

14) Testing Checklist
- Optional field rule via IValidatableObject (as above).
- Filter by status using a nullable query param, e.g., GET /{ENTITIES}?isComplete=true|false, and conditionally apply Where in LINQ.

Commands cheat sheet
```bash path=null start=null
# run
 dotnet watch run
# migrations
 dotnet ef migrations add <Name>
 dotnet ef database update
```

Appendix (previous transcript summary retained below)

# Full-Stack MVC Notes — Teacher Transcript Summary (2025-09-08)

Recording (79 mins): https://fathom.video/calls/404070505?timestamp=241.837284

Goal: A concise, step-by-step guide that mirrors the live “BookShelf” walkthrough. Use this as your master checklist for building the MVC app this week.

1) Scaffold and run
```bash
# create project and open it
 dotnet new mvc -n BookShelf && cd BookShelf && code .
# run with hot reload
 dotnet watch run
```

2) Packages and EF CLI
```bash
# required packages
 dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.17
 dotnet add package Pomelo.EntityFrameworkCore.MySql --version 8.0.3
# install/verify EF CLI (once)
 dotnet tool install --global dotnet-ef || dotnet tool update --global dotnet-ef
 dotnet ef --version
```

3) Assets and layout (Bootstrap min + dark theme)
- Keep only minified Bootstrap files under wwwroot/lib/bootstrap/dist:
  - css: bootstrap.min.css (+ .map)
  - js: bootstrap.bundle.min.js (+ .map)
- In Views/Shared/_Layout.cshtml:
  - Set <html data-bs-theme="dark">
  - Reference local assets:
    - <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
    - <script src="~/lib/jquery/dist/jquery.min.js"></script>
    - <script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
  - Ensure site.css/site.js exist under wwwroot/css and wwwroot/js
- Create Views/Shared/_Navbar.cshtml and include it at the top of the layout body

Bootstrap dark theme — teacher pattern (Vinyl Countdown)
- Put data-bs-theme="dark" on the <html> tag in Views/Shared/_Layout.cshtml
- Reference ONLY local, minified assets under wwwroot/lib (no CDN duplicates)
  - css: ~/lib/bootstrap/dist/css/bootstrap.min.css (+ .map)
  - js:  ~/lib/bootstrap/dist/js/bootstrap.bundle.min.js (+ .map)
  - jquery: ~/lib/jquery/dist/jquery.min.js
  - validation: jquery-validation(+unobtrusive) under wwwroot/lib
- Keep wwwroot/css/site.css minimal — do NOT hardcode dark backgrounds or light text colors
- Use standard Bootstrap classes in views (card, table table-hover, btn btn-primary, form-control)
- Do NOT add bg-dark/border-dark/etc to cards/tables; let Bootstrap’s dark theme style them
- Avoid custom CSS that sets color on generic tags (p, span, dt, dd) — it can fight Bootstrap variables

Minimal _Layout example (local assets + dark theme)
```html path=/Users/tamarapalmer/2025-wos/week-11-full-stack-mvc/TheRewind/Views/Shared/_Layout.cshtml start=1
<!DOCTYPE html>
<html lang="en" data-bs-theme="dark">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>@ViewData["Title"] - App</title>
  <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
  <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
</head>
<body>
  <header>@await Html.PartialAsync("_Navbar")</header>
  <div class="container"><main class="pb-3">@RenderBody()</main></div>
  <script src="~/lib/jquery/dist/jquery.min.js"></script>
  <script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
  <script src="~/js/site.js" asp-append-version="true"></script>
  @RenderSection("Scripts", required: false)
</body>
</html>
```

Troubleshooting dark theme
- Header is dark but body isn’t → Ensure data-bs-theme="dark" is on <html>, not a child element
- Text invisible in selects/tables → Remove custom CSS overrides; rely on Bootstrap’s variables
- Buttons/alerts look “light” → You’re mixing CDN and local or missing bundle JS; keep only local min files
- Styles not updating → Hard refresh the browser (Cmd+Shift+R)
- 404 on assets → Check the exact paths: ~/lib/bootstrap/dist/... and ~/css/site.css

- Enable Tag Helpers (Views/_ViewImports.cshtml):
  - @addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers

4) Model and DbContext
- Models/Book.cs: Id, Title, Author, Genre, ReleaseYear (int), Description, CreatedAt (UTC), UpdatedAt (UTC), HasBeenRead (bool)
- Models/BookContext.cs: DbSet<Book> Books; ctor with DbContextOptions<BookContext>

5) appsettings + Program wiring
- appsettings.json → ConnectionStrings: { "MySQLConnection": "Server=localhost;port=3306;userid=root;password=YOUR_PASSWORD;database=BookshelfDB" }
- Program.cs →
  - var cs = builder.Configuration.GetConnectionString("MySQLConnection");
  - builder.Services.AddDbContext<BookContext>(o => o.UseMySql(cs, ServerVersion.AutoDetect(cs)));
  - Ensure app.UseStaticFiles() is enabled

6) Migrations
```bash
 dotnet ef migrations add InitialCreate
 dotnet ef database update
# if you add new fields later (timestamps/booleans), make another migration
 dotnet ef migrations add AddedTimestamps
 dotnet ef database update
```

7) Controller and routes
- Controllers/BookController.cs with [Route("books")]
- Inject BookContext via ctor (private readonly BookContext _context)
- Actions:
  - [HttpGet("")] AllBooks() → start with mocked HTML table
  - [HttpGet("new")] NewBook() → returns form view with ViewModel
  - [HttpPost("create")] CreateBook(BookFormViewModel vm): normalize Trim(), validate, add, SaveChanges, RedirectToAction(AllBooks) (PRG)

8) ViewModel and validation
- ViewModels/BookFormViewModel.cs
  - Title/Author/Genre: [Required], [MinLength(2)]
  - Description: [Required], [MinLength(10)]
  - ReleaseYear: [Range(1200, 2030)] (Range needs constants; for dynamic year +5, use custom validation)
  - HasBeenRead: bool

9) Views
- Views/Book/AllBooks.cshtml: table.table-striped with Title, Author, Genre, Actions; align-middle + d-flex gap-2 for buttons
- Views/Book/NewBook.cshtml:
  - @model BookFormViewModel
  - <div asp-validation-summary="All" class="text-danger"></div>
  - Bootstrap form with asp-for helpers; form-switch for HasBeenRead; submit aligned right

10) Normalize + PRG
- Always trim string inputs in POST actions before ModelState validation
- If invalid, return same view + vm to show field errors
- If valid, redirect after SaveChanges (PRG)

Troubleshooting (from session)
- Bootstrap not loading → only .map files present? add bootstrap.min.css and bootstrap.bundle.min.js (with maps)
- site.css 404 → create wwwroot/css/site.css and reference it in _Layout (asp-append-version)
- "~" not resolving → ensure @addTagHelper in Views/_ViewImports.cshtml
- HTTPS warning on http profile → harmless for styling; you can run https profile if desired

Commands cheat sheet
```bash
# run
 dotnet watch run
# migrations
 dotnet ef migrations add <Name>
 dotnet ef database update
```

Goal: One compact, practical reference that walks you through creating a full‑stack ASP.NET app using EF Core + MySQL, covering CRUD (API + MVC), LINQ examples, Razor views/partials/layouts, Bootstrap (min), and embedding maps (Leaflet). Ready-to-use snippets included.

Table of contents
- Prerequisites
- Project scaffolding (commands)
- NuGet packages & tools
- Models (Movie example)
- DbContext (MovieContext)
- appsettings.json and connection string
- Program.cs (register EF Core + MySQL)
- Create DB via EF Core migrations
- API: MoviesController (CRUD — async)
- MVC: MoviesController (Views + Partials + CRUD)
- Razor views: layout, partial, index, create/edit forms
- LINQ cheat sheet (List vs DbSet differences)
- Maps in Views (Leaflet example + passing data)
- Bootstrap (min) usage and small UI tips
- Testing (Postman / Thunder Client / curl)
- Production & deployment notes
- Troubleshooting common issues
- Next steps & resources

---

## 1) Prerequisites
- .NET 8 SDK installed (7/6 ok too)
- MySQL server accessible (local or hosted)
- VS Code or Visual Studio
- Basic C#, EF Core, Razor knowledge

Optional installers
```bash
# Mac
brew install --cask dotnet-sdk
brew install mysql

# Windows (PowerShell admin)
choco install dotnet-sdk
choco install mysql
```

---

## 2) Project scaffolding (quick commands)
```bash
# create a new MVC project (includes Controllers + Views)
dotnet new mvc -n MovieApp
cd MovieApp
# optionally create API-only project:
# dotnet new webapi -n MovieApi
```

---

## 3) NuGet packages & CLI tools
```bash
# EF Core CLI (install once globally)
dotnet tool install --global dotnet-ef
# Verify CLI is available
dotnet ef --version

# EF Core design-time package (required)
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.17

# Pomelo MySQL provider (EF Core -> MySQL)
dotnet add package Pomelo.EntityFrameworkCore.MySql --version 8.0.3

# (Optional) Microsoft provider for MySQL (rare)
# dotnet add package MySql.EntityFrameworkCore
```

---

## 4) Models (Movie example)
Create Models/Movie.cs:
```csharp
using System;
using System.ComponentModel.DataAnnotations;
namespace MovieApp.Models;
public class Movie
{
    public int Id { get; set; } // EF Core treats "Id" as PK by convention
    [Required]
    public string Title { get; set; } = string.Empty;
    public string Director { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public DateTime? ReleaseDate { get; set; }
    public double? Rating { get; set; }
    public int DurationInMinutes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    // Optional coords for maps
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
```
Tip: Use UTC (DateTime.UtcNow) for timestamps.

---

## 5) DbContext (MovieContext)
Create Models/MovieContext.cs:
```csharp
using Microsoft.EntityFrameworkCore;
namespace MovieApp.Models;
public class MovieContext : DbContext
{
    public MovieContext(DbContextOptions<MovieContext> options) : base(options) {}
    public DbSet<Movie> Movies { get; set; }
}
```

---

## 6) appsettings.json — connection string
Add to appsettings.json:
```json
{
  "ConnectionStrings": {
    "MySqlConnection": "server=localhost;user=root;password=your_password;database=movie_app_db"
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
Security note: In development you can use appsettings.Development.json or user‑secrets.

---

## 7) Program.cs — configure EF Core + MySQL
Minimal hosting model:
```csharp
using Microsoft.EntityFrameworkCore;
using MovieApp.Models;
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("MySqlConnection");

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<MovieContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```
Note: ServerVersion.AutoDetect() probes the DB version — fine in dev.

---

## 8) Create DB & migrations
```bash
# initial migration
dotnet ef migrations add InitialCreate
# apply migration (creates DB + tables)
dotnet ef database update
```
This will add a Migrations/ folder.

---

## 9) API: MoviesController (CRUD — async)
Create Controllers/Api/MoviesController.cs:
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieApp.Models;

[Route("api/[controller]")]
[ApiController]
public class MoviesController : ControllerBase
{
    private readonly MovieContext _context;
    public MoviesController(MovieContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Movie>>> GetAll()
    {
        var movies = await _context.Movies.ToListAsync();
        if (movies.Count == 0) return NotFound("No movies found.");
        return Ok(movies);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Movie>> Get(int id)
    {
        var movie = await _context.Movies.FindAsync(id);
        if (movie == null) return NotFound();
        return Ok(movie);
    }

    [HttpPost]
    public async Task<ActionResult<Movie>> Create(Movie movie)
    {
        _context.Movies.Add(movie);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = movie.Id }, movie);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Movie updated)
    {
        if (id != updated.Id) return BadRequest();
        var existing = await _context.Movies.FindAsync(id);
        if (existing == null) return NotFound();
        existing.Title = updated.Title;
        existing.Director = updated.Director;
        existing.Genre = updated.Genre;
        existing.ReleaseDate = updated.ReleaseDate;
        existing.Rating = updated.Rating;
        existing.DurationInMinutes = updated.DurationInMinutes;
        existing.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var movie = await _context.Movies.FindAsync(id);
        if (movie == null) return NotFound();
        _context.Movies.Remove(movie);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
```
Use async methods (ToListAsync, FindAsync, SaveChangesAsync) for scalability.

---

## 10) MVC: MoviesController (Views + Partials + CRUD)
Create Controllers/MoviesController.cs:
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieApp.Models;

public class MoviesController : Controller
{
    private readonly MovieContext _context;
    public MoviesController(MovieContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var movies = await _context.Movies.OrderBy(m => m.Title).ToListAsync();
        return View(movies);
    }

    public async Task<IActionResult> Details(int id)
    {
        var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == id);
        if (movie == null) return NotFound();
        return View(movie);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Movie movie)
    {
        if (!ModelState.IsValid) return View(movie);
        _context.Movies.Add(movie);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var movie = await _context.Movies.FindAsync(id);
        if (movie == null) return NotFound();
        return View(movie);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Movie movie)
    {
        if (id != movie.Id) return BadRequest();
        if (!ModelState.IsValid) return View(movie);
        var existing = await _context.Movies.FindAsync(id);
        if (existing == null) return NotFound();
        existing.Title = movie.Title;
        existing.Director = movie.Director;
        existing.Genre = movie.Genre;
        existing.ReleaseDate = movie.ReleaseDate;
        existing.Rating = movie.Rating;
        existing.DurationInMinutes = movie.DurationInMinutes;
        existing.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var movie = await _context.Movies.FindAsync(id);
        if (movie == null) return NotFound();
        return View(movie);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var movie = await _context.Movies.FindAsync(id);
        if (movie != null) _context.Movies.Remove(movie);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
```

---

## 11) Razor views: layout, partial, index, create/edit
_Layout.cshtml (Bootstrap min):
```html
<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>@ViewData["Title"] - MovieApp</title>
  <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" />
  <link rel="stylesheet" href="~/css/site.css" />
</head>
<body>
  <div class="container">@RenderBody()</div>
  <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
  @RenderSection("Scripts", required: false)
</body>
</html>
```

Views/Movies/Index.cshtml:
```cshtml
@model IEnumerable<MovieApp.Models.Movie>
@{ ViewData["Title"] = "Movies"; }
<h1>Movies</h1>
<p><a class="btn btn-primary" asp-action="Create">Create new</a></p>
<table class="table table-striped">
  <thead><tr><th>Title</th><th>Director</th><th>Genre</th><th></th></tr></thead>
  <tbody>
  @foreach (var m in Model) {
    <tr>
      <td>@m.Title</td>
      <td>@m.Director</td>
      <td>@m.Genre</td>
      <td>
        <a asp-action="Details" asp-route-id="@m.Id">Details</a> |
        <a asp-action="Edit" asp-route-id="@m.Id">Edit</a> |
        <a asp-action="Delete" asp-route-id="@m.Id">Delete</a>
      </td>
    </tr>
  }
  </tbody>
</table>
```

Partial form Views/Movies/_MovieForm.cshtml:
```cshtml
@model MovieApp.Models.Movie
<div class="mb-3">
  <label asp-for="Title" class="form-label"></label>
  <input asp-for="Title" class="form-control" />
  <span asp-validation-for="Title" class="text-danger"></span>
</div>
<div class="mb-3">
  <label asp-for="Director" class="form-label"></label>
  <input asp-for="Director" class="form-control" />
</div>
<div class="mb-3">
  <label asp-for="Genre" class="form-label"></label>
  <input asp-for="Genre" class="form-control" />
</div>
<div class="mb-3">
  <label asp-for="ReleaseDate" class="form-label"></label>
  <input asp-for="ReleaseDate" class="form-control" type="date" />
</div>
<button type="submit" class="btn btn-primary">Save</button>
```
Use the partial in Create/Edit to keep DRY.

---

## 11.5) Bootstrap + Navbar — Step-by-step (from 2025-09-08 lecture)

Option A — LibMan (recommended)
```bash
# purge any existing lib folders if you previously copied files by hand
rm -rf wwwroot/lib/bootstrap wwwroot/lib/jquery wwwroot/lib/jquery-validation wwwroot/lib/jquery-validation-unobtrusive 2>/dev/null || true

# initialize LibMan and install exact libraries used by template/teacher
libman init
libman install bootstrap -d wwwroot/lib/bootstrap
libman install jquery -d wwwroot/lib/jquery
libman install jquery-validation -d wwwroot/lib/jquery-validation
libman install jquery-validation-unobtrusive -d wwwroot/lib/jquery-validation-unobtrusive
```

Ensure only the min + map files are referenced by _Layout:
```html
<link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
<script src="~/lib/jquery/dist/jquery.min.js"></script>
<script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
```

Option B — Manual copy (as shown in lecture)
- Remove extra bootstrap files under wwwroot/lib/bootstrap/dist (keep only):
  - css: bootstrap.min.css, bootstrap.min.css.map
  - js: bootstrap.bundle.min.js, bootstrap.bundle.min.js.map
- Keep jquery/jquery.min(.map), jquery-validation (including additional-methods.*), jquery-validation-unobtrusive

Create a navbar partial (Views/Shared/_Navbar.cshtml):
```cshtml
<nav class="navbar navbar-expand-lg bg-body-tertiary mb-3">
  <div class="container">
    <a class="navbar-brand" href="/">BookShelf</a>
    <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarNav"
            aria-controls="navbarNav" aria-expanded="false" aria-label="Toggle navigation">
      <span class="navbar-toggler-icon"></span>
    </button>
    <div class="collapse navbar-collapse" id="navbarNav">
      <ul class="navbar-nav ms-auto">
        <li class="nav-item"><a class="nav-link" href="/">Home</a></li>
        <li class="nav-item"><a class="nav-link" href="/books">All Books</a></li>
        <li class="nav-item"><a class="nav-link" href="/books/new">Add Book</a></li>
      </ul>
    </div>
  </div>
</nav>
```

Update _Layout to use the partial and dark theme:
```cshtml
<!DOCTYPE html>
<html lang="en" data-bs-theme="dark">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>@ViewData["Title"] - BookShelf</title>
  <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
  <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
</head>
<body>
  <header>
    @await Html.PartialAsync("_Navbar")
  </header>
  <div class="container">
    <main role="main" class="pb-3">@RenderBody()</main>
  </div>
  <script src="~/lib/jquery/dist/jquery.min.js"></script>
  <script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
  <script src="~/js/site.js" asp-append-version="true"></script>
  @RenderSection("Scripts", required: false)
</body>
</html>
```

Validation scripts partial (Views/Shared/_ValidationScriptsPartial.cshtml):
```cshtml
<script src="~/lib/jquery-validation/dist/jquery.validate.min.js"></script>
<script src="~/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js"></script>
```

AllBooks table markup (table-striped + aligned action buttons):
```cshtml
<table class="table table-striped">
  <thead>
    <tr>
      <th>Title</th>
      <th>Author</th>
      <th>Genre</th>
      <th>Actions</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td class="align-middle">The Tao of Pooh</td>
      <td class="align-middle">Benjamin Hoff</td>
      <td class="align-middle">Taoism, Philosophy</td>
      <td class="d-flex align-items-center gap-2">
        <a class="btn btn-sm btn-primary" href="#">View</a>
        <a class="btn btn-sm btn-warning" href="#">Edit</a>
        <a class="btn btn-sm btn-danger" href="#">Delete</a>
      </td>
    </tr>
  </tbody>
</table>
```

Tips from lecture
- Use ms-auto on the UL to push nav links to the right; prefer container over container-fluid.
- Add mb-3 to navbar for breathing room below the header.
- Prefer partials for navbar and flash messages; include them in _Layout.
- Restart dotnet watch if assets/layout changes don’t reflect immediately.

---

## 12) LINQ cheat sheet (DbSet vs List)
```csharp
// DB (deferred until enumerated)
var top5 = _context.Movies
    .Where(m => m.Rating >= 8.0)
    .OrderByDescending(m => m.Rating)
    .Take(5)
    .ToList(); // executes SQL

// Async paging
var topPaged = await _context.Movies
    .Where(m => m.Genre == "Action")
    .OrderBy(m => m.Title)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

// Projection (DTO)
var listDto = await _context.Movies
    .Where(m => m.ReleaseDate >= new DateTime(2000,1,1))
    .Select(m => new { m.Id, m.Title, m.Rating })
    .ToListAsync();

// Eager loading
var movieWithRelated = await _context.Movies
    .Include(m => m.RelatedCollection)
    .FirstOrDefaultAsync(m => m.Id == id);

// AsNoTracking for read-only
var readOnly = await _context.Movies.AsNoTracking().ToListAsync();

// Client-side only method (after materialize)
var local = _context.Movies.ToList().Where(m => SomeLocalMethod(m.Title));
```
Key differences:
- IQueryable is translated into SQL. Avoid non-translatable methods until after AsEnumerable()/ToList().
- Use AsNoTracking() for read-only high-volume queries.
- Use projections to reduce transferred columns.

---

## 13) Maps in Views (Leaflet example)
Add Leaflet CDN in _Layout.cshtml:
```html
<link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.3/dist/leaflet.css" />
<script src="https://unpkg.com/leaflet@1.9.3/dist/leaflet.js"></script>
```
In a Details view, show a map using coordinates:
```cshtml
<div id="map" style="height:400px"></div>
@section Scripts {
<script>
  const lat = @Model.Latitude ?? 0;
  const lon = @Model.Longitude ?? 0;
  const map = L.map('map').setView([lat, lon], 13);
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { maxZoom: 19 }).addTo(map);
  L.marker([lat, lon]).addTo(map).bindPopup('@Model.Title').openPopup();
</script>
}
```
Passing many points from Index to JS:
```cshtml
<script>
const points =
@Html.Raw(System.Text.Json.JsonSerializer.Serialize(Model.Select(m => new {
  m.Title, m.Latitude, m.Longitude
})));
</script>
```

---

## 14) Bootstrap (min) usage & small UI tips
- Use CDN for bootstrap.min.css and bootstrap.bundle.min.js for quick start.
- Keep layout responsive using container, row, col-*.
- Use utilities for spacing (e.g., mt-3, p-2).

---

## 14.5) MVC form validation, normalization, and PRG (from lecture)

- In the form view, include a summary and field-level validation helpers:
```cshtml
<div asp-validation-summary="All" class="text-danger"></div>
<span asp-validation-for="Title" class="form-text text-danger"></span>
```
- Normalize input in POST actions before validation:
```csharp
vm.Title = (vm.Title ?? string.Empty).Trim();
vm.Director = (vm.Director ?? string.Empty).Trim();
vm.Genre = (vm.Genre ?? string.Empty).Trim();
```
- If validation fails, return the same view with the same view model:
```csharp
if (!ModelState.IsValid) return View(nameof(Create), vm);
```
- PRG pattern: after a successful POST, redirect to a GET action (Index/Details):
```csharp
_context.Add(entity);
_context.SaveChanges();
return RedirectToAction(nameof(Index));
```
- Range attributes require constants; if you need a dynamic upper bound for year, either:
  - Hard-code a near-future max (e.g., [Range(1200, 2030)]), or
  - Implement IValidatableObject for custom dynamic validation logic.

---

## 15) Testing endpoints
API (curl examples):
```bash
curl https://localhost:5001/api/movies
curl -X POST -H "Content-Type: application/json" -d '{"title":"X"}' https://localhost:5001/api/movies
```
MVC: Manual UI testing by running app.

---

## 16) Production & deployment notes
- Use environment variables/secrets for DB passwords in production.
- Migrations: apply during CI/CD or on deploy.
- Consider connection pooling settings.

---

## 17) Troubleshooting common issues
- ServerVersion.AutoDetect fails: ensure DB reachable, or specify explicit version
  `new MySqlServerVersion(new Version(8,0,32))`.
- Migrations cannot open connection: check connection string, privileges, and firewall.
- Date/time mismatches: prefer UTC for DB storage.
- EF Core translation errors: move non-translatable code client-side after AsEnumerable().

---

## 18) Next steps & resources
- Add authentication (Identity) for protected CRUD.
- Add paging/sorting/filtering.
- Build an SPA (React/Vue) that consumes the API endpoints.
- Explore Repository + Unit of Work pattern.

Quick bookmarks
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```
Async EF methods: `ToListAsync()`, `FindAsync(id)`, `SaveChangesAsync()`

