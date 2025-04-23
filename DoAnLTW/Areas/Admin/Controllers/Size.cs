using DoAnLTW.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DoAnLTW.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SizeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SizeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Size
        public async Task<IActionResult> Index()
        {
            return View(await _context.Sizes.ToListAsync());
        }

        // GET: /Admin/Size/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Admin/Size/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Size size)
        {
            if (ModelState.IsValid)
            {
                _context.Add(size);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(size);
        }

        // GET: /Admin/Size/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var size = await _context.Sizes.FindAsync(id);
            if (size == null) return NotFound();

            return View(size);
        }

        // POST: /Admin/Size/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Size size)
        {
            if (id != size.SizeId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(size);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SizeExists(size.SizeId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(size);
        }

        // GET: /Admin/Size/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var size = await _context.Sizes
                .FirstOrDefaultAsync(m => m.SizeId == id);

            if (size == null) return NotFound();

            return View(size);
        }

        // POST: /Admin/Size/Delete/5
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var size = await _context.Sizes.FindAsync(id);
            if (size == null) return NotFound();

            // Kiểm tra xem kích thước có đang được sử dụng trong ProductSizes không
            var hasProductSizes = await _context.ProductSizes.AnyAsync(ps => ps.SizeId == id);
            if (hasProductSizes)
            {
                TempData["ErrorMessage"] = "Không thể xóa kích thước vì đang được sử dụng trong sản phẩm.";
                return RedirectToAction(nameof(Index));
            }

            _context.Sizes.Remove(size);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa kích thước thành công!";
            return RedirectToAction(nameof(Index));
        }

        private bool SizeExists(int id)
        {
            return _context.Sizes.Any(e => e.SizeId == id);
        }
    }
}