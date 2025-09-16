using Microsoft.EntityFrameworkCore;

namespace AuthForge.Models;

// EF Core DbContext: represents our database session
// Add DbSet<T> properties for each table you want to track.
public class ApplicationContext : DbContext
{
    // Users table
    public DbSet<User> Users { get; set; }

    public ApplicationContext(DbContextOptions<ApplicationContext> options)
        : base(options) { }
}

