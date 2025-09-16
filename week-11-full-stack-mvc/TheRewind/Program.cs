using Microsoft.EntityFrameworkCore;
using TheRewind.Models;
using TheRewind.Services;

var builder = WebApplication.CreateBuilder(args);

// =============================
// Program.cs (app bootstrapping)
// - Registers MVC, Session, DbContext, and services
// - Sets the error pipeline and main route
// =============================

// MVC pipeline: enable controllers + views
builder.Services.AddControllersWithViews();

// Session (manual auth): persists userId/username between requests
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;   // mitigate XSS cookie theft
    options.Cookie.IsEssential = true; // required for auth/session to function
});

// EF Core + MySQL: register DbContext using MySql connection string
// appsettings.json must include:
// "ConnectionStrings": { "MySqlConnection": "Server=localhost;Database=therewinddb;User=root;Password=rootroot;" }
var cs = builder.Configuration.GetConnectionString("MySqlConnection");
builder.Services.AddDbContext<ApplicationContext>(opt =>
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs))
);

// Password hashing service (BCrypt)
builder.Services.AddScoped<IPasswordService, BcryptService>();

var app = builder.Build();

// Error pipeline for prod-like runs
// - In Production: send unhandled errors to /error/500 and map status codes to /error/{code}
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error/500"); // friendly 500
    app.UseStatusCodePagesWithReExecute("/error/{0}"); // friendly 404/401/403
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // IMPORTANT: session before authorization
app.UseAuthorization();

// Conventional route. Controllers also use attribute routing for clean URLs.
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

// Optional seed hook (only if you created a Seed class)
// using (var scope = app.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
//     // TheRewind.Data.Seed.Run(db);
// }

app.Run();
