using System.ComponentModel.DataAnnotations;

namespace TheVinylCountdown.Models;

// ASSIGNMENT REQUIREMENT: "Create the Album Model"
// "The Album model should have the following properties: 
// Id, Title, Artist, UserId, and a navigation property for User."
public class Album
{
    // Primary Key
    [Key]
    public int Id { get; set; }
    
    // ASSIGNMENT REQUIREMENT: Album must have Title property
    [Required(ErrorMessage = "Title is required")]
    [MinLength(1, ErrorMessage = "Title must be at least 1 character")]
    public string Title { get; set; } = string.Empty;
    
    // ASSIGNMENT REQUIREMENT: Album must have Artist property
    [Required(ErrorMessage = "Artist is required")]
    [MinLength(1, ErrorMessage = "Artist must be at least 1 character")]
    public string Artist { get; set; } = string.Empty;
    
    // Additional fields shown in wireframe
    [Range(1900, 2100, ErrorMessage = "Please enter a valid year")]
    public int? ReleaseYear { get; set; }
    
    public string? Genre { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // ASSIGNMENT REQUIREMENT: "Creating an Album model with a foreign key to User"
    // Foreign key - MUST be UserId (follows EF Core naming convention: TableName + Id)
    // This links each album to the user who added it
    public int UserId { get; set; }
    
    // ASSIGNMENT REQUIREMENT: Navigation property to User
    // "Create a new Album model that contains a foreign key and a navigation property to User"
    // This allows us to access the User who created this Album
    public User? User { get; set; }
}
