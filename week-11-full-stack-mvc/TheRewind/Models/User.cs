using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TheRewind.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, StringLength(32, MinimumLength = 2)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        // Store hashed password only (use BCrypt)
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // Timestamps (align with teacher projects)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation: One User -> Many Movies
        public List<Movie> Movies { get; set; } = new();

        // Navigation: One User -> Many Ratings
        public List<Rating> Ratings { get; set; } = new();
    }
}
