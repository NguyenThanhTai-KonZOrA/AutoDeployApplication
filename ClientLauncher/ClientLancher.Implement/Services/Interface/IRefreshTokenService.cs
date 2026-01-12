namespace ClientLancher.Implement.Services.Interface
{
    public interface IRefreshTokenService
    {
        string GenerateRefreshToken();
        void StoreRefreshToken(string username, string refreshToken, DateTime expiration);
        bool ValidateRefreshToken(string username, string refreshToken);
        void RevokeRefreshToken(string username);
    }
}