using System.ComponentModel.DataAnnotations;

namespace AuthForge.Models;

// Database entity for a registered user
public class User
{
    [Key]
    public int Id { get; set; }

    // Unique username we display in the UI
    public string Username { get; set; } = string.Empty;

    // Unique email used for login
    public string Email { get; set; } = string.Empty;

    // BCrypt hashed password (never store plain text!)
    public string PasswordHash { get; set; } = string.Empty;

    // Auditing timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

