using System.ComponentModel.DataAnnotations;

namespace AuthForge.ViewModels;

// ViewModel for the Register form (only contains fields the form needs)
public class RegisterFormViewModel
{
    // Username shown in the app (min 3 chars)
    [Required(ErrorMessage = "Please enter your username.")]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters long.")]
    public string Username { get; set; } = string.Empty;

    // Email used for login
    [DataType(DataType.EmailAddress)]
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    // Plain text password (will be hashed server-side)
    [DataType(DataType.Password)]
    [Required(ErrorMessage = "Please enter your password.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    public string Password { get; set; } = string.Empty;

    // Must match Password exactly
    [DataType(DataType.Password)]
    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
