# The Rewind - Complete Build Guide

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

### Step 1: Create Project & Install Packages

```bash
# Create and enter project
cd ~/2025-wos/week-10-full-stack-mvc
dotnet new mvc -n TheRewind
cd TheRewind

# Install required packages
dotnet add package Pomelo.EntityFrameworkCore.MySql
dotnet add package BCrypt.Net-Next
dotnet add package Microsoft.EntityFrameworkCore.Design

# Create extra folders
mkdir Services ViewModels

# Open in VS Code
code .
```

### Step 2: Bootstrap Setup

Create `libman.json` in project root:

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

Run:
```bash
libman restore
```

### Step 3: Test Project Runs

```bash
dotnet build
dotnet run
```

Open browser to the URL shown. Press `Ctrl+C` to stop.

---

## Part B: Database Setup

### Step 4: Configure Connection String

Add to `appsettings.json`:

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

### Project Structure

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
    "DefaultConnection": "Server=localhost;Database=therewinddb;User=root;Password=rootroot;"
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
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
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

**Troubleshooting**: 
- If "dotnet ef not found": `dotnet tool install --global dotnet-ef`
- If "Cannot connect to MySQL": Check MySQL is running and password correct

---

## Authentication System

### Step 1: Create Password Service

Create `Services` folder in project root:

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

Create `ViewModels` folder in project root:

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
        if (HttpContext.Session.GetInt32("userId") != null)
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
        HttpContext.Session.SetInt32("userId", user.Id);
        HttpContext.Session.SetString("username", user.Username);

        _logger.LogInformation("New user registered: {Username}", user.Username);
        
        TempData["Success"] = "Registration successful! Welcome to The Rewind!";
        return RedirectToAction("Index", "Movies");
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
// Redirect if already logged in
        if (HttpContext.Session.GetInt32("userId") != null)
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
        HttpContext.Session.SetInt32("userId", user.Id);
        HttpContext.Session.SetString("username", user.Username);

        _logger.LogInformation("User logged in: {Username}", user.Username);
        
        TempData["Success"] = $"Welcome back, {user.Username}!";
        return RedirectToAction("Index", "Movies");
    }

    [HttpGet("logout")]
    public IActionResult ConfirmLogout()
    {
if (HttpContext.Session.GetInt32("userId") == null)
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
if (HttpContext.Session.GetInt32("userId") is not int userId)
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
if (HttpContext.Session.GetInt32("userId") == null)
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
if (HttpContext.Session.GetInt32("userId") is not int userId)
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
if (HttpContext.Session.GetInt32("userId") == null)
        {
            return RedirectToAction("Login", "Account");
        }

        return View(new MovieFormViewModel());
    }

    [HttpPost("movies/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MovieFormViewModel model)
    {
if (HttpContext.Session.GetInt32("userId") is not int userId)
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
if (HttpContext.Session.GetInt32("userId") is not int userId)
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
if (HttpContext.Session.GetInt32("userId") is not int userId)
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
if (HttpContext.Session.GetInt32("userId") is not int userId)
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
if (HttpContext.Session.GetInt32("userId") is not int userId)
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
@if (Context.Session.GetInt32("userId") != null)
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
@if (Context.Session.GetInt32("userId") != null)
                {
                    <li class="nav-item dropdown">
                        <a class="nav-link dropdown-toggle" href="#" id="userDropdown" role="button" 
                           data-bs-toggle="dropdown" aria-expanded="false">
<i class="bi bi-person-circle"></i> @Context.Session.GetString("username")
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
if (HttpContext.Session.GetInt32("userId") != null)
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