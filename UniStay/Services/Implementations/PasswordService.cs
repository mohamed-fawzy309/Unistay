using UniStay.Services.Interfaces;

namespace UniStay.Services.Implementations
{
    public class PasswordService : IPasswordService
    {
        public string HashPassword(string plainText) => BCrypt.Net.BCrypt.HashPassword(plainText, 12);
        public bool VerifyPassword(string plainText, string hash) => BCrypt.Net.BCrypt.Verify(plainText, hash);
    }
}
