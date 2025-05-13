using DoAnLTW.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnLTW.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    public class PromotionCodeController : Controller
    {
        private readonly ApplicationDbContext _context;
        public PromotionCodeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/PromotionCode/Index
        public async Task<IActionResult> Index()
        {
            var promotions = await _context.PromotionCodes
                .OrderBy(p => p.IsActive ? 0 : 1)
                .ThenBy(p => p.StartDate)
                .ToListAsync();
            return View(promotions);
        }

        // GET: Admin/PromotionCode/Create
        public IActionResult Create()
        {
            return View(new PromotionCode());
        }

        // POST: Admin/PromotionCode/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PromotionCode promotion)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra mã khuyến mãi đã tồn tại
                if (await _context.PromotionCodes.AnyAsync(p => p.Code == promotion.Code))
                {
                    ModelState.AddModelError("Code", "Mã khuyến mãi đã tồn tại.");
                    return View(promotion);
                }

                // Đảm bảo ngày hợp lệ
                if (promotion.StartDate.HasValue && promotion.EndDate.HasValue && promotion.StartDate > promotion.EndDate)
                {
                    ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu.");
                    return View(promotion);
                }

                promotion.UsageCount = 0; // Khởi tạo số lần sử dụng
                _context.PromotionCodes.Add(promotion);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tạo mã khuyến mãi thành công.";
                return RedirectToAction(nameof(Index));
            }
            return View(promotion);
        }

        // GET: Admin/PromotionCode/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var promotion = await _context.PromotionCodes.FindAsync(id);
            if (promotion == null)
            {
                return NotFound();
            }
            return View(promotion);
        }

        // POST: Admin/PromotionCode/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PromotionCode promotion)
        {
            if (id != promotion.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra mã khuyến mãi trùng lặp (trừ mã hiện tại)
                    if (await _context.PromotionCodes.AnyAsync(p => p.Code == promotion.Code && p.Id != id))
                    {
                        ModelState.AddModelError("Code", "Mã khuyến mãi đã tồn tại.");
                        return View(promotion);
                    }

                    // Kiểm tra ngày hợp lệ
                    if (promotion.StartDate.HasValue && promotion.EndDate.HasValue && promotion.StartDate > promotion.EndDate)
                    {
                        ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu.");
                        return View(promotion);
                    }

                    _context.Update(promotion);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật mã khuyến mãi thành công.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.PromotionCodes.AnyAsync(p => p.Id == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }
            return View(promotion);
        }

        // POST: Admin/PromotionCode/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var promotion = await _context.PromotionCodes.FindAsync(id);
            if (promotion == null)
            {
                return NotFound();
            }

            _context.PromotionCodes.Remove(promotion);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa mã khuyến mãi thành công.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/PromotionCode/ToggleActive/5
        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var promotion = await _context.PromotionCodes.FindAsync(id);
            if (promotion == null)
            {
                return Json(new { success = false, message = "Không tìm thấy mã khuyến mãi." });
            }

            promotion.IsActive = !promotion.IsActive;
            _context.Update(promotion);
            await _context.SaveChangesAsync();
            return Json(new { success = true, isActive = promotion.IsActive });
        }
    }
}
