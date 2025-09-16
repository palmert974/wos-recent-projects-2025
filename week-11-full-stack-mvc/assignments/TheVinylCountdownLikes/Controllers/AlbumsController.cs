using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheVinylCountdownLikes.Models;
using TheVinylCountdownLikes.ViewModels;

namespace TheVinylCountdownLikes.Controllers;

public class AlbumsController : Controller
{
    private readonly ApplicationContext _context;
    private const string SessionUserId = "userId";

    public AlbumsController(ApplicationContext context)
    {
        _context = context;
    }

    // Display a list of all albums with like counts and whether the current user liked each album
    [HttpGet("/albums")]
    public async Task<IActionResult> Index(string? sort, string? dir)
    {
        sort = sort?.ToLower() ?? "date";
        dir = dir?.ToLower() ?? "desc";

        if (!new[] { "title", "artist", "date", "genre", "year" }.Contains(sort)) sort = "date";
        if (!new[] { "asc", "desc" }.Contains(dir)) dir = "desc";

        var baseQuery = _context.Albums
            .AsNoTracking()
            .Include(a => a.User);

        var sortedQuery = sort switch
        {
            "title" => (dir == "asc" ? baseQuery.OrderBy(a => a.Title) : baseQuery.OrderByDescending(a => a.Title)),
            "artist" => (dir == "asc" ? baseQuery.OrderBy(a => a.Artist) : baseQuery.OrderByDescending(a => a.Artist)),
            "genre" => (dir == "asc" ? baseQuery.OrderBy(a => a.Genre) : baseQuery.OrderByDescending(a => a.Genre)),
            "year" => (dir == "asc" ? baseQuery.OrderBy(a => a.ReleaseYear) : baseQuery.OrderByDescending(a => a.ReleaseYear)),
            _ => (dir == "asc" ? baseQuery.OrderBy(a => a.CreatedAt) : baseQuery.OrderByDescending(a => a.CreatedAt))
        };

        var uid = HttpContext.Session.GetInt32(SessionUserId) ?? -1;

        var albums = await sortedQuery
            .Select(a => new AlbumItemViewModel
            {
                Id = a.Id,
                Title = a.Title,
                Artist = a.Artist,
                Genre = a.Genre,
                ReleaseYear = a.ReleaseYear,
                Username = a.User!.Username,
                LikeCount = a.Likes.Count,
                LikedByMe = a.Likes.Any(l => l.UserId == uid)
            })
            .ToListAsync();

        ViewBag.CurrentSort = sort;
        ViewBag.CurrentDir = dir;
        ViewBag.NextDir = dir == "asc" ? "desc" : "asc";

        return View(albums);
    }

    [HttpGet("/albums/new")]
    public IActionResult New()
    {
        var userId = HttpContext.Session.GetInt32(SessionUserId);
        if (userId == null)
        {
            TempData["Error"] = "Please login to add albums to your wishlist";
            return RedirectToAction("LoginForm", "Account");
        }
        return View();
    }

    [HttpPost("/albums/new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AlbumFormViewModel vm)
    {
        var userId = HttpContext.Session.GetInt32(SessionUserId);
        if (userId == null)
        {
            TempData["Error"] = "Please login to add albums to your wishlist";
            return RedirectToAction("LoginForm", "Account");
        }

        if (!ModelState.IsValid)
            return View("New", vm);

        var album = new Album
        {
            Title = vm.Title,
            Artist = vm.Artist,
            ReleaseYear = vm.ReleaseYear,
            Genre = vm.Genre,
            UserId = userId.Value,
        };

        _context.Albums.Add(album);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"'{album.Title}' by {album.Artist} has been added to the wishlist!";
        return RedirectToAction("Index");
    }

    [HttpPost("/albums/{id}/like")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Like(int id)
    {
        var userId = HttpContext.Session.GetInt32(SessionUserId);
        if (userId is not int uid)
        {
            TempData["Error"] = "Please login to like albums";
            return RedirectToAction("LoginForm", "Account");
        }

        bool already = await _context.Likes.AnyAsync(l => l.UserId == uid && l.AlbumId == id);
        if (!already)
        {
            _context.Likes.Add(new Like { UserId = uid, AlbumId = id });
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/albums/{id}/unlike")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlike(int id)
    {
        var userId = HttpContext.Session.GetInt32(SessionUserId);
        if (userId is not int uid)
        {
            TempData["Error"] = "Please login to unlike albums";
            return RedirectToAction("LoginForm", "Account");
        }

        var like = await _context.Likes.FirstOrDefaultAsync(l => l.UserId == uid && l.AlbumId == id);
        if (like != null)
        {
            _context.Likes.Remove(like);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/my-albums")]
    public async Task<IActionResult> MyAlbums()
    {
        var userId = HttpContext.Session.GetInt32(SessionUserId);
        if (userId == null)
        {
            TempData["Error"] = "Please login to view your albums";
            return RedirectToAction("LoginForm", "Account");
        }

        var albums = await _context
            .Albums.AsNoTracking()
            .Where(a => a.UserId == userId.Value)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        ViewBag.Username = HttpContext.Session.GetString("username");
        return View(albums);
    }
}
