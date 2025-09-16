# Merged into THE-REWIND-BUILD-GUIDE.md

This document has been consolidated. Please use the single canonical guide:
- THE-REWIND-BUILD-GUIDE.md

## Project Overview
A full-stack MVC social movie rating application with authentication, CRUD operations, and many-to-many relationships through ratings.

## Complete Feature Checklist

### ✅ Authentication System
- [x] User registration with Username, Email, Password, Confirm Password
- [x] User login with session management
- [x] Password hashing with BCrypt
- [x] Profile page showing user statistics
- [x] Logout with confirmation prompt
- [x] Protected routes (redirects to login when not authenticated)

### ✅ Movie Management  
- [x] List all movies with ratings
- [x] Create new movies (authenticated users only)
- [x] Edit movies (owner only with authorization check)
- [x] Delete movies (owner only with confirmation)
- [x] Movie details page with all information
- [x] Display average ratings to 1 decimal place

### ✅ Rating System
- [x] Authenticated users can rate movies (1-5 stars)
- [x] One rating per user per movie (enforced in code)
- [x] Rating form hidden if user already rated
- [x] Average rating display on list and details

### ✅ Data Validation
- [x] Server-side validation with data annotations
- [x] Field-level error messages
- [x] Validation summaries on forms
- [x] CSRF protection on all POST actions

### ✅ Navigation & UI
- [x] Conditional navbar (different for logged in/out)
- [x] Dark theme Bootstrap layout
- [x] Footer with copyright
- [x] Alert system for success/error messages
- [x] Responsive design

Dark theme implementation (Vinyl Countdown pattern)
- _Layout.cshtml uses <html data-bs-theme="dark">
- Assets are local: ~/lib/bootstrap/dist/css/bootstrap.min.css and ~/lib/bootstrap/dist/js/bootstrap.bundle.min.js
- CSS kept minimal (wwwroot/css/site.css) — no forced bg-dark/text-light overrides
- Views use standard Bootstrap classes (card, table table-hover, btn btn-*)
- No CDN duplication and no forced dark classes on cards/tables
- If styles look wrong, do a hard refresh (Cmd+Shift+R)

### ✅ Error Handling
- [x] Custom 404 page
- [x] Custom 500 page  
- [x] General error page
- [x] Forbid (403) for unauthorized actions

## Database Schema

### User Model
```csharp
public class User
{
    public int Id { get; set; }
    
    [Required, StringLength(32, MinimumLength = 2)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    // Navigation properties
    public List<Movie> Movies { get; set; } = new();
    public List<Rating> Ratings { get; set; } = new();
}
```

### Movie Model (Updated with all required fields)
```csharp
public class Movie
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Title is required")]
    [MinLength(2, ErrorMessage = "Title must be at least 2 characters")]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Genre is required")]
    [MinLength(2, ErrorMessage = "Genre must be at least 2 characters")]
    [Display(Name = "Genre")]
    public string Genre { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Release Date is required")]
    [Display(Name = "Release Date")]
    [DataType(DataType.Date)]
    public DateTime ReleaseDate { get; set; }
    
    [Required(ErrorMessage = "Description is required")]
    [MinLength(10, ErrorMessage = "Description must be at least 10 characters")]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;
    
    // Foreign Key
    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }
    
    // Navigation
    public List<Rating> Ratings { get; set; } = new();
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

### Rating Model
```csharp
public class Rating
{
    public int Id { get; set; }
    
    [Range(1, 5)]
    public int Value { get; set; }
    
    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }
    
    [Required]
    public int MovieId { get; set; }
    public Movie? Movie { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

## ViewModels

### RegisterViewModel
```csharp
public class RegisterViewModel
{
    [Required(ErrorMessage = "Username is required")]
    [MinLength(2, ErrorMessage = "Username must be at least 2 characters")]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Please confirm your password")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords must match")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

### LoginViewModel
```csharp
public class LoginViewModel
{
    [Required(ErrorMessage = "Username is required")]
    public string Username { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
```

## Routes & Navigation Flow

### Authentication Routes
- `GET /` → Home page (public)
- `GET /account/register` → Registration form
- `POST /account/register` → Process registration → Redirect to `/movies`
- `GET /account/login` → Login form
- `POST /account/login` → Process login → Redirect to `/movies`
- `POST /account/logout` → Logout → Redirect to `/`
- `GET /account/profile` → User profile (protected)

### Movie Routes
- `GET /movies` → List all movies
- `GET /movies/create` → Create form (protected)
- `POST /movies/create` → Process creation → Redirect to `/movies/{id}`
- `GET /movies/{id}` → Movie details
- `GET /movies/edit/{id}` → Edit form (owner only)
- `POST /movies/edit/{id}` → Process edit → Redirect to `/movies/{id}`
- `GET /movies/delete/{id}` → Delete confirmation (owner only)
- `POST /movies/delete/{id}` → Process deletion → Redirect to `/movies`
- `POST /movies/{id}/rate` → Rate movie → Redirect to `/movies/{id}`

### Error Routes
- `/error/404` → Page not found
- `/error/500` → Server error
- `/error/{code}` → General error handler

## Key Implementation Details

### Program.cs Configuration
```csharp
// Session configuration
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Database
var cs = builder.Configuration.GetConnectionString("MySqlConnection");
builder.Services.AddDbContext<ApplicationContext>(opt =>
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs))
);

// Password service
builder.Services.AddScoped<IPasswordService, BcryptService>();

// Error handling middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error/500");
    app.UseStatusCodePagesWithReExecute("/error/{0}");
}
```

### Authorization Pattern in Controllers
```csharp
// Check if logged in
if (!IsLoggedIn) return RedirectToAction("Login", "Account");

// Check if user owns resource
if (movie.UserId != CurrentUserId) return Forbid();
```

### Rating Enforcement
```csharp
// Check for existing rating
var existingRating = await _context.Ratings
    .FirstOrDefaultAsync(r => r.MovieId == id && r.UserId == CurrentUserId);
if (existingRating != null) 
    return BadRequest("You have already rated this movie");
```

## Layout Structure

### _Layout.cshtml
- Dark theme: `data-bs-theme="dark"`
- Header with navbar partial
- Container for main content
- Alert system for TempData messages
- Footer
- Scripts: jQuery, Bootstrap, site.js

### _Navbar.cshtml
- Conditional rendering based on session
- Shows username when logged in
- Different menu items for authenticated users
- Logout button with confirmation

## Testing Checklist

### Authentication Tests
- [ ] Can register new account
- [ ] Cannot register with existing username
- [ ] Can login with valid credentials
- [ ] Cannot login with invalid credentials
- [ ] Session persists across pages
- [ ] Logout clears session

### Movie CRUD Tests
- [ ] Can view all movies without login
- [ ] Must login to create movie
- [ ] Can edit own movies
- [ ] Cannot edit others' movies
- [ ] Can delete own movies with confirmation
- [ ] Cannot delete others' movies

### Rating Tests
- [ ] Can rate movies when logged in
- [ ] Cannot rate same movie twice
- [ ] Average calculation is correct
- [ ] Rating form hidden after rating

### Validation Tests
- [ ] Registration validation works
- [ ] Movie form validation works
- [ ] Error messages display correctly
- [ ] CSRF tokens present on forms

## Common Issues & Solutions

### Issue: Migrations fail
```bash
# Remove and recreate
dotnet ef migrations remove
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Issue: Port already in use
```bash
# Kill existing process
lsof -i :5187
kill -9 [PID]
```

### Issue: Session not working
- Ensure `app.UseSession()` is called before `app.UseAuthorization()`
- Check session configuration in Program.cs

### Issue: Styles not loading
- Verify Bootstrap paths in _Layout.cshtml
- Check wwwroot/lib folder structure
- Clear browser cache

## Running the Application

```bash
# Build and run
dotnet build
dotnet run

# Or use watch mode for development
dotnet watch

# Access at
http://localhost:5187
```

## Database Commands

```bash
# Create migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove

# Drop database
dotnet ef database drop
```

## File Structure
```
TheRewind/
├── Controllers/
│   ├── AccountController.cs
│   ├── ErrorController.cs
│   ├── HomeController.cs
│   └── MoviesController.cs
├── Models/
│   ├── ApplicationContext.cs
│   ├── Movie.cs
│   ├── Rating.cs
│   └── User.cs
├── Services/
│   ├── BcryptService.cs
│   └── IPasswordService.cs
├── ViewModels/
│   ├── LoginViewModel.cs
│   └── RegisterViewModel.cs
├── Views/
│   ├── Account/
│   │   ├── Login.cshtml
│   │   ├── Profile.cshtml
│   │   └── Register.cshtml
│   ├── Error/
│   │   ├── 404.cshtml
│   │   ├── 500.cshtml
│   │   └── General.cshtml
│   ├── Home/
│   │   └── Index.cshtml
│   ├── Movies/
│   │   ├── Create.cshtml
│   │   ├── Delete.cshtml
│   │   ├── Details.cshtml
│   │   ├── Edit.cshtml
│   │   └── Index.cshtml
│   ├── Shared/
│   │   ├── _Layout.cshtml
│   │   ├── _Navbar.cshtml
│   │   └── _ValidationScriptsPartial.cshtml
│   └── _ViewImports.cshtml
├── wwwroot/
│   ├── css/
│   │   └── site.css
│   ├── js/
│   │   └── site.js
│   └── lib/
│       └── bootstrap/
├── appsettings.json
└── Program.cs
```

This completes the full implementation of The Rewind application with all required features!