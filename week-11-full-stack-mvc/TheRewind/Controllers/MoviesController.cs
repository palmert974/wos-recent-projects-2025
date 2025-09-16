using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheRewind.Models;

namespace TheRewind.Controllers
{
    [Route("movies")]
    public class MoviesController : Controller
    {
        // Beginner note: Main feature controller for listing, creating, editing, deleting, and rating movies.
        private readonly ApplicationContext _context;
        private readonly ILogger<MoviesController> _logger;

        public MoviesController(ApplicationContext context, ILogger<MoviesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private int? CurrentUserId => HttpContext.Session.GetInt32("userId");
        private bool IsLoggedIn => CurrentUserId.HasValue;

        // GET: /movies
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            // Guard: redirect unauthenticated users to login
            if (!IsLoggedIn)
            {
                TempData["Error"] = "Please sign in to continue.";
                return RedirectToAction("Login", "Account");
            }

            // Read-only query: AsNoTracking improves performance
            _logger.LogInformation(
                "Movies/Index requested by user {UserId} at {Time}",
                CurrentUserId,
                DateTime.UtcNow
            );
            var movies = await _context
                .Movies.AsNoTracking()
                .Include(m => m.User)
                .Include(m => m.Ratings)
                .ToListAsync();
            ViewBag.IsLoggedIn = IsLoggedIn;
            return View(movies);
        }

        // GET: /movies/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (!IsLoggedIn)
            {
                TempData["Error"] = "Please sign in to continue.";
                return RedirectToAction("Login", "Account");
            }

            if (id is null)
                return NotFound();
            var movie = await _context
                .Movies.Include(m => m.User)
                .Include(m => m.Ratings)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            _logger.LogInformation(
                "Movies/Details id={Id} requested by user {UserId}",
                id,
                CurrentUserId
            );
            if (movie is null)
                return NotFound();

            var hasRated = await _context.Ratings.AnyAsync(r =>
                r.MovieId == movie.Id && r.UserId == CurrentUserId
            );
            ViewBag.HasRated = hasRated;
            ViewBag.Average =
                (movie.Ratings != null && movie.Ratings.Count > 0)
                    ? movie.Ratings.Average(r => r.Value)
                    : 0d;
            return View(movie);
        }

        // GET: /movies/create
        [HttpGet("create")]
        public IActionResult Create()
        {
            if (!IsLoggedIn)
            {
                TempData["Error"] = "Please sign in to continue.";
                return RedirectToAction("Login", "Account");
            }
            return View(new Movie());
        }

        // POST: /movies/create
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Movie movie)
        {
            if (!IsLoggedIn)
            {
                TempData["Error"] = "Please sign in to continue.";
                return RedirectToAction("Login", "Account");
            }
            if (!ModelState.IsValid)
                return View(movie);

            try
            {
                movie.UserId = CurrentUserId!.Value;
                _context.Movies.Add(movie);
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "Movie created id={Id} by user {UserId}",
                    movie.Id,
                    CurrentUserId
                );
                TempData["Success"] = "Movie added successfully!";
                return RedirectToAction(nameof(Details), new { id = movie.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating movie for user {UserId}", CurrentUserId);
                TempData["Error"] = "An error occurred while adding the movie. Please try again.";
                return View(movie);
            }
        }

        // GET: /movies/edit/{id}
        [HttpGet("edit/{id:int}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsLoggedIn)
            {
                TempData["Error"] = "Please sign in to continue.";
                return RedirectToAction("Login", "Account");
            }
            if (id is null)
                return NotFound();
            var movie = await _context.Movies.FindAsync(id);
            if (movie is null)
                return NotFound();
            if (movie.UserId != CurrentUserId)
                return Forbid();
            return View(movie);
        }

        // POST: /movies/edit/{id}
        [HttpPost("edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Movie movie)
        {
            if (!IsLoggedIn)
            {
                TempData["Error"] = "Please sign in to continue.";
                return RedirectToAction("Login", "Account");
            }
            if (id != movie.Id)
                return NotFound();
            var existingMovie = await _context.Movies.FindAsync(id);
            if (existingMovie is null)
                return NotFound();
            if (existingMovie.UserId != CurrentUserId)
                return Forbid();

            if (!ModelState.IsValid)
                return View(movie);

            existingMovie.Title = movie.Title;
            existingMovie.Genre = movie.Genre;
            existingMovie.ReleaseDate = movie.ReleaseDate;
            existingMovie.Description = movie.Description;
            existingMovie.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation(
                "Movie updated id={Id} by user {UserId}",
                movie.Id,
                CurrentUserId
            );
            TempData["Success"] = "Movie updated successfully!";
            return RedirectToAction(nameof(Details), new { id = movie.Id });
        }

        // GET: /movies/delete/{id}
        [HttpGet("delete/{id:int}")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsLoggedIn)
            {
                TempData["Error"] = "Please sign in to continue.";
                return RedirectToAction("Login", "Account");
            }
            if (id is null)
                return NotFound();
            var movie = await _context
                .Movies.Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie is null)
                return NotFound();
            if (movie.UserId != CurrentUserId)
                return Forbid();
            return View(movie);
        }

        // POST: /movies/delete/{id}
        [HttpPost("delete/{id:int}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsLoggedIn)
            {
                TempData["Error"] = "Please sign in to continue.";
                return RedirectToAction("Login", "Account");
            }
            var movie = await _context.Movies.FindAsync(id);
            if (movie is null)
                return NotFound();
            if (movie.UserId != CurrentUserId)
                return Forbid();

            _context.Movies.Remove(movie);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Movie deleted id={Id} by user {UserId}", id, CurrentUserId);
            TempData["Success"] = "Movie deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /movies/{id}/rate
        [HttpPost("{id:int}/rate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rate(int id, int value)
        {
            if (!IsLoggedIn)
            {
                TempData["Error"] = "Please sign in to continue.";
                return RedirectToAction("Login", "Account");
            }

            if (value < 1 || value > 5)
            {
                TempData["Error"] = "Rating must be between 1 and 5.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var movie = await _context.Movies.FindAsync(id);
            if (movie is null)
                return NotFound();

            var existingRating = await _context.Ratings.FirstOrDefaultAsync(r =>
                r.MovieId == id && r.UserId == CurrentUserId
            );
            if (existingRating != null)
            {
                TempData["Error"] = "You have already rated this movie.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var rating = new Rating
            {
                MovieId = id,
                UserId = CurrentUserId!.Value,
                Value = value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _context.Ratings.Add(rating);
            await _context.SaveChangesAsync();
            _logger.LogInformation(
                "Rating added movieId={Id} by user {UserId} value={Value}",
                id,
                CurrentUserId,
                value
            );
            TempData["Success"] = "Rating added successfully!";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
