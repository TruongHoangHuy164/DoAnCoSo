using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnLTW.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using DoAnLTW.Models.Repositories;

namespace DoAnLTW.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public ProductController(
     ApplicationDbContext context,
     IWebHostEnvironment webHostEnvironment,
     IProductRepository productRepository,
     ICategoryRepository categoryRepository)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        // 1. Danh sách sản phẩm
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                    .ThenInclude(ps => ps.Size)
                .Include(p => p.Brand)
                .ToListAsync();
            return View(products);
        }

        // 2. Xem chi tiết sản phẩm
        public IActionResult Details(int id)
        {
            var product = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.ProductSizes)
                    .ThenInclude(ps => ps.Size)
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return StatusCode(404, "Không tìm thấy sản phẩm");
            }

            return View(product);
        }

        // 3. Thêm sản phẩm - GET
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "Id", "Name");
            ViewBag.Sizes = await _context.Sizes.ToListAsync();
            return View();
        }

        // 4. Thêm sản phẩm - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, List<int> selectedSizes, List<int> sizeQuantities, List<decimal> sizePrices)
        {
            if (!ModelState.IsValid || selectedSizes == null || sizeQuantities == null || sizePrices == null || selectedSizes.Count != sizeQuantities.Count || selectedSizes.Count != sizePrices.Count)
            {
                ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
                ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "Id", "Name");
                ViewBag.Sizes = await _context.Sizes.ToListAsync();
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return View(product);
            }

            try
            {
                // Thêm sản phẩm vào database
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Thêm kích thước, số lượng tồn kho và giá
                for (int i = 0; i < selectedSizes.Count; i++)
                {
                    _context.ProductSizes.Add(new ProductSize
                    {
                        ProductId = product.ProductId,
                        SizeId = selectedSizes[i],
                        Stock = sizeQuantities[i],
                        Price = sizePrices[i]
                    });
                }
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Sản phẩm đã được thêm thành công! Hãy thêm hình ảnh.";
                return RedirectToAction("AddImages", new { productId = product.ProductId });
            }
            catch (Exception ex)
            {
                ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
                ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "Id", "Name");
                ViewBag.Sizes = await _context.Sizes.ToListAsync();
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi thêm sản phẩm: " + ex.Message;
                return View(product);
            }
        }

        // 5. Thêm hình ảnh - GET
        public async Task<IActionResult> AddImages(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại!";
                return RedirectToAction("Index");
            }

            ViewBag.ProductId = productId;
            return View();
        }

        // 6. Tải hình ảnh - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImages(int ProductId, List<IFormFile> ImageFiles)
        {
            if (ImageFiles != null && ImageFiles.Count > 0)
            {
                var imageList = new List<Product_Images>();
                foreach (var image in ImageFiles)
                {
                    string imageUrl = await SaveImage(image);
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        imageList.Add(new Product_Images
                        {
                            ProductId = ProductId,
                            ImageUrl = imageUrl
                        });
                    }
                }

                if (imageList.Count > 0)
                {
                    _context.ProductImages.AddRange(imageList);
                    await _context.SaveChangesAsync();
                }
            }

            TempData["SuccessMessage"] = "Sản phẩm đã được thêm thành công!";
            return RedirectToAction("Index");
        }

        // 7. Sửa sản phẩm - GET
        public IActionResult Edit(int id)
        {
            var product = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.ProductSizes)
                    .ThenInclude(ps => ps.Size)
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewBag.Brands = new SelectList(_context.Brands, "Id", "Name", product.BrandId);
            ViewBag.Sizes = _context.Sizes.ToList();
            return View(product);
        }

        // 8. Sửa sản phẩm - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, List<IFormFile> ImageFiles, List<int> selectedSizes, List<int> sizeQuantities, List<decimal> sizePrices)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid || (selectedSizes != null && (selectedSizes.Count != sizeQuantities.Count || selectedSizes.Count != sizePrices.Count)))
            {
                ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
                ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "Id", "Name", product.BrandId);
                ViewBag.Sizes = await _context.Sizes.ToListAsync();
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return View(product);
            }

            try
            {
                // Lấy sản phẩm hiện tại
                var existingProduct = await _context.Products
                    .Include(p => p.Images)
                    .Include(p => p.ProductSizes)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (existingProduct == null)
                {
                    return NotFound();
                }

                // Cập nhật thông tin sản phẩm
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.BrandId = product.BrandId;
                existingProduct.CategoryId = product.CategoryId;

                // Cập nhật ảnh nếu có
                if (ImageFiles != null && ImageFiles.Count > 0)
                {
                    foreach (var img in existingProduct.Images)
                    {
                        var filePath = Path.Combine(_webHostEnvironment.WebRootPath, img.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                    _context.ProductImages.RemoveRange(existingProduct.Images);

                    var imageList = new List<Product_Images>();
                    foreach (var image in ImageFiles)
                    {
                        string imageUrl = await SaveImage(image);
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            imageList.Add(new Product_Images { ProductId = existingProduct.ProductId, ImageUrl = imageUrl });
                        }
                    }
                    _context.ProductImages.AddRange(imageList);
                }

                // Cập nhật kích thước, số lượng tồn kho và giá
                if (selectedSizes != null && sizeQuantities != null && sizePrices != null)
                {
                    _context.ProductSizes.RemoveRange(existingProduct.ProductSizes);
                    for (int i = 0; i < selectedSizes.Count; i++)
                    {
                        _context.ProductSizes.Add(new ProductSize
                        {
                            ProductId = existingProduct.ProductId,
                            SizeId = selectedSizes[i],
                            Stock = sizeQuantities[i],
                            Price = sizePrices[i]
                        });
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
                ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "Id", "Name", product.BrandId);
                ViewBag.Sizes = await _context.Sizes.ToListAsync();
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật sản phẩm: " + ex.Message;
                return View(product);
            }
        }

        // 9. Xóa hình ảnh
        [HttpPost]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var image = await _context.ProductImages.FindAsync(imageId);
            if (image != null)
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, image.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }

                _context.ProductImages.Remove(image);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Edit", new { id = image.ProductId });
        }

        // 10. Xóa sản phẩm - GET
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // 11. Xóa sản phẩm - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            foreach (var img in product.Images)
            {
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, img.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                _context.ProductImages.Remove(img);
            }

            _context.ProductSizes.RemoveRange(product.ProductSizes);
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa sản phẩm thành công!";
            return RedirectToAction(nameof(Index));
        }

        // Hàm lưu ảnh
        private async Task<string> SaveImage(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return null;
            }

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "img/products");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return "/img/products/" + uniqueFileName;
        }
    }
}
