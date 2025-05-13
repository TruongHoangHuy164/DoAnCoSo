using DoAnLTW.Models;
using DoAnLTW.Models.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnLTW.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class PetServicesController : Controller
    {
        private readonly IPetServiceRepository _petServiceRepository;
        private readonly IPetRepository _petRepository;
        private readonly IServiceRepository _serviceRepository;

        public PetServicesController(
            IPetServiceRepository petServiceRepository,
            IPetRepository petRepository,
            IServiceRepository serviceRepository)
        {
            _petServiceRepository = petServiceRepository;
            _petRepository = petRepository;
            _serviceRepository = serviceRepository;
        }

        public async Task<IActionResult> UpdateOrderStatus(int id)
        {
            var booking = await _petServiceRepository.GetByIdAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            ViewBag.StatusList = new SelectList(Enum.GetValues(typeof(PetServiceStatus)), booking.Status);

            return View(booking);
        }

        // POST: Admin/PetServices/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int id, PetServiceStatus status)
        {
            var booking = await _petServiceRepository.GetByIdAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            await _petServiceRepository.UpdateStatusAsync(id, status);
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/PetServices
        public async Task<IActionResult> Index()
        {
            var petServices = await _petServiceRepository.GetAllAsync();
            return View(petServices);
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var petService = await _petServiceRepository.GetByIdAsync(id);
                if (petService == null)
                {
                    return NotFound();
                }
                return View(petService);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải chi tiết dịch vụ.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/PetServices/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var pets = await _petRepository.GetAllAsync();
                var services = await _serviceRepository.GetAllAsync();

                ViewBag.Pets = new SelectList(pets, "PetId", "Name");
                ViewBag.Services = new SelectList(services, "ServiceId", "Name");
                ViewBag.StatusList = new SelectList(Enum.GetValues(typeof(PetServiceStatus)));

                return View();
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách thú cưng hoặc dịch vụ.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/PetServices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PetService petService)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var service = await _serviceRepository.GetByIdAsync(petService.ServiceId);
                    if (service != null)
                    {
                        petService.Price = service.Price;
                    }

                    petService.BookingDate = DateTime.Now;
                    await _petServiceRepository.AddAsync(petService);
                    TempData["SuccessMessage"] = "Đặt dịch vụ thành công!";
                    return RedirectToAction(nameof(Index));
                }

                // Nếu ModelState không hợp lệ, lấy lại dữ liệu để hiển thị form
                await LoadViewBagData(petService);
                return View(petService);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo dịch vụ.";
                return View(petService);
            }
        }

      // GET: Admin/PetServices/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var petService = await _petServiceRepository.GetByIdAsync(id);
                if (petService == null)
                {
                    return NotFound();
                }
                return View(petService);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin xóa dịch vụ.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/PetServices/DeleteConfirmed
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _petServiceRepository.DeleteAsync(id);
                TempData["SuccessMessage"] = "Xóa dịch vụ thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa dịch vụ.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Phương thức hỗ trợ để tải dữ liệu cho ViewBag
        private async Task LoadViewBagData(PetService petService)
        {
            var pets = await _petRepository.GetAllAsync();
            var services = await _serviceRepository.GetAllAsync();

            ViewBag.Pets = new SelectList(pets, "PetId", "Name", petService.PetId);
            ViewBag.Services = new SelectList(services, "ServiceId", "Name", petService.ServiceId);
            ViewBag.StatusList = new SelectList(Enum.GetValues(typeof(PetServiceStatus)), petService.Status);
        }
    }
}