using System.Text;
using SportsStore.Services;
using SportsStore.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SportsStore.Models;

// Cấu hình ứng dụng ASP.NET Core MVC: đăng ký dịch vụ, middleware,
// routing, kết nối database và seed dữ liệu ban đầu.
var builder = WebApplication.CreateBuilder(args);

// Bật bộ nhớ cache phân tán, session và truy cập HttpContext.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();

// Đăng ký DbContext dùng SQL Server với chuỗi kết nối từ cấu hình.
builder.Services.AddDbContext<StoreDbContext>(opts =>
{
    // Lấy chuỗi kết nối từ cấu hình ứng dụng và cấu hình EF Core dùng SQL Server.
    opts.UseSqlServer(
        builder.Configuration["ConnectionStrings:SportsStoreConnection"]);
});

// Đăng ký repository thao tác dữ liệu sản phẩm.
builder.Services.AddScoped<IStoreRepository, EFStoreRepository>();

// Chọn nơi lưu giỏ hàng: database nếu đã đăng nhập, session nếu là khách.
builder.Services.AddScoped<ICartStore>(sp =>
{
    // Lấy HttpContext hiện tại và DbContext từ container để quyết định nơi lưu giỏ.
    var http = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
    var ctx = sp.GetRequiredService<StoreDbContext>();

    // Nếu tài khoản đã đăng nhập thì lưu giỏ vào database, ngược lại dùng session.
    if (http?.User?.Identity?.IsAuthenticated == true)
        return new DbCartStore(ctx, http.User.Identity!.Name!);
    return new SessionCartStore(sp.GetRequiredService<IHttpContextAccessor>());
});

// Đăng ký dịch vụ xử lý nghiệp vụ giỏ hàng.
builder.Services.AddScoped<ICartService, CartService>();

// Đăng ký hệ thống Identity dùng chung database với ứng dụng.
// Identity vẫn quản lý user/role/password, nhưng auth sẽ do JWT xử lý.
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<StoreDbContext>()
    .AddDefaultTokenProviders();

// Bind cấu hình JWT từ appsettings.json.
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("JwtOptions"));

// Đăng ký dịch vụ tạo/validate JWT.
builder.Services.AddScoped<IJwtService, JwtService>();

// Cấu hình JWT authentication, đọc token từ cookie access_token.
builder.Services.AddAuthentication(options =>
{
    // Đặt JWT làm scheme mặc định cho toàn bộ ứng dụng.
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtOptions = builder.Configuration.GetSection("JwtOptions").Get<JwtOptions>()!;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
    };

    // Đọc JWT từ cookie HttpOnly thay vì Authorization header.
    // Khi chưa đăng nhập, redirect về trang login thay vì trả 401.
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["access_token"];
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.Redirect("/Account/Login");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Pipeline xử lý request: file tĩnh, session, xác thực, phân quyền.
app.UseStaticFiles();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Định nghĩa các route: danh sách sản phẩm, phân trang, chi tiết sản phẩm.

// Route hiển thị danh sách sản phẩm ở đường dẫn /Products.
app.MapControllerRoute("products",
    "Products",
    new { Controller = "Home", action = "Products" });

// Route phân trang với tham số số trang trong URL.
app.MapControllerRoute("pagination",
    "Products/Page{productPage}",
    new { Controller = "Home", action = "Products" });

// Route xem chi tiết sản phẩm theo id.
app.MapControllerRoute("productDetail",
    "Products/Detail/{id:long}",
    new { Controller = "Home", action = "Detail" });

// Route mặc định cho các controller/action thông thường.
app.MapDefaultControllerRoute();

// Seed dữ liệu mẫu cho sản phẩm và tài khoản admin mặc định.

// Đảm bảo database có dữ liệu sản phẩm mẫu.
SeedData.EnsurePopulated(app);

// Đảm bảo tài khoản admin mặc định đã được tạo.
IdentitySeedData.EnsurePopulated(app).Wait();
app.Run();
