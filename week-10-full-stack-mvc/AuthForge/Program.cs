using AuthForge.Models;
using AuthForge.Services;
using Microsoft.EntityFrameworkCore;

// Program.cs
// This is the app bootstrap: where we configure services (DI) and the request pipeline.
//
// DATABASE CONNECTION
// We read the MySQL connection string from configuration under the key
//   ConnectionStrings:MySqlConnection
// Recommended: store it in user-secrets during development so you never commit passwords.
//
// Quick setup (run these in the AuthForge project directory):
//   dotnet user-secrets init
//   dotnet user-secrets set "ConnectionStrings:MySqlConnection" "Server={{HOST}};Port={{PORT}};Database={{DB_NAME}};User={{DB_USER}};Password={{DB_PASSWORD}};SslMode=Preferred;"
// Replace {{...}} with your actual values. Do NOT commit secrets to source control.
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("MySqlConnection");

// Add services to the container.
// MVC controllers + Razor views
builder.Services.AddControllersWithViews();

// Session state so we can remember the logged-in user between requests.
builder.Services.AddSession();

// Inject our password hashing service (BCrypt implementation)
builder.Services.AddScoped<IPasswordService, BcryptPasswordService>();

// Register EF Core DbContext using MySQL (Pomelo provider)
// ServerVersion.AutoDetect inspects the connection and selects the correct MySQL version.
builder.Services.AddDbContext<ApplicationContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // In production, use the error handler page and enable HSTS
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Important: enable session before you need to read/write it in controllers.
app.UseSession();

// Serve static files from wwwroot
app.UseStaticFiles();

app.UseRouting();

// We aren't using [Authorize] yet, but keep this here for later.
app.UseAuthorization();

// Conventional routing: default to Home/Index
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
