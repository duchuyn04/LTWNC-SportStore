using SportsStore.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SportsStore.Models;
using SportsStore.Models.ViewModels;

namespace SportsStore.Areas.Admin.Controllers
{
    // Controller quản trị trong area Admin: chỉ cho phép user thuộc role Admin truy cập.
    // Quản lý sản phẩm (CRUD), xử lý ảnh và xem đơn hàng.
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly StoreDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminController(StoreDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Danh sách sản phẩm có phân trang, sắp xếp theo ID giảm dần.
        public IActionResult Index(int page = 1)
        {
            // Truy vấn sản phẩm theo thứ tự ID mới nhất, phân trang theo tham số page.
            var products = _context.Products
                .OrderByDescending(p => p.ProductID)
                .Skip((page - 1) * Setting.PageSize)
                .Take(Setting.PageSize)
                .ToList();

            // Tạo thông tin phân trang để view hiển thị các nút chuyển trang.
            ViewBag.PagingInfo = new PagingInfo
            {
                TotalItems = _context.Products.Count(),
                ItemsPerPage = Setting.PageSize,
                CurrentPage = page
            };

            return View(products);
        }

        // Hiển thị form thêm sản phẩm mới.
        [HttpGet]
        public IActionResult Create()
        {
            SetCategoryList();
            return View(new Product());
        }

        // Xử lý thêm sản phẩm: cho phép nhập danh mục mới, upload nhiều ảnh và chọn ảnh chính.
        [HttpPost]
        public async Task<IActionResult> Create(Product product, string? newCategory, List<IFormFile> imageFiles)
        {
            // Nếu admin nhập danh mục mới thì ưu tiên dùng giá trị đó thay vì chọn từ dropdown.
            if (!string.IsNullOrEmpty(newCategory))
            {
                product.Category = newCategory;
            }

            // Xử lý upload ảnh và ghi nhận lỗi nếu có file không hợp lệ.
            var uploadedImages = await ProcessUploadedImagesAsync(imageFiles);
            if (uploadedImages.ErrorMessage != null)
            {
                ModelState.AddModelError("imageFiles", uploadedImages.ErrorMessage);
            }

            // Khi toàn bộ dữ liệu hợp lệ thì lưu sản phẩm cùng ảnh vào database.
            if (ModelState.IsValid)
            {
                // Gắn các ảnh đã upload vào sản phẩm.
                foreach (var img in uploadedImages.Images)
                {
                    product.Images.Add(img);
                }

                // Chọn ảnh chính làm đại diện cho sản phẩm.
                product.ImagePro = product.Images.FirstOrDefault(i => i.IsPrimary)?.ImagePath;

                // Lưu sản phẩm và ảnh, sau đó chuyển về danh sách với thông báo thành công.
                _context.Products.Add(product);
                _context.SaveChanges();
                TempData["SuccessMessage"] = $"Thêm sản phẩm thành công! ({uploadedImages.Images.Count} ảnh)";
                return RedirectToAction(nameof(Index));
            }

            // Nếu validate thất bại thì nạp lại dropdown danh mục và giữ nguyên dữ liệu đã nhập.
            SetCategoryList();
            return View(product);
        }

        // Hiển thị form chỉnh sửa sản phẩm kèm ảnh hiện có.
        [HttpGet]
        public IActionResult Edit(long id)
        {
            var product = _context.Products
                .Include(p => p.Images)
                .FirstOrDefault(p => p.ProductID == id);
            if (product == null)
            {
                return NotFound();
            }

            SetCategoryList();
            return View(product);
        }

        // Cập nhật sản phẩm: xóa ảnh được chọn, thêm ảnh mới, đảm bảo có ít nhất một ảnh chính.
        [HttpPost]
        public async Task<IActionResult> Edit(Product product, string? newCategory, List<IFormFile> imageFiles, long[]? removeImageIds)
        {
            // Ưu tiên danh mục mới nếu admin nhập tay thay vì chọn từ danh sách.
            if (!string.IsNullOrEmpty(newCategory))
            {
                product.Category = newCategory;
            }

            // Lấy sản phẩm hiện có kèm danh sách ảnh để cập nhật từng trường.
            var existing = _context.Products
                .Include(p => p.Images)
                .FirstOrDefault(p => p.ProductID == product.ProductID);
            if (existing == null)
            {
                return NotFound();
            }

            // Xử lý ảnh mới upload; nếu có lỗi thì đưa vào ModelState để hiển thị.
            var uploadedImages = await ProcessUploadedImagesAsync(imageFiles);
            if (uploadedImages.ErrorMessage != null)
            {
                ModelState.AddModelError("imageFiles", uploadedImages.ErrorMessage);
            }

            // Khi dữ liệu hợp lệ thì tiến hành cập nhật sản phẩm.
            if (ModelState.IsValid)
            {
                // Xóa ảnh được chọn và xóa luôn file vật lý trên đĩa.
                if (removeImageIds != null && removeImageIds.Length > 0)
                {
                    var toRemove = existing.Images.Where(i => removeImageIds.Contains(i.ProductImageId)).ToList();
                    foreach (var img in toRemove)
                    {
                        DeleteProductImage(img.ImagePath);
                        _context.ProductImages.Remove(img);
                    }
                }

                // Thêm ảnh vừa upload vào sản phẩm.
                foreach (var img in uploadedImages.Images)
                {
                    existing.Images.Add(img);
                }

                // Nếu không còn ảnh chính, chọn ảnh đầu tiên làm ảnh đại diện.
                if (!existing.Images.Any(i => i.IsPrimary))
                {
                    var first = existing.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault();
                    if (first != null)
                    {
                        first.IsPrimary = true;
                    }
                }

                // Sắp xếp lại thứ tự hiển thị cho liên tục.
                var ordered = existing.Images.OrderBy(i => i.DisplayOrder).ToList();
                for (int i = 0; i < ordered.Count; i++)
                {
                    ordered[i].DisplayOrder = i;
                }

                // Cập nhật các thuộc tính cơ bản và ảnh đại diện, rồi lưu thay đổi.
                existing.ImagePro = existing.Images.FirstOrDefault(i => i.IsPrimary)?.ImagePath;
                existing.Name = product.Name;
                existing.Description = product.Description;
                existing.Price = product.Price;
                existing.Category = product.Category;
                _context.SaveChanges();

                TempData["SuccessMessage"] = $"Cập nhật sản phẩm thành công! ({uploadedImages.Images.Count} ảnh mới)";
                return RedirectToAction(nameof(Index));
            }

            // Giữ lại ảnh để preview khi validate thất bại.
            product.Images = existing.Images.ToList();
            product.ImagePro = existing.ImagePro;
            SetCategoryList();
            return View(product);
        }

        // Xóa sản phẩm kèm ảnh; báo lỗi nếu sản phẩm đang được tham chiếu bởi đơn hàng hoặc giỏ hàng.
        [HttpPost]
        public IActionResult Delete(long id)
        {
            // Tìm sản phẩm cần xóa, kèm theo danh sách ảnh để xóa file vật lý.
            var product = _context.Products
                .Include(p => p.Images)
                .FirstOrDefault(p => p.ProductID == id);
            if (product == null)
            {
                return NotFound();
            }

            try
            {
                // Xóa từng file ảnh trên đĩa trước khi xóa bản ghi trong database.
                foreach (var img in product.Images.ToList())
                {
                    DeleteProductImage(img.ImagePath);
                }

                // Xóa sản phẩm và lưu thay đổi, sau đó thông báo thành công.
                _context.Products.Remove(product);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa sản phẩm.";
            }
            catch (DbUpdateException)
            {
                // Bắt lỗi khóa ngoại khi sản phẩm đang được tham chiếu bởi giỏ hàng hoặc đơn hàng.
                TempData["ErrorMessage"] =
                    "Không thể xóa sản phẩm vì đang được dùng trong giỏ hàng hoặc đơn hàng.";
            }

            // Quay lại danh sách sản phẩm dù thành công hay thất bại.
            return RedirectToAction(nameof(Index));
        }

        // Liệt kê tất cả đơn hàng, sắp xếp mới nhất lên đầu.
        public async Task<IActionResult> Orders()
        {
            // Truy vấn toàn bộ đơn hàng, kèm chi tiết từng dòng và sản phẩm tương ứng.
            var orders = await _context.Orders
                .Include(o => o.Lines)
                .ThenInclude(l => l.Product)
                .OrderByDescending(o => o.OrderedAt)
                .ToListAsync();

            return View(orders);
        }

        // Xem chi tiết một đơn hàng theo ID.
        public async Task<IActionResult> OrderDetails(long id)
        {
            // Tìm đơn hàng theo ID, kèm chi tiết các dòng và thông tin sản phẩm.
            var order = await _context.Orders
                .Include(o => o.Lines)
                .ThenInclude(l => l.Product)
                .FirstOrDefaultAsync(o => o.OrderID == id);

            // Trả về 404 nếu không tìm thấy đơn hàng.
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // Chuẩn bị danh sách danh mục để đổ vào dropdown trong view.
        private void SetCategoryList()
        {
            // Lấy các danh mục duy nhất từ bảng sản phẩm.
            var names = _context.Products
                .Select(p => p.Category)
                .Distinct()
                .ToList();

            // Mapping thành danh sách Category có ID tăng dần để view sử dụng.
            var list = names.Select((name, index) => new Category
            {
                Idcate = index + 1,
                NameCate = name
            }).ToList();

            // Lưu dưới dạng JSON vào TempData để dropdown đọc trong view.
            TempData["CategoryList"] = JsonSerializer.Serialize(list);
        }

        // Kiểm tra và lưu ảnh upload: giới hạn 5MB, định dạng cho phép, ảnh đầu tiên làm ảnh chính.
        private async Task<UploadResult> ProcessUploadedImagesAsync(List<IFormFile> imageFiles)
        {
            var result = new UploadResult();

            // Lọc ra các file thực sự được chọn, bỏ qua slot rỗng.
            var validFiles = imageFiles.Where(f => f != null && f.Length > 0).ToList();

            // Không có ảnh nào thì trả về kết quả rỗng.
            if (!validFiles.Any())
            {
                return result;
            }

            // Kiểm tra kích thước và định dạng từng file trước khi lưu.
            foreach (var file in validFiles)
            {
                if (file.Length > 5 * 1024 * 1024)
                {
                    result.ErrorMessage = $"Ảnh '{file.FileName}' vượt quá 5MB.";
                    return result;
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                if (!allowed.Contains(extension))
                {
                    result.ErrorMessage = $"Ảnh '{file.FileName}' không đúng định dạng (chỉ hỗ trợ JPG, PNG, GIF, WebP).";
                    return result;
                }
            }

            // Lưu từng ảnh hợp lệ; ảnh đầu tiên được đánh dấu là ảnh chính.
            int order = 0;
            foreach (var file in validFiles)
            {
                var path = await SaveCroppedSquareImageAsync(file, 800);
                result.Images.Add(new ProductImage
                {
                    ImagePath = path,
                    IsPrimary = order == 0,
                    DisplayOrder = order
                });
                order++;
            }

            return result;
        }

        // Cắt ảnh thành hình vuông 800x800, lưu vào wwwroot/images/products với tên file duy nhất.
        private async Task<string> SaveCroppedSquareImageAsync(IFormFile imageFile, int size)
        {
            // Đảm bảo thư mục lưu ảnh tồn tại trước khi ghi file.
            var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "products");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Làm sạch tên file và thêm GUID để tránh trùng tên khi nhiều sản phẩm upload cùng ảnh.
            var safeFileName = Path.GetFileNameWithoutExtension(imageFile.FileName)
                .Replace(" ", "-")
                .Replace(".", "-")
                .Replace("\\", "-")
                .Replace("/", "-");
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            var fileName = $"{safeFileName}-{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Mở file upload, cắt ảnh vuông từ trung tâm rồi lưu xuống đĩa.
            using (var inputStream = imageFile.OpenReadStream())
            using (var image = await Image.LoadAsync(inputStream))
            {
                image.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Crop,
                    Position = AnchorPositionMode.Center,
                    Size = new Size(size, size)
                }));

                await image.SaveAsync(filePath);
            }

            // Trả về đường dẫn tương đối để lưu vào database.
            return $"/images/products/{fileName}";
        }

        // Xóa file ảnh vật lý, bỏ qua nếu đường dẫn rỗng hoặc là URL bên ngoài.
        private void DeleteProductImage(string? imagePath)
        {
            // Không xóa nếu đường dẫn rỗng hoặc là ảnh từ URL bên ngoài.
            if (string.IsNullOrEmpty(imagePath) || imagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Chuyển đường dẫn tương đối thành đường dẫn tuyệt đối trong wwwroot.
            var relativePath = imagePath.TrimStart('/');
            var filePath = Path.Combine(_env.WebRootPath, relativePath);

            // Chỉ xóa nếu file thực sự tồn tại trên đĩa.
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        // Kết quả trả về khi xử lý upload ảnh: danh sách ảnh hợp lệ hoặc thông báo lỗi.
        private class UploadResult
        {
            public List<ProductImage> Images { get; set; } = new List<ProductImage>();
            public string? ErrorMessage { get; set; }
        }
    }
}
