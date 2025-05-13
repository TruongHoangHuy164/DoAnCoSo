using DoAnLTW.Models;
using DoAnLTW.Models.Repositories;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize(Roles = "Admin,Employee")]
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CategoryController(ApplicationDbContext context, ICategoryRepository categoryRepository, IWebHostEnvironment webHostEnvironment)
        {
            _categoryRepository = categoryRepository;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // Hiển thị danh sách danh mục
        public async Task<IActionResult> Index()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return View(categories);
        }

        // Hiển thị form thêm danh mục
        public IActionResult Create()
        {
            return View();
        }

        // Xử lý thêm danh mục với upload ảnh

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category, IFormFile ImageFile)
        {
           
                // Nếu có ảnh, lưu ảnh
                category.ImageUrl = await SaveImage(ImageFile);

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            

            return View(category);
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




        // Hiển thị form chỉnh sửa danh mục
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, IFormFile? ImageFile, Category updatedCategory)
        {
            var existingCategory = await _categoryRepository.GetByIdAsync(id);
            if (existingCategory == null)
            {
                return NotFound();
            }

            // Cập nhật các trường cần thiết
            existingCategory.Name = updatedCategory.Name;

            // Nếu có ảnh mới, xóa ảnh cũ và cập nhật ảnh mới
            if (ImageFile != null && ImageFile.Length > 0)
            {
                // Xóa ảnh cũ nếu có
                if (!string.IsNullOrEmpty(existingCategory.ImageUrl))
                {
                    string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existingCategory.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Lưu ảnh mới và cập nhật URL
                existingCategory.ImageUrl = await SaveImage(ImageFile);
            }

            // Cập nhật danh mục trong cơ sở dữ liệu
            await _categoryRepository.UpdateAsync(existingCategory);
            return RedirectToAction(nameof(Index));
        }




        // Xác nhận xóa danh mục
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) return NotFound();

            return View(category);
        }

        // Xử lý xóa danh mục
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) return NotFound();

            var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);
            if (hasProducts)
            {
                TempData["ErrorMessage"] = "Không thể xóa danh mục vì còn sản phẩm liên quan.";
                return RedirectToAction("Index");
            }

            // Xóa ảnh nếu có
            if (!string.IsNullOrEmpty(category.ImageUrl))
            {
                string filePath = Path.Combine(_webHostEnvironment.WebRootPath, category.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            await _categoryRepository.DeleteAsync(category.CategoryId);
            TempData["SuccessMessage"] = "Xóa danh mục thành công!";
            return RedirectToAction("Index");
        }
       

    }
}
