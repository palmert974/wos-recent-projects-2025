namespace AuthForge.Services;

// BCrypt implementation of IPasswordService
public class BcryptPasswordService : IPasswordService
{
    public string Hash(string plainText)
    {
        // BCrypt automatically handles salt generation under the hood
        return BCrypt.Net.BCrypt.HashPassword(plainText);
    }

    public bool Verify(string plainText, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(plainText, hash);
    }
}

