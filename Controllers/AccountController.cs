using SportsStore.Services;
using SportsStore.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SportsStore.Models;
using SportsStore.Models.ViewModels;

namespace SportsStore.Controllers
{
    // Controller xử lý đăng ký, đăng nhập và đăng xuất cho khách hàng.
    // Sau khi đăng nhập/đăng ký thành công, giỏ hàng trong session sẽ được gộp vào database.
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IJwtService _jwtService;
        private readonly ICartService _cartService;

        public AccountController(UserManager<IdentityUser> userManager,
            IJwtService jwtService, ICartService cartService)
        {
            _userManager = userManager;
            _jwtService = jwtService;
            _cartService = cartService;
        }

        // Ghi JWT vào cookie HttpOnly để browser tự gửi theo mỗi request.
        private void SetJwtCookie(string token)
        {
            Response.Cookies.Append("access_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddHours(24)
            });
        }

        // Hiển thị form đăng ký tài khoản.
        public IActionResult Register() => View();

        // Xử lý đăng ký: tạo user mới, đăng nhập ngay và gộp giỏ hàng từ session.
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Chỉ xử lý khi dữ liệu nhập vào hợp lệ theo annotation của ViewModel.
            if (ModelState.IsValid)
            {
                // Khởi tạo đối tượng user với username và email từ form.
                IdentityUser user = new IdentityUser
                {
                    UserName = model.Username,
                    Email = model.Email
                };

                // Gọi Identity tạo tài khoản với mật khẩu từ form đăng ký.
                IdentityResult result = await _userManager.CreateAsync(user, model.Password);

                // Nếu tạo thành công thì tạo JWT, set cookie, gộp giỏ hàng session và chuyển về trang chủ.
                if (result.Succeeded)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var token = _jwtService.GenerateToken(user, roles);
                    SetJwtCookie(token);

                    await _cartService.MergeSessionToDbAsync(model.Username);
                    return RedirectToAction("Index", "Home");
                }

                // Hiển thị lỗi từ Identity (ví dụ: mật khẩu yếu, username đã tồn tại).
                foreach (IdentityError error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            // Trả lại form kèm lỗi validate hoặc lỗi từ Identity.
            return View(model);
        }

        // Hiển thị form đăng nhập.
        public IActionResult Login() => View();

        // Xác thực thông tin đăng nhập, sau đó gộp giỏ hàng session vào tài khoản.
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Kiểm tra dữ liệu form trước khi gọi hệ thống xác thực.
            if (ModelState.IsValid)
            {
                // Tìm user và kiểm tra password trực tiếp qua UserManager.
                var user = await _userManager.FindByNameAsync(model.Username);
                if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
                {
                    // Tạo JWT từ user + roles, ghi vào cookie rồi gộp giỏ hàng.
                    var roles = await _userManager.GetRolesAsync(user);
                    var token = _jwtService.GenerateToken(user, roles);
                    SetJwtCookie(token);

                    await _cartService.MergeSessionToDbAsync(model.Username);
                    return RedirectToAction("Index", "Home");
                }

                // Thông báo lỗi chung để tránh lộ thông tin tài khoản có tồn tại hay không.
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
            }

            // Quay lại form đăng nhập nếu validate hoặc xác thực thất bại.
            return View(model);
        }

        // Đăng xuất: xóa cookie JWT và quay về trang chủ.
        public IActionResult Logout()
        {
            Response.Cookies.Delete("access_token");
            return RedirectToAction("Index", "Home");
        }
    }
}
