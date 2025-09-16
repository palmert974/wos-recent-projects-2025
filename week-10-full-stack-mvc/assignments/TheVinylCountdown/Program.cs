using Microsoft.EntityFrameworkCore;
using TheVinylCountdown.Models;
using TheVinylCountdown.Services;

// ==========================================
// THE VINYL COUNTDOWN - MVC Application
// Assignment: One-to-Many Relationships
// Features: User Auth, Albums CRUD, Foreign Keys
// Database: MySQL (vinylcountdowndb)
// ==========================================

var builder = WebApplication.CreateBuilder(args);

// SERVICE CONFIGURATION SECTION
// ==========================================

// 1. MVC Services - Enables Controllers and Views
builder.Services.AddControllersWithViews();

// 2. DATABASE Configuration (Entity Framework + MySQL)
// Reads connection string from appsettings.json: "Server=localhost;Database=vinylcountdowndb;User=root;Password=rootroot;"
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 3. SESSION Configuration (for user authentication)
// Stores: userId and username after login
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);  // Session expires after 30 minutes
    options.Cookie.HttpOnly = true;                   // Prevents JavaScript access (security)
    options.Cookie.IsEssential = true;                // Required for GDPR compliance
});

// 4. PASSWORD SERVICE (BCrypt for secure password hashing)
// Used in AccountController for Register/Login
builder.Services.AddScoped<IPasswordService, BcryptService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error/500");
    app.UseStatusCodePagesWithReExecute("/error/{0}");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();  // Must be before UseAuthorization

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
