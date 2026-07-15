using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SportsStore.Models;
using SportsStore.Services;
using Xunit;

namespace SportsStore.Tests.Services
{
    // Unit tests cho JwtService: tạo token, validate đúng/sai, hết hạn.
    public class JwtServiceTests
    {
        private readonly JwtOptions _options = new JwtOptions
        {
            Issuer = "SportsStore",
            Audience = "SportsStoreUsers",
            SecretKey = "this-is-a-super-secret-key-32bytes!",
            ExpiryHours = 1
        };

        // Tạo JwtService với options mặc định.
        private JwtService CreateService()
        {
            return new JwtService(Options.Create(_options));
        }

        // Tạo user giả lập cho các test case.
        private IdentityUser CreateUser()
        {
            return new IdentityUser
            {
                Id = "user-123",
                UserName = "testuser",
                Email = "test@example.com"
            };
        }

        [Fact]
        public void GenerateToken_ReturnsNonEmptyString()
        {
            var service = CreateService();
            var token = service.GenerateToken(CreateUser(), new[] { "User" });

            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        [Fact]
        public void ValidateToken_ValidToken_ReturnsPrincipalWithClaims()
        {
            var service = CreateService();
            var user = CreateUser();
            var token = service.GenerateToken(user, new[] { "User", "Admin" });

            var principal = service.ValidateToken(token);

            Assert.NotNull(principal);
            Assert.Equal(user.Id, principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            Assert.Equal(user.UserName, principal.FindFirst(ClaimTypes.Name)?.Value);
            Assert.Equal(user.Email, principal.FindFirst(ClaimTypes.Email)?.Value);
            Assert.Contains(principal.FindAll(ClaimTypes.Role), c => c.Value == "User");
            Assert.Contains(principal.FindAll(ClaimTypes.Role), c => c.Value == "Admin");
        }

        [Fact]
        public void ValidateToken_InvalidSignature_ReturnsNull()
        {
            var service = CreateService();
            var token = service.GenerateToken(CreateUser(), Array.Empty<string>());

            // Dùng secret key khác để validate -> phải thất bại.
            var tamperedOptions = new JwtOptions
            {
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                SecretKey = "different-super-secret-key-32bytes!",
                ExpiryHours = _options.ExpiryHours
            };
            var tamperedService = new JwtService(Options.Create(tamperedOptions));
            var principal = tamperedService.ValidateToken(token);

            Assert.Null(principal);
        }

        [Fact]
        public void ValidateToken_ExpiredToken_ReturnsNull()
        {
            // Tạo token đã hết hạn ngay lập tức.
            var expiredOptions = new JwtOptions
            {
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                SecretKey = _options.SecretKey,
                ExpiryHours = -1
            };
            var service = new JwtService(Options.Create(expiredOptions));
            var token = service.GenerateToken(CreateUser(), Array.Empty<string>());

            var principal = CreateService().ValidateToken(token);

            Assert.Null(principal);
        }
    }
}
