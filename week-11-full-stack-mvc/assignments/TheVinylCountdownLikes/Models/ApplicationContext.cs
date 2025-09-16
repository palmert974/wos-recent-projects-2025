using Microsoft.EntityFrameworkCore;

namespace TheVinylCountdownLikes.Models;

// ==========================================
// DATABASE CONTEXT - Bridge between C# and MySQL
// This class tells Entity Framework:
// 1. What tables to create (DbSets)
// 2. How to connect to the database
// 3. Relationships between tables
// ==========================================

// ASSIGNMENT REQUIREMENT: "Update DbContext and Run Migrations"
public class ApplicationContext : DbContext
{
    // TABLE 1: Users Table
    // Creates 'Users' table in MySQL with columns from User model
    // Primary Key: Id (auto-increment)
    public DbSet<User> Users { get; set; }
    
    // TABLE 2: Albums Table  
    // ASSIGNMENT REQUIREMENT: "Add a DbSet<Album> to your ApplicationContext file."
    // Creates 'Albums' table with:
    // - Primary Key: Id (auto-increment)
    // - Foreign Key: UserId (references Users.Id)
    // - CASCADE DELETE: When user deleted, their albums deleted too
    public DbSet<Album> Albums { get; set; }

    // TABLE 3: Likes Table (Join table for many-to-many User <-> Album)
    public DbSet<Like> Likes { get; set; }
    
    // Constructor - receives database configuration from Program.cs
    public ApplicationContext(DbContextOptions<ApplicationContext> options) 
        : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Unique like per user per album
        modelBuilder.Entity<Like>()
            .HasIndex(l => new { l.UserId, l.AlbumId })
            .IsUnique();

        modelBuilder.Entity<Like>()
            .HasOne(l => l.User)
            .WithMany(u => u.Likes)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Like>()
            .HasOne(l => l.Album)
            .WithMany(a => a.Likes)
            .HasForeignKey(l => l.AlbumId)
            .OnDelete(DeleteBehavior.Cascade);
    }
    
    // RELATIONSHIP MAGIC HAPPENS HERE!
    // Entity Framework automatically creates the foreign key because:
    // 1. Album has 'UserId' property (foreign key)
    // 2. Album has 'User' navigation property
    // 3. User has 'List<Album>' navigation property
    // Result: One User -> Many Albums relationship with FK constraint
}
