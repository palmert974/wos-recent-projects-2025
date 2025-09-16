using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheVinylCountdown.Models;
using TheVinylCountdown.ViewModels;

namespace TheVinylCountdown.Controllers;

public class AlbumsController : Controller
{
    private readonly ApplicationContext _context;
    private const string SessionUserId = "userId";

    public AlbumsController(ApplicationContext context)
    {
        _context = context;
    }

    // ASSIGNMENT REQUIREMENT: "Display a List of All Albums"
    // "Create a [HttpGet("/albums")] action method that retrieves all albums from the database
    // and uses .Include() to also get the User data for each album."
    // JAM-MASTER CHALLENGE: "Sortable Albums List" - Accept sort and dir parameters
    [HttpGet("/albums")]
    public async Task<IActionResult> Index(string? sort, string? dir)
    {
        // JAM-MASTER: Default sort should be Album.CreatedAt descending
        sort = sort?.ToLower() ?? "date";
        dir = dir?.ToLower() ?? "desc";

        // Validate sort parameter
        if (!new[] { "title", "artist", "date", "genre", "year" }.Contains(sort))
            sort = "date";

        // Validate direction parameter
        if (!new[] { "asc", "desc" }.Contains(dir))
            dir = "desc";

        // TEACHER METHOD: Using projection like Blurb project for efficiency
        var query = _context.Albums.Include(a => a.User); // Still need Include for User data

        // JAM-MASTER: Apply server-side sorting with LINQ
        var sortedQuery = sort switch
        {
            "title" => dir == "asc"
                ? query.OrderBy(a => a.Title)
                : query.OrderByDescending(a => a.Title),
            "artist" => dir == "asc"
                ? query.OrderBy(a => a.Artist)
                : query.OrderByDescending(a => a.Artist),
            "genre" => dir == "asc"
                ? query.OrderBy(a => a.Genre)
                : query.OrderByDescending(a => a.Genre),
            "year" => dir == "asc"
                ? query.OrderBy(a => a.ReleaseYear)
                : query.OrderByDescending(a => a.ReleaseYear),
            _ => dir == "asc"
                ? query.OrderBy(a => a.CreatedAt)
                : query.OrderByDescending(a => a.CreatedAt),
        };

        var albums = await sortedQuery.ToListAsync();

        // Pass sorting info to view for UI updates
        ViewBag.CurrentSort = sort;
        ViewBag.CurrentDir = dir;
        ViewBag.NextDir = dir == "asc" ? "desc" : "asc"; // Toggle direction

        // ASSIGNMENT REQUIREMENT: "Pass the list of albums to a strongly typed view"
        return View(albums);
    }

    // ASSIGNMENT REQUIREMENT: "Create a [HttpGet] Route for Creating a New Album"
    // "Create a [HttpGet("/albums/new")] action method that returns the new form view."
    [HttpGet("/albums/new")]
    public IActionResult New()
    {
        // Check if user is logged in (authentication check)
        var userId = HttpContext.Session.GetInt32(SessionUserId);
        if (userId == null)
        {
            TempData["Error"] = "Please login to add albums to your wishlist";
            return RedirectToAction("LoginForm", "Account");
        }

        // ASSIGNMENT REQUIREMENT: "Create a form view that is strongly typed to the AlbumFormViewModel"
        return View();
    }

    // ASSIGNMENT REQUIREMENT: "Create a [HttpPost] Route to Create a New Album"
    // "Create a [HttpPost("/albums/new")] action method that handles the form submission."
    [HttpPost("/albums/new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AlbumFormViewModel vm)
    {
        // ASSIGNMENT REQUIREMENT: "Using session data to automatically set the foreign key on a new album"
        // Get the user's ID from session to link the album to the current user
        var userId = HttpContext.Session.GetInt32(SessionUserId);
        if (userId == null)
        {
            TempData["Error"] = "Please login to add albums to your wishlist";
            return RedirectToAction("LoginForm", "Account");
        }

        // ASSIGNMENT REQUIREMENT: "Using the @model directive to bind a form to a ViewModel"
        // Validate the AlbumFormViewModel that was bound from the form
        if (!ModelState.IsValid)
            return View("New", vm);

        // ASSIGNMENT REQUIREMENT: "Inside the action, use the user's Id from the session
        // to automatically set the foreign key for the new Album object."
        var album = new Album
        {
            Title = vm.Title,
            Artist = vm.Artist,
            ReleaseYear = vm.ReleaseYear,
            Genre = vm.Genre,
            UserId = userId.Value, // FOREIGN KEY SET FROM SESSION!
        };

        // ASSIGNMENT REQUIREMENT: "Save the new album to the database and redirect to a main list page."
        _context.Albums.Add(album);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"'{album.Title}' by {album.Artist} has been added to the wishlist!";
        return RedirectToAction("Index"); // Redirect to main albums list
    }

    // JAM-MASTER CHALLENGE (OPTIONAL): "User-Specific List"
    // "Add a new [HttpGet] action method at /my-albums that only displays
    // the albums for the currently logged-in user."
    [HttpGet("/my-albums")]
    public async Task<IActionResult> MyAlbums()
    {
        var userId = HttpContext.Session.GetInt32(SessionUserId);
        if (userId == null)
        {
            TempData["Error"] = "Please login to view your albums";
            return RedirectToAction("LoginForm", "Account");
        }

        // JAM-MASTER CHALLENGE: "You will need to use a where clause in your LINQ query."
        // Filter albums to only show those belonging to the logged-in user
        var albums = await _context
            .Albums.Where(a => a.UserId == userId.Value) // WHERE clause filters by UserId
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        ViewBag.Username = HttpContext.Session.GetString("username");
        return View(albums);
    }
}
