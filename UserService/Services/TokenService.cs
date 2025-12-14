using System.Security.Claims;
using System.Text;
using UserService.Interfaces;
using UserService.Model;

namespace UserService.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(ApplicationUser user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!";
            var issuer = jwtSettings["Issuer"] ?? "UserService";
            var audience = jwtSettings["Audience"] ?? "OrderManagementSystem";
            var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
          
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),

            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),

            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

            new Claim(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

          
            var tokenDescriptor = new SecurityTokenDescriptor
            {
     
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
                // Issuer - who created the token (this service)
                Issuer = issuer,
                // Audience - who the token is intended for (other services)
                Audience = audience,
                SigningCredentials = credentials
            };

            // Create token handler handles token generation and validation
            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
