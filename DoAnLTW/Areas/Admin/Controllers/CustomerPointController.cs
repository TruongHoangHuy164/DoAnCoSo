using DoAnLTW.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnLTW.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    public class CustomerPointController : Controller
    {
       
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        public CustomerPointController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/CustomerPoint/Index
        public async Task<IActionResult> Index()
        {
            var points = await _context.CustomerPoints
                .Include(cp => cp.Order)
                .OrderByDescending(cp => cp.EarnedDate)
                .ToListAsync();
            return View(points);
        }

        // POST: Admin/CustomerPoint/Edit
        [HttpPost]
        public async Task<IActionResult> Edit(int id, int points)
        {
            var customerPoint = await _context.CustomerPoints.FindAsync(id);
            if (customerPoint == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bản ghi điểm." });
            }

            if (points < 0)
            {
                return Json(new { success = false, message = "Điểm phải lớn hơn hoặc bằng 0." });
            }

            customerPoint.Points = points;
            _context.Update(customerPoint);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật điểm thành công." });
        }

        // POST: Admin/CustomerPoint/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var customerPoint = await _context.CustomerPoints.FindAsync(id);
            if (customerPoint == null)
            {
                return NotFound();
            }

            _context.CustomerPoints.Remove(customerPoint);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa bản ghi điểm thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}

