using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnLTW.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using DoAnLTW.Models.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace DoAnLTW.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
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
            var products = await _productRepository.GetAllAsync();
            return View(products);
        }

        // 2. Xem chi tiết sản phẩm
        public async Task<IActionResult> Details(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound("Không tìm thấy sản phẩm");
            }
            return View(product);
        }

        // 3. Thêm sản phẩm - GET
        public async Task<IActionResult> Create()
        {
            await PopulateDropdownLists();
            return View(new Product());
        }

        // 4. Thêm sản phẩm - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, List<int> selectedSizes, List<int> sizeQuantities, List<decimal> sizePrices, List<IFormFile>? imageFiles)
        {
            if (!IsValidSizeData(selectedSizes, sizeQuantities, sizePrices) || !ModelState.IsValid)
            {
                await PopulateDropdownLists();
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return View(product);
            }

            try
            {
                // Thêm sản phẩm
                await _productRepository.AddAsync(product);

                // Thêm ProductSizes
                for (int i = 0; i < selectedSizes.Count; i++)
                {
                    await _productRepository.AddProductSizeAsync(new ProductSize
                    {
                        ProductId = product.ProductId,
                        SizeId = selectedSizes[i],
                        Stock = sizeQuantities[i],
                        Price = sizePrices[i]
                    });
                }

                // Thêm hình ảnh nếu có
                if (imageFiles?.Any() == true)
                {
                    foreach (var image in imageFiles)
                    {
                        string imageUrl = await SaveImage(image);
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            await _productRepository.AddProductImageAsync(new Product_Images
                            {
                                ProductId = product.ProductId,
                                ImageUrl = imageUrl
                            });
                        }
                    }
                }

                TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await PopulateDropdownLists();
                TempData["ErrorMessage"] = $"Lỗi khi thêm sản phẩm: {ex.Message}";
                return View(product);
            }
        }

        // 5. Sửa sản phẩm - GET
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound("Không tìm thấy sản phẩm");
            }

            await PopulateDropdownLists(product.CategoryId, product.BrandId);
            return View(product);
        }

        // 6. Sửa sản phẩm - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, List<IFormFile>? imageFiles, List<int>? selectedSizes, List<int>? sizeQuantities, List<decimal>? sizePrices)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            if (!IsValidSizeData(selectedSizes, sizeQuantities, sizePrices) || !ModelState.IsValid)
            {
                await PopulateDropdownLists(product.CategoryId, product.BrandId);
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return View(product);
            }

            try
            {
                // Cập nhật sản phẩm
                await _productRepository.UpdateAsync(product);

                // Cập nhật hình ảnh nếu có
                if (imageFiles?.Any() == true)
                {
                    await _productRepository.DeleteProductImagesAsync(id);
                    foreach (var image in imageFiles)
                    {
                        string imageUrl = await SaveImage(image);
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            await _productRepository.AddProductImageAsync(new Product_Images
                            {
                                ProductId = id,
                                ImageUrl = imageUrl
                            });
                        }
                    }
                }

                // Cập nhật ProductSizes
                if (selectedSizes?.Any() == true)
                {
                    await _productRepository.DeleteProductSizesAsync(id);
                    for (int i = 0; i < selectedSizes.Count; i++)
                    {
                        await _productRepository.AddProductSizeAsync(new ProductSize
                        {
                            ProductId = id,
                            SizeId = selectedSizes[i],
                            Stock = sizeQuantities[i],
                            Price = sizePrices[i]
                        });
                    }
                }

                TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await PopulateDropdownLists(product.CategoryId, product.BrandId);
                TempData["ErrorMessage"] = $"Lỗi khi cập nhật sản phẩm: {ex.Message}";
                return View(product);
            }
        }

        // 7. Xóa hình ảnh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var image = await _context.ProductImages.FindAsync(imageId);
            if (image == null)
            {
                return NotFound();
            }

            try
            {
                await DeleteImageFile(image.ImageUrl);
                await _productRepository.DeleteProductImageAsync(imageId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi xóa ảnh: {ex.Message}" });
            }
        }

        // 8. Xóa sản phẩm - GET
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // 9. Xóa sản phẩm - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var product = await _productRepository.GetByIdAsync(id);
                if (product == null)
                {
                    return NotFound();
                }

                // Xóa hình ảnh
                foreach (var img in product.Images)
                {
                    await DeleteImageFile(img.ImageUrl);
                }

                await _productRepository.DeleteAsync(id);
                TempData["SuccessMessage"] = "Xóa sản phẩm thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xóa sản phẩm: {ex.Message}";
                return RedirectToAction("Delete", new { id });
            }
        }

        // Hàm hỗ trợ
        private async Task PopulateDropdownLists(int? selectedCategoryId = null, int? selectedBrandId = null)
        {
            ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "CategoryId", "Name", selectedCategoryId);
            ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "BrandId", "Name", selectedBrandId);
            ViewBag.Sizes = await _context.Sizes.ToListAsync();
        }

        private bool IsValidSizeData(List<int> selectedSizes, List<int> sizeQuantities, List<decimal> sizePrices)
        {
            if (selectedSizes == null || sizeQuantities == null || sizePrices == null)
                return false;

            return selectedSizes.Count > 0 &&
                   selectedSizes.Count == sizeQuantities.Count &&
                   selectedSizes.Count == sizePrices.Count &&
                   sizeQuantities.All(q => q >= 0) &&
                   sizePrices.All(p => p >= 0);
        }

        private async Task<string> SaveImage(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(imageFile.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
                throw new InvalidOperationException("Định dạng ảnh không được hỗ trợ.");

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "img/products");
            Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + extension;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return "/img/products/" + uniqueFileName;
        }

        private async Task DeleteImageFile(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return;

            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, imageUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                await Task.Run(() => System.IO.File.Delete(filePath));
            }
        }
    }
}