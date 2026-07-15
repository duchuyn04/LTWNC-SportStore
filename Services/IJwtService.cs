using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace SportsStore.Services
{
    // Dịch vụ tạo và validate JWT token.
    public interface IJwtService
    {
        // Tạo JWT từ user và roles, trả về chuỗi token đã mã hóa.
        string GenerateToken(IdentityUser user, IEnumerable<string> roles);

        // Validate token và trả về ClaimsPrincipal nếu hợp lệ.
        ClaimsPrincipal? ValidateToken(string token);
    }
}
