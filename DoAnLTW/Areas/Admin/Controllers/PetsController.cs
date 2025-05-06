using DoAnLTW.Models;
using DoAnLTW.Models.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace DoAnLTW.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, Staff")]
    public class PetController : Controller
    {
        private readonly IPetRepository _petRepository;
        private readonly UserManager<IdentityUser> _userManager;  // Khai báo _userManager

        // Constructor: Tiêm UserManager và IPetRepository vào controller
        public PetController(IPetRepository petRepository, UserManager<IdentityUser> userManager)
        {
            _petRepository = petRepository;
            _userManager = userManager;  // Khởi tạo _userManager
        }

        // GET: Admin/Pet/Index
        public async Task<IActionResult> Index()
        {
            var pets = await _petRepository.GetAllAsync(); // Lấy danh sách thú cưng từ repository
            return View("~/Areas/Admin/Views/Pet/Index.cshtml", pets);
        }

        // GET: Admin/Pet/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Pet/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Pet pet)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _petRepository.AddAsync(pet);
                    TempData["SuccessMessage"] = "Thú cưng đã được thêm thành công.";
                    return RedirectToAction(nameof(Index));
                }
                return View(pet);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo thú cưng.";
                return View(pet);
            }
        }

        // GET: Admin/Pet/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var pet = await _petRepository.GetByIdAsync(id);
                if (pet == null)
                {
                    return NotFound();
                }
                return View(pet);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thú cưng để chỉnh sửa.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Pet/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Pet pet)
        {
            try
            {
                if (id != pet.PetId)
                {
                    return NotFound();
                }

                if (ModelState.IsValid)
                {
                    await _petRepository.UpdateAsync(pet);
                    TempData["SuccessMessage"] = "Thú cưng đã được cập nhật thành công.";
                    return RedirectToAction(nameof(Index));
                }
                return View(pet);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật thú cưng.";
                return View(pet);
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var pet = await _petRepository.GetByIdAsync(id);
                if (pet == null)
                {
                    return NotFound();
                }

                // Xóa thú cưng
                await _petRepository.DeleteAsync(pet.PetId);  // Truyền PetId vào thay vì Id

                TempData["SuccessMessage"] = "Thú cưng đã được xóa thành công!";
                return RedirectToAction(nameof(Index));  // Chuyển hướng về trang danh sách
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra khi xóa thú cưng: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }



        // GET: Admin/Pet/Detail/5
        public async Task<IActionResult> Detail(int id)
        {
            // Lấy thông tin thú cưng
            var pet = await _petRepository.GetByIdAsync(id);
            if (pet == null)
            {
                return NotFound();
            }

            // Lấy tên của chủ sở hữu từ UserManager
            var user = await _userManager.FindByIdAsync(pet.UserId);  // pet.UserId là Id của người sở hữu
            var ownerName = user?.UserName; // Nếu bạn có tên đầy đủ, bạn có thể thay UserName bằng trường FullName hoặc tên khác

            // Gán tên chủ sở hữu vào model để truyền vào view
            ViewBag.OwnerName = ownerName;

            return View(pet);
        }
    }
}
