using DoAnLTW.Models;
using DoAnLTW.Models.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DoAnLTW.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BrandController : Controller
    {
        private readonly IBrandRepository _brandRepository;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public BrandController(ApplicationDbContext context, IBrandRepository brandRepository, IWebHostEnvironment webHostEnvironment)
        {
            _brandRepository = brandRepository;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // Hiển thị danh sách thương hiệu
        public async Task<IActionResult> Index()
        {
            var brands = await _brandRepository.GetAllAsync();
            return View(brands);
        }

        // Hiển thị form thêm thương hiệu
        public IActionResult Create()
        {
            return View();
        }

        // Xử lý thêm thương hiệu với upload ảnh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brand brand, IFormFile ImageFile)
        {
            if (ModelState.IsValid)
            {
                // Nếu có ảnh, lưu ảnh
                brand.ImageUrl = await SaveImage(ImageFile);

                _context.Brands.Add(brand);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(brand);
        }

        private async Task<string> SaveImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return null;
            }

            string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "img");
            Console.WriteLine("Upload Path: " + uploadDir);

            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
            string savePath = Path.Combine(uploadDir, uniqueFileName);

            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            return "/img/" + uniqueFileName;
        }

        // Hiển thị form chỉnh sửa thương hiệu
        public async Task<IActionResult> Edit(int id)
        {
            var brand = await _brandRepository.GetByIdAsync(id);
            if (brand == null)
            {
                return NotFound();
            }
            return View(brand);
        }

        // Xử lý chỉnh sửa thương hiệu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, IFormFile? ImageFile, Brand updatedBrand)
        {
            if (id != updatedBrand.BrandId)
            {
                return NotFound();
            }

            var existingBrand = await _brandRepository.GetByIdAsync(id);
            if (existingBrand == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Cập nhật các trường cần thiết
                existingBrand.Name = updatedBrand.Name;

                // Nếu có ảnh mới, xóa ảnh cũ và cập nhật ảnh mới
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(existingBrand.ImageUrl))
                    {
                        string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existingBrand.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    existingBrand.ImageUrl = await SaveImage(ImageFile);
                }

                await _brandRepository.UpdateAsync(existingBrand);
                return RedirectToAction(nameof(Index));
            }

            return View(updatedBrand);
        }

        // Xác nhận xóa thương hiệu
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _brandRepository.GetByIdAsync(id);
            if (brand == null)
            {
                return NotFound();
            }

            return View(brand);
        }

        // Xử lý xóa thương hiệu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var brand = await _brandRepository.GetByIdAsync(id);
            if (brand == null)
            {
                return NotFound();
            }

            var hasProducts = await _context.Products.AnyAsync(p => p.BrandId == id);
            if (hasProducts)
            {
                TempData["ErrorMessage"] = "Không thể xóa thương hiệu vì còn sản phẩm liên quan.";
                return RedirectToAction("Index");
            }

            // Xóa ảnh nếu có
            if (!string.IsNullOrEmpty(brand.ImageUrl))
            {
                string filePath = Path.Combine(_webHostEnvironment.WebRootPath, brand.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            await _brandRepository.DeleteAsync(brand.BrandId);
            TempData["SuccessMessage"] = "Xóa thương hiệu thành công!";
            return RedirectToAction("Index");
        }
    }
}