using DoAnLTW.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DoAnLTW.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class SizeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SizeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Danh sách kích thước
        public async Task<IActionResult> Index()
        {
            var sizes = await _context.Sizes.ToListAsync();
            return View(sizes);
        }

        // 2. Xem chi tiết kích thước
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound("Không tìm thấy kích thước");
            }

            var size = await _context.Sizes
                .Include(s => s.ProductSizes)
                .FirstOrDefaultAsync(s => s.SizeId == id);

            if (size == null)
            {
                return NotFound("Không tìm thấy kích thước");
            }

            return View(size);
        }

        // 3. Thêm kích thước - GET
        public IActionResult Create()
        {
            return View();
        }

        // 4. Thêm kích thước - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Size sizes)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(sizes);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Thêm kích thước thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Lỗi khi thêm kích thước: {ex.Message}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
            }
            return View(sizes);
        }

        // 5. Sửa kích thước - GET
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound("Không tìm thấy kích thước");
            }

            var size = await _context.Sizes.FindAsync(id);
            if (size == null)
            {
                return NotFound("Không tìm thấy kích thước");
            }
            return View(size);
        }

        // 6. Sửa kích thước - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Size sizes)
        {
            if (id != sizes.SizeId)
            {
                return NotFound("Kích thước không hợp lệ");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(sizes);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật kích thước thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await SizeExists(sizes.SizeId))
                    {
                        return NotFound("Kích thước không tồn tại");
                    }
                    TempData["ErrorMessage"] = "Lỗi đồng bộ hóa dữ liệu. Vui lòng thử lại.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Lỗi khi cập nhật kích thước: {ex.Message}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
            }
            return View(sizes);
        }

        // 7. Xóa kích thước - GET
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID kích thước không hợp lệ.";
                return NotFound();
            }

            var size = await _context.Sizes
                .Include(s => s.ProductSizes)
                .FirstOrDefaultAsync(s => s.SizeId == id);

            if (size == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy kích thước.";
                return NotFound();
            }

            return View(size);
        }

        // 8. Xóa kích thước - POST
        [HttpPost]
      
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var size = await _context.Sizes
                .Include(s => s.ProductSizes)
                .FirstOrDefaultAsync(s => s.SizeId == id);

            if (size == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy kích thước để xóa.";
                return NotFound();
            }

            if (size.ProductSizes != null && size.ProductSizes.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa kích thước vì nó đang được sử dụng trong các sản phẩm.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Sizes.Remove(size);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa kích thước thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xóa kích thước: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> SizeExists(int id)
        {
            return await _context.Sizes.AnyAsync(s => s.SizeId == id);
        }
    }
}