# Backlog Boss — Assignment (Matches 2025-09-08 Teacher Flow)

Recording for reference: https://fathom.video/calls/404070505?timestamp=241.837284

Goal: Build a one-entity, full CRUD MVC app (Games) mirroring the BookShelf demo. Keep it simple, follow PRG, and use local Bootstrap min assets.

1) Scaffold and run
```bash
 dotnet new mvc -n BacklogBoss && cd BacklogBoss && code .
 dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.17
 dotnet add package Pomelo.EntityFrameworkCore.MySql --version 8.0.3
 dotnet tool install --global dotnet-ef || dotnet tool update --global dotnet-ef
 dotnet watch run
```

2) Assets and layout
- Ensure these exist under wwwroot (LibMan or manual copy):
  - lib/bootstrap/dist/css/bootstrap.min.css (+ .map)
  - lib/bootstrap/dist/js/bootstrap.bundle.min.js (+ .map)
  - css/site.css, js/site.js (create if missing)
- Views/Shared/_Layout.cshtml:
  - <html data-bs-theme="dark">
  - <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
  - <script src="~/lib/jquery/dist/jquery.min.js"></script>
  - <script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
  - <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
- Create Views/Shared/_Navbar.cshtml and include it in _Layout (brand: Backlog Boss; links: /games, /games/new)
- Views/_ViewImports.cshtml must include: @addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers

3) Game model and DbContext
- Models/Game.cs: Id, Title, Platform, IsComplete (bool), Notes, CreatedAt (UTC), UpdatedAt (UTC)
- Models/GameContext.cs: DbSet<Game> Games; ctor with DbContextOptions<GameContext>

4) appsettings + Program wiring
- appsettings.json → ConnectionStrings: { "DefaultConnection": "Server=localhost;port=3306;userid=root;password=YOUR_PASSWORD;database=backlogboss_db" }
- Program.cs →
  - var cs = builder.Configuration.GetConnectionString("DefaultConnection");
  - builder.Services.AddDbContext<GameContext>(o => o.UseMySql(cs, new MySqlServerVersion(new Version(8, 0, 32))));
  - app.UseStaticFiles(); (ensure present)

5) Migrations
```bash
 dotnet ef migrations add InitialCreate
 dotnet ef database update
```
If you add fields later, create a new migration and update again.

6) Controller and routes
- Controllers/GameController.cs with [Route("games")]
- Inject GameContext via ctor (private readonly GameContext _context)
- Actions:
  - [HttpGet("")] GamesIndex() → return View("GamesIndex", vm)
  - [HttpGet("new")] NewGameForm() → return View("NewGameForm", new GameViewModel())
  - [HttpPost("create")] CreateGame(GameViewModel vm) → normalize Trim(), validate, map → Game, _context.Add, SaveChanges, RedirectToAction(GamesIndex)

7) ViewModel and validation
- ViewModels/GameViewModel.cs
  - Title/Platform: [Required], [MinLength(2)]
  - Notes: optional; if provided must be at least 5 characters (implemented via IValidatableObject)
  - IsComplete: bool

8) Views
- Views/Game/GamesIndex.cshtml: table.table-striped with Title, Platform, Completed, Actions; badges for Completed; filter bar (All/Completed/Incomplete)
- Views/Game/NewGameForm.cshtml:
  - @model GameViewModel
  - <div asp-validation-summary="All" class="text-danger"></div>
  - Bootstrap form with asp-for inputs; form-switch for IsComplete; text area for Notes; Cancel + Submit buttons

9) Normalize + PRG
- In POST actions, normalize strings (Trim) before ModelState validation
- Return the same view + vm on invalid ModelState; redirect after success (PRG)

Troubleshooting
- Bootstrap not loading → only .map files present? Add bootstrap.min.css and bootstrap.bundle.min.js
- site.css 404 → create wwwroot/css/site.css and reference it in _Layout
- "~" not resolving → ensure @addTagHelper in Views/_ViewImports.cshtml
- HTTPS redirect warning on http profile → harmless for styling

Commands cheat sheet
```bash
 dotnet watch run
 dotnet ef migrations add <Name>
 dotnet ef database update
```

## 💾 Step 3: Create DbContext

### Models/GameContext.cs
```csharp
using Microsoft.EntityFrameworkCore;

namespace BacklogBoss.Models
{
    public class GameContext : DbContext
    {
        public GameContext(DbContextOptions<GameContext> options) : base(options)
        {
        }
        
        public DbSet<Game> Games { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Seed initial data (optional)
            modelBuilder.Entity<Game>().HasData(
                new Game
                {
                    Id = 1,
                    Title = "The Legend of Zelda: Breath of the Wild",
                    Platform = "Nintendo Switch",
                    IsComplete = false,
                    Notes = "Need to finish the DLC content",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Game
                {
                    Id = 2,
                    Title = "God of War Ragnarök",
                    Platform = "PlayStation 5",
                    IsComplete = true,
                    Notes = "Amazing story! Completed 100%",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
        }
    }
}
```

---

## 📦 Step 4: Create ViewModels

### ViewModels/GameFormViewModel.cs
```csharp
using System.ComponentModel.DataAnnotations;

namespace BacklogBoss.ViewModels
{
    public class GameFormViewModel
    {
        // Null on Create; populated on Edit
        public int? Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MinLength(2, ErrorMessage = "Title must be at least 2 characters")]
        [Display(Name = "Game Title")]
        public string Title { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Platform is required")]
        [MinLength(2, ErrorMessage = "Platform must be at least 2 characters")]
        [Display(Name = "Gaming Platform")]
        public string Platform { get; set; } = string.Empty;
        
        [Display(Name = "Mark as Completed")]
        public bool IsComplete { get; set; } = false;
        
        [Required(ErrorMessage = "Notes are required")]
        [MinLength(5, ErrorMessage = "Notes must be at least 5 characters")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Your Notes")]
        public string Notes { get; set; } = string.Empty;
    }
}
```

### ViewModels/GameRowViewModel.cs
```csharp
namespace BacklogBoss.ViewModels
{
    public class GameRowViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public bool IsComplete { get; set; }
    }
}
```

### ViewModels/GameViewModel.cs
```csharp
namespace BacklogBoss.ViewModels
{
    public class GameViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public bool IsComplete { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
```

### ViewModels/ConfirmDeleteViewModel.cs
```csharp
namespace BacklogBoss.ViewModels
{
    public class ConfirmDeleteViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}
```

---

## 🧩 Step 4.5: Layout, Partials, and Navbar

Create a shared navbar partial and a flash messages partial, then render them from the main layout.

### Views/Shared/_Navbar.cshtml (match BookShelf styling)
```html
<nav class="navbar navbar-expand-lg bg-body-tertiary mb-3">
  <div class="container">
    <a class="navbar-brand" asp-area="" asp-controller="Home" asp-action="Index">Backlog Boss</a>
    <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarNav"
      aria-controls="navbarNav" aria-expanded="false" aria-label="Toggle navigation">
      <span class="navbar-toggler-icon"></span>
    </button>
    <div class="collapse navbar-collapse" id="navbarNav">
      <ul class="navbar-nav ms-auto">
        <li class="nav-item">
          <a class="nav-link" asp-controller="Home" asp-action="Index">Home</a>
        </li>
        <li class="nav-item">
<a class="nav-link" asp-controller="Game" asp-action="GamesIndex">All Games</a>
        </li>
        <li class="nav-item">
<a class="nav-link" asp-controller="Game" asp-action="NewGameForm">Add Game</a>
        </li>
      </ul>
    </div>
  </div>
</nav>
```

### Views/Shared/_FlashMessages.cshtml
```html
@if (TempData["Success"] != null)
{
  <div class="alert alert-success alert-dismissible fade show mt-3" role="alert">
    @TempData["Success"]
    <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
  </div>
}
@if (TempData["Error"] != null)
{
  <div class="alert alert-danger alert-dismissible fade show mt-3" role="alert">
    @TempData["Error"]
    <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
  </div>
}
```

### Views/Shared/_Layout.cshtml (match BookShelf head/body/scripts)
```html
<!DOCTYPE html>
<html lang="en" data-bs-theme="dark">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>@ViewData["Title"] - Backlog Boss</title>
  <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
  <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
  <link rel="stylesheet" href="~/BacklogBoss.styles.css" asp-append-version="true" />
</head>
<body>
  <header>
    @await Html.PartialAsync("_Navbar")
  </header>
  <div class="container">
    <main role="main" class="pb-3">
      @RenderBody()
    </main>
  </div>
  @await Html.PartialAsync("_FlashMessages")
  <footer class="border-top footer text-muted">
    <div class="container">
      &copy; 2025 - Backlog Boss - <a asp-area="" asp-controller="Home" asp-action="Privacy">Privacy</a>
    </div>
  </footer>
  <script src="~/lib/jquery/dist/jquery.min.js"></script>
  <script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
  <script src="~/js/site.js" asp-append-version="true"></script>
  @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

Tip: Ensure the local library paths under wwwroot/lib exist (use LibMan commands in Step 1B) so this matches BookShelf exactly.

## ⚙️ Step 5: Configure Application

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;port=3306;userid=root;password=rootroot;database=backlogboss_db"
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

### Program.cs
```csharp
using BacklogBoss.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();

// Add DbContext with MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<GameContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Add Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

---

## 🎮 Step 6: Create GamesController

### Controllers/GameController.cs
```csharp
using BacklogBoss.Models;
using BacklogBoss.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BacklogBoss.Controllers;

[Route("games")]
public class GameController : Controller
{
    private readonly GameContext _context;

    public GameController(GameContext context)
    {
        _context = context;
    }

    // GET /games
    [HttpGet("")]
    public async Task<IActionResult> AllGames()
    {
        var games = await _context.Games
            .AsNoTracking()
            .Select(g => new GameRowViewModel
            {
                Id = g.Id,
                Title = g.Title,
                Platform = g.Platform,
                IsComplete = g.IsComplete,
            })
            .ToListAsync();
        return View("AllGames", games);
    }

    // GET /games/new
    [HttpGet("new")]
    public IActionResult NewGame()
    {
        var vm = new GameFormViewModel();
        return View("NewGame", vm);
    }

    // POST /games/create
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public IActionResult CreateGame(GameFormViewModel vm)
    {
        // normalize input
        vm.Title = (vm.Title ?? string.Empty).Trim();
        vm.Platform = (vm.Platform ?? string.Empty).Trim();
        vm.Notes = (vm.Notes ?? string.Empty).Trim();

        if (!ModelState.IsValid)
        {
            return View(nameof(NewGame), vm);
        }

        var newGame = new Game
        {
            Title = vm.Title,
            Platform = vm.Platform,
            IsComplete = vm.IsComplete,
            Notes = vm.Notes,
        };
        _context.Games.Add(newGame);
        _context.SaveChanges();
        return RedirectToAction(nameof(AllGames));
    }

    // GET /games/{id}
    [HttpGet("{id}")]
    public IActionResult GameDetails(int id)
    {
        var maybeGame = _context.Games.AsNoTracking().FirstOrDefault(g => g.Id == id);
        if (maybeGame is null) return NotFound();

        var vm = new GameViewModel
        {
            Id = maybeGame.Id,
            Title = maybeGame.Title,
            Platform = maybeGame.Platform,
            IsComplete = maybeGame.IsComplete,
            Notes = maybeGame.Notes,
        };
        return View(vm);
    }

    // GET /games/{id}/edit
    [HttpGet("{id}/edit")]
    public IActionResult EditGame(int id)
    {
        var maybeGame = _context.Games.FirstOrDefault(g => g.Id == id);
        if (maybeGame is null) return NotFound();

        var vm = new GameFormViewModel
        {
            Id = maybeGame.Id,
            Title = maybeGame.Title,
            Platform = maybeGame.Platform,
            IsComplete = maybeGame.IsComplete,
            Notes = maybeGame.Notes,
        };
        return View(vm);
    }

    // POST /games/{id}/update
    [HttpPost("{id}/update")]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateGame(int id, GameFormViewModel vm)
    {
        // normalize input
        vm.Title = (vm.Title ?? string.Empty).Trim();
        vm.Platform = (vm.Platform ?? string.Empty).Trim();
        vm.Notes = (vm.Notes ?? string.Empty).Trim();

        if (!ModelState.IsValid)
        {
            return View(nameof(EditGame), vm);
        }
        if (vm.Id is null || id != vm.Id.Value)
        {
            return BadRequest();
        }

        var maybeGame = _context.Games.FirstOrDefault(g => g.Id == id);
        if (maybeGame is null) return NotFound();

        maybeGame.Title = vm.Title;
        maybeGame.Platform = vm.Platform;
        maybeGame.IsComplete = vm.IsComplete;
        maybeGame.Notes = vm.Notes;
        maybeGame.UpdatedAt = DateTime.UtcNow;

        _context.SaveChanges();
        return RedirectToAction(nameof(GameDetails), new { id });
    }

    // GET /games/{id}/delete
    [HttpGet("{id}/delete")]
    public IActionResult ConfirmDelete(int id)
    {
        var maybeGame = _context.Games.FirstOrDefault(g => g.Id == id);
        if (maybeGame is null) return NotFound();

        var vm = new ConfirmDeleteViewModel { Id = maybeGame.Id, Title = maybeGame.Title };
        return View(vm);
    }

    // POST /games/{id}/destroy
    [HttpPost("{id}/destroy")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteGame(int id, ConfirmDeleteViewModel vm)
    {
        if (id != vm.Id) return BadRequest();

        var maybeGame = _context.Games.FirstOrDefault(g => g.Id == id);
        if (maybeGame is null) return NotFound();

        _context.Games.Remove(maybeGame);
        _context.SaveChanges();
        return RedirectToAction(nameof(AllGames));
    }
}
```

---

## 🗺️ Routes Overview
- Base route: [Route("games")] on GameController
- GET `/games` → `GameController.GamesIndex`
- GET `/games/new` → `GameController.NewGameForm`
- POST `/games/create` → `GameController.CreateGame`
- GET `/games/{id}` → `GameController.GameDetails`
- GET `/games/{id}/edit` → `GameController.EditGameForm`
- POST `/games/{id}/update` → `GameController.UpdateGame`
- GET `/games/{id}/delete` → `GameController.ConfirmDelete`
- POST `/games/{id}/delete` → `GameController.DeleteGame`

---

## 🎨 Step 7: Create Views

### Views/Game/AllGames.cshtml
```html
@model List<BacklogBoss.ViewModels.GameRowViewModel>
@{
    ViewData["Title"] = "All Games";
    var games = Model;
}

<h1 class="display-4">All Games</h1>
<table class="table table-striped">
    <thead>
        <tr>
            <th>Title</th>
            <th>Platform</th>
            <th>Actions</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var game in games)
        {
            <tr>
                <td class="align-middle">@game.Title</td>
                <td class="align-middle">@game.Platform</td>
                <td class="d-flex align-items-center gap-2">
                    <a class="btn btn-sm btn-primary" href="/games/@game.Id">View</a>
                    <a class="btn btn-sm btn-warning" href="/games/@game.Id/edit">Edit</a>
                    <a class="btn btn-sm btn-danger" href="/games/@game.Id/delete">Delete</a>
                </td>
            </tr>
        }
    </tbody>
</table>
```

### Views/Game/NewGame.cshtml
```html
@model BacklogBoss.ViewModels.GameFormViewModel
@{
    ViewData["Title"] = "Add a New Game";
}

<h1 class="display-4">Add a New Game</h1>

<div asp-validation-summary="All" class="text-danger"></div>

<div class="card shadow">
    <form asp-action="CreateGame" asp-controller="Game" method="post">
        <div class="card-body">
            <div class="row mb-3">
                <div class="col">
                    <label asp-for="Title" class="form-label">Title:</label>
                    <input asp-for="Title" class="form-control">
                    <span asp-validation-for="Title" class="form-text text-danger"></span>
                </div>
                <div class="col">
                    <label asp-for="Platform" class="form-label">Platform:</label>
                    <input asp-for="Platform" class="form-control">
                    <span asp-validation-for="Platform" class="form-text text-danger"></span>
                </div>
            </div>
            <div class="mb-3">
                <div class="form-check form-switch">
                    <input class="form-check-input" type="checkbox" role="switch" asp-for="IsComplete">
                    <label class="form-check-label" asp-for="IsComplete">Completed?</label>
                    <span asp-validation-for="IsComplete" class="form-text text-danger"></span>
                </div>
            </div>
            <div class="mb-3">
                <label asp-for="Notes" class="form-label">Notes:</label>
                <textarea asp-for="Notes" class="form-control" style="height: 7rem;"></textarea>
                <span asp-validation-for="Notes" class="form-text text-danger"></span>
            </div>
            <div class="text-end">
                <button type="submit" class="btn btn-primary">Add game</button>
            </div>
        </div>
    </form>
</div>
```

### Views/Game/GameDetails.cshtml
```html
@model BacklogBoss.ViewModels.GameViewModel
@{
    ViewData["Title"] = "Game Details";
    var game = Model;
}

<h1 class="display-4">Game Details</h1>

<div class="card shadow">
    <div class="card-body">
        <h5 class="card-title">@game.Title</h5>
        <p>
            <strong>Platform: </strong> @game.Platform<br />
            <strong>Completed: </strong> @(game.IsComplete ? "yes" : "no")<br />
            <strong>Notes: </strong><br /> @game.Notes
        </p>
    </div>
    <div class="card-footer text-end">
        <a href="/games/@game.Id/edit" class="btn btn-sm btn-warning me-2">Edit</a>
        <a href="/games/@game.Id/delete" class="btn btn-sm btn-danger">Delete</a>
    </div>
</div>
```

### Views/Game/EditGame.cshtml
```html
@model BacklogBoss.ViewModels.GameFormViewModel
@{
    ViewData["Title"] = "Edit Game";
}

<h1 class="display-4">Edit Game</h1>

<div asp-validation-summary="All" class="text-danger"></div>

<div class="card shadow">
    <form asp-action="UpdateGame" asp-controller="Game" asp-route-id="@Model.Id" method="post">
        <input type="hidden" asp-for="Id" value="@Model.Id">
        <div class="card-body">
            <div class="row mb-3">
                <div class="col">
                    <label asp-for="Title" class="form-label">Title:</label>
                    <input asp-for="Title" class="form-control">
                    <span asp-validation-for="Title" class="form-text text-danger"></span>
                </div>
                <div class="col">
                    <label asp-for="Platform" class="form-label">Platform:</label>
                    <input asp-for="Platform" class="form-control">
                    <span asp-validation-for="Platform" class="form-text text-danger"></span>
                </div>
            </div>
            <div class="mb-3">
                <div class="form-check form-switch">
                    <input class="form-check-input" type="checkbox" role="switch" asp-for="IsComplete">
                    <label class="form-check-label" asp-for="IsComplete">Completed?</label>
                    <span asp-validation-for="IsComplete" class="form-text text-danger"></span>
                </div>
            </div>
            <div class="mb-3">
                <label asp-for="Notes" class="form-label">Notes:</label>
                <textarea asp-for="Notes" class="form-control" style="height: 7rem;"></textarea>
                <span asp-validation-for="Notes" class="form-text text-danger"></span>
            </div>
            <div class="text-end">
                <button type="submit" class="btn btn-primary">Edit game</button>
            </div>
        </div>
    </form>
</div>
```

### Views/Game/ConfirmDelete.cshtml
```html
@model BacklogBoss.ViewModels.ConfirmDeleteViewModel
@{
    ViewData["Title"] = "Confirm Delete";
    var game = Model;
}

<h1 class="display-4">Confirm Delete</h1>

<p>Are you sure you want to delete @game.Title?</p>
<p>This action cannot be undone.</p>

<form asp-action="DeleteGame" asp-controller="Game" asp-route-id="@game.Id" method="post">
    <input type="hidden" asp-for="Id" value="@game.Id">
    <button type="submit" class="btn btn-danger">Delete Game</button>
</form>
```

---

## 🔗 Step 8: Update Layout Navigation

### Views/Shared/_Layout.cshtml (Add to navbar)
```html
<ul class="navbar-nav flex-grow-1">
    <li class="nav-item">
        <a class="nav-link text-dark" asp-area="" asp-controller="Home" asp-action="Index">Home</a>
    </li>
    <li class="nav-item">
<a class="nav-link text-dark" asp-controller="Game" asp-action="GamesIndex">
            <i class="bi bi-controller"></i> All Games
        </a>
    </li>
    <li class="nav-item">
<a class="nav-link text-dark" asp-controller="Game" asp-action="NewGameForm">
            <i class="bi bi-plus-circle"></i> Add Game
        </a>
    </li>
    <li class="nav-item">
        <a class="nav-link text-dark" asp-area="" asp-controller="Home" asp-action="Privacy">Privacy</a>
    </li>
</ul>
```

---

## 🗃️ Step 9: Run Migrations

```bash
# Create initial migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update

# Verify migration
dotnet ef migrations list
```

---

## ▶️ Step 10: Run and Test

```bash
# Run the application
dotnet run

# Or with hot reload
dotnet watch run
```

Navigate to: http://localhost:5062/games

---

## ✅ Testing Checklist

### Create (C)
- [ ] Navigate to /games/new
- [ ] Fill out form with valid data
- [ ] Submit and verify redirect to index
- [ ] Verify new game appears in list
- [ ] Test validation by submitting empty form
- [ ] Test validation with short values (< minimum length)

### Read All (R)
- [ ] Navigate to /games
- [ ] Verify all games display in table
- [ ] Check statistics cards show correct counts
- [ ] Verify status badges display correctly

### Read One (R)
- [ ] Click "View" on any game
- [ ] Verify all details display correctly
- [ ] Check formatted dates appear properly

### Update (U)
- [ ] Click "Edit" on any game
- [ ] Verify form pre-populates with existing data
- [ ] Change values and submit
- [ ] Verify redirect to details page
- [ ] Confirm changes were saved

### Delete (D)
- [ ] Click "Delete" on any game
- [ ] Verify confirmation page shows correct game
- [ ] Click confirm and verify redirect to index
- [ ] Confirm game no longer appears in list

### PRG Pattern
- [ ] After creating a game, refresh the page
- [ ] Verify no duplicate submission warning
- [ ] Check same for update and delete operations

---

## 🎯 Bonus Features to Add

1. **Search Functionality**
   - Add search box to filter games by title or platform

2. **Sorting**
   - Allow sorting by title, platform, or date added

3. **Progress Tracking**
   - Add percentage complete field for games in progress

4. **Categories/Tags**
   - Add genre tags to better organize games

5. **Priority System**
   - Mark games as high/medium/low priority to play next

6. **Play Time Tracking**
   - Track hours played for each game

---

## 📚 Key Concepts Demonstrated

✅ **Full CRUD Operations**
- Create, Read (All & One), Update, Delete

✅ **ViewModels Pattern**
- Separation of concerns between UI and domain

✅ **Data Validation**
- DataAnnotations attributes
- ModelState.IsValid checking
- Client and server-side validation

✅ **Entity Framework Core**
- DbContext configuration
- Migrations
- Async database operations

✅ **PRG Pattern**
- Post-Redirect-Get for safe form submissions

✅ **MVC Architecture**
- Models, Views, Controllers working together
- Strongly-typed views
- Tag helpers for forms

---

## 🚨 Common Issues & Solutions

### MySQL Connection Error
```bash
# Verify MySQL is running
mysql -u root -p

# Create database manually if needed
CREATE DATABASE backlogboss_db;
```

### Migration Errors
```bash
# Remove and recreate migrations
rm -rf Migrations/
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Port Already in Use
Change port in Properties/launchSettings.json

---

## 📦 Submission Requirements

1. Zip your entire BacklogBoss project folder
2. Exclude bin/ and obj/ directories
3. Include your Migrations folder
4. Ensure appsettings.json has generic connection string

```bash
# Create submission zip (excluding build artifacts)
zip -r BacklogBoss.zip BacklogBoss/ -x "*/bin/*" -x "*/obj/*"
```

---

## 🧠 Week 4: Full-Stack MVC Notes (Global Setup + CRUD Pages)

Note: This section mirrors the teacher’s BookShelf naming and structure: singular controller with [Route("games")], AllGames/NewGame/EditGame view names, and a simple table-based index.

These notes mirror the “Books” style and will be used throughout Week 4. They are written for Backlog Boss (Games), but if your course notes say “MoviesApp” (Movie/MovieContext, MoviesController, /movies), simply substitute:
- Movie → Game
- MovieContext → GameContext
- MoviesController → GamesController
- /movies → /games

### Global Setup: A Full-Featured CRUD Web App
By the end of setup, you have a .NET MVC app that is:
- Connected to MySQL via EF Core
- Configured with a Game model and GameContext
- Ready for full CRUD through the web UI

Steps
1) Create project
```bash
# Foundation project for the week
 dotnet new mvc -n BacklogBoss
 cd BacklogBoss
 code .
```

2) Install packages
```bash
# EF Core design-time tools
 dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.17
# MySQL provider
 dotnet add package Pomelo.EntityFrameworkCore.MySql --version 8.0.3
```

3) Model: Game
```csharp path=null start=null
using System.ComponentModel.DataAnnotations;
namespace BacklogBoss.Models;
public class Game
{
  [Key] public int Id { get; set; }
  [Required] public string Title { get; set; } = string.Empty;
  [Required] public string Platform { get; set; } = string.Empty;
  [Required] public string Notes { get; set; } = string.Empty;
  public bool IsComplete { get; set; } = false;
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

4) DbContext: GameContext
```csharp path=null start=null
using Microsoft.EntityFrameworkCore;
namespace BacklogBoss.Models;
public class GameContext : DbContext
{
  public DbSet<Game> Games { get; set; }
  public GameContext(DbContextOptions<GameContext> options) : base(options) { }
}
```

5) Connection string (appsettings.json)
```json path=null start=null
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;port=3306;userid=root;password=your_password;database=backlogboss_db"
  }
}
```

6) Program.cs configuration
```csharp path=null start=null
using Microsoft.EntityFrameworkCore;
using BacklogBoss.Models;
var builder = WebApplication.CreateBuilder(args);
var cs = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddControllersWithViews();
builder.Services.AddSession();
builder.Services.AddDbContext<GameContext>(o => o.UseMySql(cs, ServerVersion.AutoDetect(cs)));
var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Home/Error");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseSession();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
```

7) Create DB with migrations
```bash
 dotnet ef migrations add FirstMigration
 dotnet ef database update
```

Note on assets & navbar
- Match BookShelf exactly: install Bootstrap + jQuery libs with LibMan and include them in Views/Shared/_Layout.cshtml.
- Use the BookShelf-style navbar (bg-body-tertiary, ms-auto, toggler) in Views/Shared/_Navbar.cshtml.

---

### CRUD Workflow (Pages by chapter)

1) Reading All Rows (Index View)
- Controller action
```csharp path=null start=null
// GET /games
[Route("games")]
public class GameController : Controller
{
  private readonly GameContext _context;
  public GameController(GameContext context) { _context = context; }

  [HttpGet("")]
  public IActionResult AllGames()
  {
    var rows = _context.Games.AsNoTracking()
      .Select(g => new GameRowViewModel { Id=g.Id, Title=g.Title, Platform=g.Platform, IsComplete=g.IsComplete })
      .ToList();
    return View(rows); // Views/Game/AllGames.cshtml
  }
}
```
- View model
```csharp path=null start=null
public class GameRowViewModel
{
  public int Id { get; set; }
  public string Title { get; set; } = "";
  public string Platform { get; set; } = "";
  public bool IsComplete { get; set; }
}
```
- View (Views/Game/AllGames.cshtml)
```cshtml path=null start=null
@model List<BacklogBoss.ViewModels.GameRowViewModel>
<h1 class="display-4">All Games</h1>
<table class="table table-striped">
  <thead><tr><th>Title</th><th>Platform</th><th>Actions</th></tr></thead>
  <tbody>
    @foreach (var g in Model){
      <tr>
        <td class="align-middle">@g.Title</td>
        <td class="align-middle">@g.Platform</td>
        <td class="d-flex align-items-center gap-2">
          <a class="btn btn-sm btn-primary" href="/games/@g.Id">View</a>
          <a class="btn btn-sm btn-warning" href="/games/@g.Id/edit">Edit</a>
          <a class="btn btn-sm btn-danger" href="/games/@g.Id/delete">Delete</a>
        </td>
      </tr>
    }
  </tbody>
</table>
```

2) GET Route for Create Form & Building the Create Form
- Controller
```csharp path=null start=null
[HttpGet("new")]
public IActionResult NewGame() => View(new GameFormViewModel());
```
- View model
```csharp path=null start=null
public class GameFormViewModel
{
  public int? Id { get; set; }
  [Required, MinLength(2)] public string Title { get; set; } = string.Empty;
  [Required, MinLength(2)] public string Platform { get; set; } = string.Empty;
  [Required, MinLength(5)] public string Notes { get; set; } = string.Empty;
  public bool IsComplete { get; set; }
}
```
- View (Views/Game/NewGame.cshtml)
```cshtml path=null start=null
@model BacklogBoss.ViewModels.GameFormViewModel
<h1 class="display-4">Add a New Game</h1>
<div asp-validation-summary="All" class="text-danger"></div>
<div class="card shadow">
  <form asp-controller="Game" asp-action="CreateGame" method="post">
    <div class="card-body">
      <div class="row mb-3">
        <div class="col"><label asp-for="Title" class="form-label">Title:</label><input asp-for="Title" class="form-control"><span asp-validation-for="Title" class="form-text text-danger"></span></div>
        <div class="col"><label asp-for="Platform" class="form-label">Platform:</label><input asp-for="Platform" class="form-control"><span asp-validation-for="Platform" class="form-text text-danger"></span></div>
      </div>
      <div class="mb-3">
        <div class="form-check form-switch">
          <input class="form-check-input" type="checkbox" role="switch" asp-for="IsComplete">
          <label class="form-check-label" asp-for="IsComplete">Completed?</label>
        </div>
      </div>
      <div class="mb-3"><label asp-for="Notes" class="form-label">Notes:</label><textarea asp-for="Notes" class="form-control" style="height:7rem;"></textarea><span asp-validation-for="Notes" class="form-text text-danger"></span></div>
      <div class="text-end"><button type="submit" class="btn btn-primary">Add game</button></div>
    </div>
  </form>
</div>
```

3) POST Route for Create & Validation (PRG)
```csharp path=null start=null
[HttpPost("create")]
[ValidateAntiForgeryToken]
public IActionResult CreateGame(GameFormViewModel vm, [FromServices] GameContext _context)
{
  vm.Title = (vm.Title ?? "").Trim();
  vm.Platform = (vm.Platform ?? "").Trim();
  vm.Notes = (vm.Notes ?? "").Trim();
  if (!ModelState.IsValid) return View(nameof(NewGame), vm);
  var entity = new Game{ Title = vm.Title, Platform = vm.Platform, Notes = vm.Notes, IsComplete = vm.IsComplete };
  _context.Games.Add(entity);
  _context.SaveChanges();
  return RedirectToAction(nameof(AllGames));
}
```

4) Read One Row (Details View)
```csharp path=null start=null
[HttpGet("{id}")]
public IActionResult GameDetails(int id, [FromServices] GameContext _context)
{
  var game = _context.Games.AsNoTracking().FirstOrDefault(g => g.Id == id);
  if (game == null) return NotFound();
  var vm = new GameViewModel{ Id=game.Id, Title=game.Title, Platform=game.Platform, IsComplete=game.IsComplete, Notes=game.Notes };
  return View(vm);
}
```
```cshtml path=null start=null
@model BacklogBoss.ViewModels.GameViewModel
<h1 class="display-4">Game Details</h1>
<div class="card shadow"><div class="card-body">
  <h5 class="card-title">@Model.Title</h5>
  <p><strong>Platform:</strong> @Model.Platform<br />
  <strong>Completed:</strong> @(Model.IsComplete?"yes":"no")<br />
  <strong>Notes:</strong><br /> @Model.Notes</p>
</div><div class="card-footer text-end">
  <a href="/games/@Model.Id/edit" class="btn btn-sm btn-warning me-2">Edit</a>
  <a href="/games/@Model.Id/delete" class="btn btn-sm btn-danger">Delete</a>
</div></div>
```

5) GET Route for Edit Form & Building the Edit Form
```csharp path=null start=null
[HttpGet("{id}/edit")]
public IActionResult EditGame(int id, [FromServices] GameContext _context)
{
  var game = _context.Games.FirstOrDefault(g => g.Id == id);
  if (game == null) return NotFound();
  var vm = new GameFormViewModel{ Id=game.Id, Title=game.Title, Platform=game.Platform, Notes=game.Notes, IsComplete=game.IsComplete };
  return View(vm);
}
```
```cshtml path=null start=null
@model BacklogBoss.ViewModels.GameFormViewModel
<h1 class="display-4">Edit Game</h1>
<div asp-validation-summary="All" class="text-danger"></div>
<div class="card shadow">
  <form asp-controller="Game" asp-action="UpdateGame" asp-route-id="@Model.Id" method="post">
    <input type="hidden" asp-for="Id" />
    <div class="card-body">
      <div class="row mb-3">
        <div class="col"><label asp-for="Title" class="form-label">Title:</label><input asp-for="Title" class="form-control"><span asp-validation-for="Title" class="form-text text-danger"></span></div>
        <div class="col"><label asp-for="Platform" class="form-label">Platform:</label><input asp-for="Platform" class="form-control"><span asp-validation-for="Platform" class="form-text text-danger"></span></div>
      </div>
      <div class="mb-3"><div class="form-check form-switch"><input class="form-check-input" type="checkbox" role="switch" asp-for="IsComplete"><label class="form-check-label" asp-for="IsComplete">Completed?</label></div></div>
      <div class="mb-3"><label asp-for="Notes" class="form-label">Notes:</label><textarea asp-for="Notes" class="form-control" style="height:7rem;"></textarea><span asp-validation-for="Notes" class="form-text text-danger"></span></div>
      <div class="text-end"><button class="btn btn-primary" type="submit">Save Changes</button></div>
    </div>
  </form>
</div>
```

6) POST Route for Edit & Validation
```csharp path=null start=null
[HttpPost("{id}/update")]
[ValidateAntiForgeryToken]
public IActionResult UpdateGame(int id, GameFormViewModel vm, [FromServices] GameContext _context)
{
  vm.Title = (vm.Title ?? "").Trim();
  vm.Platform = (vm.Platform ?? "").Trim();
  vm.Notes = (vm.Notes ?? "").Trim();
  if (vm.Id is null || vm.Id.Value != id) return BadRequest();
  if (!ModelState.IsValid) return View(nameof(EditGame), vm);
  var game = _context.Games.Find(id);
  if (game == null) return NotFound();
  game.Title = vm.Title;
  game.Platform = vm.Platform;
  game.Notes = vm.Notes;
  game.IsComplete = vm.IsComplete;
  game.UpdatedAt = DateTime.UtcNow;
  _context.SaveChanges();
  return RedirectToAction(nameof(GameDetails), new { id = game.Id });
}
```

7) GET Delete Confirmation & POST Delete
```csharp path=null start=null
[HttpGet("{id}/delete")]
public IActionResult ConfirmDelete(int id, [FromServices] GameContext _context)
{
  var maybeGame = _context.Games.FirstOrDefault(g => g.Id == id);
  if (maybeGame is null) return NotFound();
  var vm = new ConfirmDeleteViewModel { Id = maybeGame.Id, Title = maybeGame.Title };
  return View(vm);
}
```
```cshtml path=null start=null
@model BacklogBoss.ViewModels.ConfirmDeleteViewModel
<h1>Delete @Model.Title?</h1>
<form asp-controller="Game" asp-action="DeleteGame" asp-route-id="@Model.Id" method="post">
  <input type="hidden" asp-for="Id" />
  <a href="/games" class="btn btn-outline-danger">Cancel</a>
  <button type="submit" class="btn btn-danger">Delete</button>
</form>
```
```csharp path=null start=null
[HttpPost("{id}/destroy")]
[ValidateAntiForgeryToken]
public IActionResult DeleteGame(int id, BacklogBoss.ViewModels.ConfirmDeleteViewModel vm, [FromServices] GameContext _context)
{
  if (id != vm.Id) return BadRequest();
  var game = _context.Games.Find(id);
  if (game is null) return NotFound();
  _context.Games.Remove(game);
  _context.SaveChanges();
  return RedirectToAction(nameof(AllGames));
}
```

PRG reminder
- Always redirect after successful POST (Create/Update/Delete) to prevent duplicate submissions on refresh.

Navbar links
- Add “All Games” (/games) and “Add Game” (/games/new) to Views/Shared/_Navbar.cshtml so pages are reachable from anywhere.

---

**Good luck with your Backlog Boss project! 🎮**
