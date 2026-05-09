namespace UniStay.Services.Interfaces
{
    public interface IPasswordService
    {
        string HashPassword(string plainText);      // BCrypt فقط
        bool VerifyPassword(string plainText, string hash);
    }
}