using DoAnLTW.Models;
using DoAnLTW.Models.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DoAnLTW.Controllers
{
    [Authorize] // Chỉ cho phép người dùng đã đăng nhập
    public class UserPetsController : Controller
    {
        private readonly IPetRepository _petRepository;

        public UserPetsController(IPetRepository petRepository)
        {
            _petRepository = petRepository;
        }

        // GET: UserPets
        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để thực hiện thao tác này.";
                return Redirect("/Identity/Account/Login?ReturnUrl=" + Url.Action("Index", "UserPets"));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var pets = await _petRepository.GetAllByUserIdAsync(userId);
            return View(pets);
        }
        // GET: UserPets/Detail/5
        public async Task<IActionResult> Detail(int id)
        {
            var pet = await _petRepository.GetByIdAsync(id);
            if (pet == null)
            {
                return NotFound(); // Nếu không tìm thấy thú cưng
            }

            // Kiểm tra xem thú cưng có thuộc về người dùng hiện tại không
            if (pet.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return NotFound(); // Nếu không phải là thú cưng của người dùng
            }

            return View(pet); // Trả về view với thông tin thú cưng
        }
        // GET: UserPets/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: UserPets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Pet pet)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để thực hiện thao tác này.";
                return RedirectToAction("Login", "Account");
            }

            pet.UserId = userId; // Gán UserId từ Claims
            Console.WriteLine($"UserId: {pet.UserId}"); // Để debug

            if (ModelState.IsValid)
            {
                await _petRepository.AddAsync(pet);
                TempData["SuccessMessage"] = "Thú cưng đã được thêm thành công!";
                return RedirectToAction(nameof(Index));
            }

            // Hiển thị lỗi cụ thể để debug
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            Console.WriteLine(string.Join(", ", errors)); // In lỗi ra console
            return View(pet);
        }

        // Trong action Edit (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var pet = await _petRepository.GetByIdAsync(id);
            if (pet == null)
            {
                Console.WriteLine($"Pet with ID {id} not found.");
                return NotFound();
            }
            if (pet.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                Console.WriteLine($"User mismatch. Pet UserId: {pet.UserId}, Current User: {User.FindFirstValue(ClaimTypes.NameIdentifier)}");
                return NotFound();
            }
            return View(pet);
        }

        // POST: UserPets/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Pet pet)
        {
            if (id != pet.PetId)
            {
                return NotFound();  // Nếu ID không trùng khớp, trả về lỗi NotFound
            }

            if (ModelState.IsValid)
            {
                await _petRepository.UpdateAsync(pet);  // Cập nhật thú cưng trong cơ sở dữ liệu
                TempData["SuccessMessage"] = "Cập nhật thú cưng thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(pet);  // Nếu model không hợp lệ, trả về form với dữ liệu và lỗi
        }

        // GET: UserPets/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var pet = await _petRepository.GetByIdAsync(id);
            if (pet == null || pet.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return NotFound();
            }
            return View(pet);
        }

        // POST: UserPets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pet = await _petRepository.GetByIdAsync(id);
            if (pet == null || pet.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return NotFound();
            }

            await _petRepository.DeleteAsync(id);
            TempData["SuccessMessage"] = "Xóa thú cưng thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}