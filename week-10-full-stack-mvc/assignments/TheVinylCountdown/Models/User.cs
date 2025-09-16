using System.ComponentModel.DataAnnotations;

namespace TheVinylCountdown.Models;

// ASSIGNMENT REQUIREMENT: "Update the User Model"
// "Add a List<Album> navigation property to your User model. 
// This will represent the 'one' side of the one-to-many relationship."
public class User
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters")]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // ASSIGNMENT REQUIREMENT: One-to-Many Relationship
    // "Modeling a one-to-many relationship between a User and an Album"
    // One User can have MANY Albums (this is the "one" side)
    public List<Album> Albums { get; set; } = new List<Album>();
}
