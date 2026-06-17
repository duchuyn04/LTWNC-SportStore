# Thiết kế tính năng Giỏ hàng và Thanh toán

## Ngữ cảnh

Dự án SportsStore là ứng dụng ASP.NET Core 6 MVC. Hiện tại đã có:

- `Product`, `StoreDbContext`, `IStoreRepository`, `EFStoreRepository`
- Identity authentication (`AccountController`)
- Trang danh sách sản phẩm, chi tiết sản phẩm, quản lý sản phẩm
- Chưa có giỏ hàng, thanh toán, lịch sử đơn hàng

## Mục tiêu

1. Cho phép khách hàng thêm/xóa/thay đổi số lượng sản phẩm trong giỏ hàng.
2. Hiển thị biểu tượng giỏ hàng trên header với số lượng sản phẩm.
3. Cho phép thanh toán giỏ hàng và lưu đơn hàng vào database.
4. Hiển thị lịch sử đơn hàng cá nhân sau khi thanh toán.

## Quyết định thiết kế

| Quyết định                  | Lựa chọn                                                | Lý do                                                             |
| --------------------------- | ------------------------------------------------------- | ----------------------------------------------------------------- |
| Lưu giỏ hàng                | Session cho khách vãng lai, DB cho ngườI dùng đăng nhập | Đáp ứng yêu cầu ngườI dùng, cân bằng giữa đơn giản và persistence |
| Merge session khi đăng nhập | Có, merge vào DB                                        | Khách không mất giỏ khi đăng nhập                                 |
| Chỉnh số lượng trong giỏ    | Nút +/- và nút xóa                                      | Không dùng input số lượng, đơn giản hơn                           |
| Thanh toán                  | Bắt buộc đăng nhập, form thông tin giao hàng            | Đơn giản, không tích hợp cổng thanh toán                          |
| Kiểm thử                    | Thủ công                                                | NgườI dùng không yêu cầu unit test                                |

## Kiến trúc

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  CartController │────▶│   ICartService   │────▶│   ICartStore    │
└─────────────────┘     └──────────────────┘     └────────┬────────┘
                                                          │
                              ┌───────────────────────────┼───────────────────────────┐
                              ▼                           ▼                           ▼
                    ┌──────────────────┐        ┌──────────────────┐        ┌──────────────────┐
                    │  SessionCartStore │        │   DbCartStore    │        │  IStoreRepository │
                    └──────────────────┘        └──────────────────┘        └──────────────────┘
                              │                           │                           │
                              ▼                           ▼                           ▼
                         HttpContext.Session          StoreDbContext               EFStoreRepository
```

- `Cart` là DTO dùng chung cho cả session và DB.
- `CartService` chứa toàn bộ business logic. Dùng `IHttpContextAccessor` để kiểm tra trạng thái đăng nhập: nếu đã đăng nhập thì dùng `DbCartStore`, ngược lại dùng `SessionCartStore`.
- `CartController` chỉ điều phối, không chứa logic lưu trữ.

## Models

### CartLine (DTO)

```csharp
public class CartLine
{
    public long ProductID { get; set; }
    public int Quantity { get; set; }
    public Product? Product { get; set; }
}
```

### Cart (DTO)

```csharp
public class Cart
{
    public List<CartLine> Lines { get; set; } = new();
    public decimal ComputeTotalValue() => Lines.Sum(l => l.Quantity * (l.Product?.Price ?? 0));
}
```

### CartItem (EF entity)

```csharp
public class CartItem
{
    public int CartItemId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public long ProductId { get; set; }
    public int Quantity { get; set; }
}
```

### Order (EF entity)

```csharp
public class Order
{
    public long OrderID { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime OrderedAt { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    public List<OrderLine> Lines { get; set; } = new();
}
```

### OrderLine (EF entity)

```csharp
public class OrderLine
{
    public long OrderLineID { get; set; }
    public long OrderID { get; set; }
    public long ProductID { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; } // Giá tại thờI điểm mua
}
```

## Interfaces

### ICartStore

```csharp
public interface ICartStore
{
    Task<Cart> GetAsync();
    Task SaveAsync(Cart cart);
    Task ClearAsync();
}
```

### ICartService

```csharp
public interface ICartService
{
    Task AddToCart(long productId, int quantity);
    Task RemoveLine(long productId);
    Task IncreaseQuantity(long productId);
    Task DecreaseQuantity(long productId);
    Task<Cart> GetCartAsync();
    Task ClearAsync();
    Task MergeSessionToDbAsync();
}
```

## Components cần tạo/sửa

### Tạo mới

- `Models/Cart.cs`
- `Models/CartLine.cs`
- `Models/CartItem.cs`
- `Models/Order.cs`
- `Models/OrderLine.cs`
- `Models/ICartStore.cs`
- `Models/SessionCartStore.cs`
- `Models/DbCartStore.cs`
- `Models/ICartService.cs`
- `Models/CartService.cs`
- `Controllers/CartController.cs`
- `Controllers/OrderController.cs`
- `Views/Cart/Index.cshtml`
- `Views/Cart/Checkout.cshtml`
- `Views/Cart/Completed.cshtml`
- `Views/Order/List.cshtml`

### Sửa đổi

- `Models/StoreDbContext.cs`: thêm `DbSet<CartItem>`, `DbSet<Order>`, `DbSet<OrderLine>`
- `Program.cs`:
  - Thêm `builder.Services.AddSession()` (và `AddDistributedMemoryCache()` nếu cần)
  - Đăng ký `IHttpContextAccessor`
  - Đăng ký `ICartService` / `ICartStore`
  - Thêm `app.UseSession()` trước `UseAuthentication`
- `Controllers/AccountController.cs`: gọi `CartService.MergeSessionToDbAsync()` sau khi đăng nhập thành công
- `Views/Shared/_Layout.cshtml`: thêm icon giỏ hàng với badge
- `Views/Shared/ProductSummary.cshtml`: thêm form thêm vào giỏ
- `Views/Home/Details.cshtml`: thêm form thêm vào giỏ

## Luồng dữ liệu

### Khách vãng lai thêm sản phẩm

1. NgườI dùng nhấn "Thêm vào giỏ" từ `Index` hoặc `Details`.
2. `CartController.AddToCart` gọi `CartService.AddToCart(productId, quantity)`.
3. `CartService` phát hiện chưa đăng nhập, dùng `SessionCartStore`.
4. `SessionCartStore` đọc `Cart` từ session, cập nhật, lưu lại dưới dạng JSON.

### Khách đăng nhập

1. `AccountController.Login` xác thực thành công.
2. Gọi `CartService.MergeSessionToDbAsync()`.
3. Đọc `Cart` từ session, chuyển thành các `CartItem` gắn với `UserId`.
4. Lưu `CartItem` vào DB qua `DbCartStore`.
5. Xóa session giỏ hàng.

### NgườI dùng đăng nhập thêm sản phẩm

1. `CartService` phát hiện đã đăng nhập, dùng `DbCartStore`.
2. `DbCartStore` query/cập nhật `CartItem` trong `StoreDbContext`.

### Xem giỏ hàng

1. `CartController.Index` gọi `CartService.GetCartAsync()`.
2. Service dùng store phù hợp lấy `Cart`.
3. Service load thông tin `Product` từ `IStoreRepository` để hiển thị.
4. Trả view `Cart/Index`.

### Thanh toán

1. `CartController.Checkout` GET kiểm tra giỏ rỗng.
2. Nếu rỗng redirect về `Index`.
3. Hiển thị form thông tin giao hàng.
4. POST validate form.
5. Tạo `Order` và các `OrderLine` từ giỏ hiện tại.
6. Lưu vào DB.
7. Gọi `CartService.ClearAsync()`.
8. Redirect `Cart/Completed`.

### Lịch sử đơn hàng

1. `OrderController.List` query `Order` theo `UserId` hiện tại.
2. Trả view `Order/List`.

## Xử lý lỗi

- Sản phẩm không tồn tại khi thêm: redirect về trang chủ, không lưu.
- Số lượng < 1: tự động điều chỉnh thành 1 hoặc xóa dòng nếu giảm từ 1.
- Giỏ rỗng khi thanh toán: redirect về `Cart/Index` với thông báo.
- Chưa đăng nhập khi thanh toán: redirect đến `Account/Login`, sau login merge giỏ.
- Lỗi lưu đơn hàng: hiển thị lỗi qua `ModelState`, giữ nguyên giỏ hàng.
- Session hết hạn: giỏ trống, ứng dụng không crash.

## Kiểm thử

Kiểm tra thủ công các luồng:

1. Thêm sản phẩm từ danh sách và chi tiết.
2. Badge trên header cập nhật đúng tổng số lượng.
3. Tăng/giảm số lượng và xóa sản phẩm trong giỏ.
4. Khách đăng nhập: giỏ trong session merge vào DB.
5. Thanh toán: tạo đơn hàng, xóa giỏ, hiển thị trang Completed.
6. Lịch sử đơn hàng hiển thị đúng đơn của user đăng nhập.
