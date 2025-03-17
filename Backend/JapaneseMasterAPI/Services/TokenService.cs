using Microsoft.IdentityModel.Tokens;
using JapaneseMasterAPI.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using dotenv.net;
using JapaneseMasterAPI.Data;
using JapaneseMasterAPI.Models;

namespace JapaneseMasterAPI.Services
{
    public class TokenService(IConfiguration configuration)

    {
        public string CreateToken(User user)
        {

            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");

            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JW_SECRET_KEY is missing, make sure it is set in a .env file");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new JwtSecurityToken(
                issuer: configuration.GetValue<string>("AppSettings:Issuer"),
                audience: configuration.GetValue<string>("AppSettings:Audience"),
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: creds);


            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public async Task<string> SaveRefreshToken(User user, JMDbContext context)
        {

            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RTokenExpiryDate = DateTime.UtcNow.AddDays(7);
            await context.SaveChangesAsync();
            return refreshToken;
        }

        private async Task<User?> ValidateRefreshToken(Guid userId, string refreshToken, JMDbContext context)
        {
            var user = await context.Users.FindAsync(userId);
            if (user == null || user.RefreshToken != refreshToken || user.RTokenExpiryDate <= DateTime.Now)
            {
                return null;
            }
            return user;
        }

        public async Task<RefreshTokenDto?> RefreshToken(RefreshTokenDto request, JMDbContext context)
        {
            var user = await ValidateRefreshToken(request.UserId, request.RefreshToken, context);

            if (user == null)
            {
                return null;
            }

            var newAccessToken = CreateToken(user);
            var newRefreshToken = await SaveRefreshToken(user, context);

            return new RefreshTokenDto
            {
                RefreshToken = newRefreshToken,
                UserId = user.Id,
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(7)
            };
        }
    }
}
