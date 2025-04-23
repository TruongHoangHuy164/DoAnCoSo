using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using DoAnLTW.Models;

namespace DoAnLTW.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductSizeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductSizeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var productSizes = await _context.ProductSizes
                .Include(ps => ps.Product)
                .Include(ps => ps.Size)
                .ToListAsync();

            return View(productSizes);
        }
        // GET: Form thêm size
        public IActionResult Create()
        {
            ViewBag.Products = _context.Products.ToList();
            ViewBag.Sizes = _context.Sizes.ToList();
            return View();
        }

        // POST: Xử lý thêm size
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductSize productSize)
        {
            if (ModelState.IsValid)
            {
                _context.ProductSizes.Add(productSize);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(productSize);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int id, int change)
        {
            var productSize = await _context.ProductSizes.FindAsync(id);
            if (productSize == null)
            {
                return NotFound();
            }

            productSize.Stock += change;
            if (productSize.Stock < 0)
            {
                productSize.Stock = 0; // Không cho âm số lượng
            }

            _context.Update(productSize);
            await _context.SaveChangesAsync();

            return Json(new { success = true, quantity = productSize.Stock });
        }

        // GET: Form sửa số lượng
        public async Task<IActionResult> Edit(int id)
        {
            var productSize = await _context.ProductSizes.FindAsync(id);
            if (productSize == null) return NotFound();

            return View(productSize);
        }

        // POST: Cập nhật số lượng tồn kho
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductSize productSize)
        {
            if (id != productSize.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(productSize);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(productSize);
        }

        // Xóa size khỏi sản phẩm
        public async Task<IActionResult> Delete(int id)
        {
            var productSize = await _context.ProductSizes.FindAsync(id);
            if (productSize == null) return NotFound();

            _context.ProductSizes.Remove(productSize);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }

}
