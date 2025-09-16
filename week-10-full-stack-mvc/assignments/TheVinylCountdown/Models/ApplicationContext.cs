using Microsoft.EntityFrameworkCore;

namespace TheVinylCountdown.Models;

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
    
    // Constructor - receives database configuration from Program.cs
    public ApplicationContext(DbContextOptions<ApplicationContext> options) 
        : base(options) { }
    
    // RELATIONSHIP MAGIC HAPPENS HERE!
    // Entity Framework automatically creates the foreign key because:
    // 1. Album has 'UserId' property (foreign key)
    // 2. Album has 'User' navigation property
    // 3. User has 'List<Album>' navigation property
    // Result: One User -> Many Albums relationship with FK constraint
}
