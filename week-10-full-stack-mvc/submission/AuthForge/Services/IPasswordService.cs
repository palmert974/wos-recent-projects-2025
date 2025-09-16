namespace AuthForge.Services;

// Abstraction for password hashing so we can swap implementations (e.g., BCrypt, Argon2)
public interface IPasswordService
{
    // Return a secure hash of the plain text password
    string Hash(string plainText);

    // Verify a plain text password against a stored hash
    bool Verify(string plainText, string hash);
}
