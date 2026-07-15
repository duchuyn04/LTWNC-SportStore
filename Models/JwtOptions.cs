namespace SportsStore.Models
{
    // Cấu hình JWT, được bind từ appsettings.json.
    public class JwtOptions
    {
        // Tên tổ chức phát hành token, dùng để validate issuer.
        public string Issuer { get; set; } = string.Empty;

        // Đối tượng sử dụng token, dùng để validate audience.
        public string Audience { get; set; } = string.Empty;

        // Khóa bí mật để ký và xác thực token, phải >= 32 bytes.
        public string SecretKey { get; set; } = string.Empty;

        // Số giờ token còn hiệu lực.
        public int ExpiryHours { get; set; } = 24;
    }
}
