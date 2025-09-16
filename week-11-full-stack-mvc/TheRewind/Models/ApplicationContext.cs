using Microsoft.EntityFrameworkCore;

namespace TheRewind.Models
{
    public class ApplicationContext : DbContext
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Movie> Movies => Set<Movie>();
        public DbSet<Rating> Ratings => Set<Rating>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // RELATIONSHIPS OVERVIEW
            // User 1..* Movie  (a user owns many movies)
            // User 1..* Rating (a user has many ratings)
            // Movie 1..* Rating (a movie has many ratings)

            // One User -> Many Movies (cascade delete movies when user is deleted)
            modelBuilder
                .Entity<Movie>()
                .HasOne(m => m.User)
                .WithMany(u => u.Movies)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // One User -> Many Ratings (cascade delete ratings when user deleted)
            modelBuilder
                .Entity<Rating>()
                .HasOne(r => r.User)
                .WithMany(u => u.Ratings)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // One Movie -> Many Ratings (cascade delete ratings when movie deleted)
            modelBuilder
                .Entity<Rating>()
                .HasOne(r => r.Movie)
                .WithMany(m => m.Ratings)
                .HasForeignKey(r => r.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            // DB constraint: one rating per (UserId, MovieId)
            modelBuilder.Entity<Rating>().HasIndex(r => new { r.UserId, r.MovieId }).IsUnique();
        }
    }
}
