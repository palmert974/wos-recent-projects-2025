using System.ComponentModel.DataAnnotations;

namespace AuthForge.ViewModels;

// ViewModel for the Login form (no database fields here)
public class LoginFormViewModel
{
    // Email used to find the user record
    [DataType(DataType.EmailAddress)]
    [Required(ErrorMessage = "Please enter email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    // Plain text password (compared against stored hash)
    [DataType(DataType.Password)]
    [Required(ErrorMessage = "Please enter password.")]
    [MinLength(8, ErrorMessage = "Password must be at least eight characters long.")]
    public string Password { get; set; } = string.Empty;

    // For query-string driven alerts (not-authenticated, invalid-credentials)
    public string? Error { get; set; }

    // For logout-successful banner (teacher style)
    public string? Message { get; set; }
}
