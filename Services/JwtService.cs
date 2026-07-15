using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SportsStore.Models;

namespace SportsStore.Services
{
    // Triển khai tạo và validate JWT dựa trên cấu hình JwtOptions.
    public class JwtService : IJwtService
    {
        private readonly JwtOptions _options;

        // Inject cấu hình JWT để dùng khi ký và xác thực token.
        public JwtService(IOptions<JwtOptions> options)
        {
            _options = options.Value;
        }

        // Tạo JWT chứa sub, name, email và các role claims.
        public string GenerateToken(IdentityUser user, IEnumerable<string> roles)
        {
            // Chuẩn bị danh sách claims từ thông tin user.
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Thêm từng role vào claims để [Authorize(Roles = "...")] hoạt động.
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Tạo khóa đối xứng từ secret key và dùng HMAC-SHA256 để ký token.
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Tạo token với issuer, audience, claims và hạn sử dụng.
            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(_options.ExpiryHours),
                signingCredentials: creds);

            // Chuyển token thành chuỗi JWT hoàn chỉnh.
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Validate token với cấu hình hiện tại.
        public ClaimsPrincipal? ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_options.SecretKey);

            try
            {
                // Validate toàn bộ thông tin: issuer, audience, lifetime, signing key.
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _options.Issuer,
                    ValidAudience = _options.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                }, out _);

                return principal;
            }
            catch
            {
                // Token không hợp lệ hoặc đã hết hạn.
                return null;
            }
        }
    }
}
