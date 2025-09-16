using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TheRewind.Models
{
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

        // Foreign Key to the User who added it
        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        // Ratings for this movie
        public List<Rating> Ratings { get; set; } = new();

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
