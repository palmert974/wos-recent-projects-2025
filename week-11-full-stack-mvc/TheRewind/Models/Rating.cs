using System;
using System.ComponentModel.DataAnnotations;

namespace TheRewind.Models
{
    public class Rating
    {
        public int Id { get; set; }

        // 1-5 inclusive
        [Range(1, 5)]
        public int Value { get; set; }

        // FK: which user rated
        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        // FK: which movie was rated
        [Required]
        public int MovieId { get; set; }
        public Movie? Movie { get; set; }

        // Timestamps (align with teacher projects)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
