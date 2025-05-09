using DoAnLTW.Models;
using DoAnLTW.Models.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DoAnLTW.Controllers
{
    [Authorize] // Chỉ cho phép người dùng đã đăng nhập
    public class UserPetsController : Controller
    {
        private readonly IPetRepository _petRepository;
        private readonly ApplicationDbContext _context;

        public UserPetsController(IPetRepository petRepository, ApplicationDbContext context)
        {
            _petRepository = petRepository;
            _context = context;
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
            var pets = await _context.Pets
                .Where(p => p.UserId == userId)
                .Include(p => p.Images)
                .ToListAsync();

            // Log để debug
            foreach (var pet in pets)
            {
                Console.WriteLine($"Pet: {pet.Name}, Images: {(pet.Images != null ? pet.Images.Count : 0)}");
                if (pet.Images != null)
                {
                    foreach (var img in pet.Images)
                    {
                        Console.WriteLine($"Image: {img.ImageUrl}, IsMain: {img.IsMainImage}");
                    }
                }
            }

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

            // Load danh sách ảnh
            pet.Images = await _context.PetImages.Where(pi => pi.PetId == id).ToListAsync();
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
        public async Task<IActionResult> Create(Pet pet, IFormFile[] images)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để thực hiện thao tác này.";
                return RedirectToAction("Login", "Account");
            }

            pet.UserId = userId;
            Console.WriteLine($"UserId: {pet.UserId}"); // Để debug

            if (ModelState.IsValid)
            {
                await _petRepository.AddAsync(pet);

                // Xử lý upload ảnh
                if (images != null && images.Length > 0)
                {
                    foreach (var image in images)
                    {
                        if (image != null && image.Length > 0)
                        {
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/pets", fileName);

                            // Tạo thư mục nếu chưa tồn tại
                            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }

                            var petImage = new PetImages
                            {
                                PetId = pet.PetId,
                                ImageUrl = "/images/pets/" + fileName,
                                IsMainImage = !pet.Images.Any() // Ảnh đầu tiên là ảnh chính
                            };
                            await _context.PetImages.AddAsync(petImage);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Thú cưng đã được thêm thành công!";
                return RedirectToAction(nameof(Index));
            }

            // Hiển thị lỗi cụ thể để debug
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            Console.WriteLine(string.Join(", ", errors)); // In lỗi ra console
            return View(pet);
        }

        // GET: UserPets/Edit/5
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

            // Load danh sách ảnh
            pet.Images = await _context.PetImages.Where(pi => pi.PetId == id).ToListAsync();
            return View(pet);
        }

        // POST: UserPets/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Pet pet, IFormFile[] images, int[] deleteImageIds)
        {
            if (id != pet.PetId)
            {
                return NotFound(); // Nếu ID không trùng khớp
            }

            if (ModelState.IsValid)
            {
                await _petRepository.UpdateAsync(pet);

                // Xóa ảnh được chọn
                if (deleteImageIds != null && deleteImageIds.Length > 0)
                {
                    foreach (var imageId in deleteImageIds)
                    {
                        var image = await _context.PetImages.FindAsync(imageId);
                        if (image != null && image.PetId == pet.PetId)
                        {
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + image.ImageUrl);
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                            _context.PetImages.Remove(image);
                        }
                    }
                }

                // Thêm ảnh mới
                if (images != null && images.Length > 0)
                {
                    foreach (var image in images)
                    {
                        if (image != null && image.Length > 0)
                        {
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/pets", fileName);

                            // Tạo thư mục nếu chưa tồn tại
                            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }

                            var petImage = new PetImages
                            {
                                PetId = pet.PetId,
                                ImageUrl = "/images/pets/" + fileName,
                                IsMainImage = !pet.Images.Any(img => img.IsMainImage) // Nếu không còn ảnh chính
                            };
                            await _context.PetImages.AddAsync(petImage);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật thú cưng thành công!";
                return RedirectToAction(nameof(Index));
            }

            // Load lại danh sách ảnh nếu model không hợp lệ
            pet.Images = await _context.PetImages.Where(pi => pi.PetId == id).ToListAsync();
            return View(pet);
        }

        // GET: UserPets/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var pet = await _petRepository.GetByIdAsync(id);
            if (pet == null || pet.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return NotFound();
            }

            // Load danh sách ảnh
            pet.Images = await _context.PetImages.Where(pi => pi.PetId == id).ToListAsync();
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

            // Xóa các ảnh liên quan
            var images = await _context.PetImages.Where(pi => pi.PetId == id).ToListAsync();
            foreach (var image in images)
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + image.ImageUrl);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                _context.PetImages.Remove(image);
            }

            await _petRepository.DeleteAsync(id);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa thú cưng thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}