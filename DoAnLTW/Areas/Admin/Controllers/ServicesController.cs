using DoAnLTW.Models;
using DoAnLTW.Models.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Threading.Tasks;

namespace DoAnLTW.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class ServiceController : Controller
    {
        private readonly IPetServiceRepository _petServiceRepository;
        private readonly IServiceRepository _serviceRepository;

        public ServiceController(IServiceRepository serviceRepository)
        {
            _serviceRepository = serviceRepository;
        }

        // GET: Admin/Service/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                // Lấy danh sách dịch vụ từ repository
                var services = await _serviceRepository.GetAllAsync();

                // Kiểm tra xem danh sách có dữ liệu không
                if (services == null || !services.Any())
                {
                    TempData["ErrorMessage"] = "Không có dịch vụ nào trong hệ thống.";
                    return View();
                }

                // Truyền vào view
                return View(services);
            }
            catch (Exception ex)
            {
                // Log lỗi
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách dịch vụ.";
                return View();
            }
        }



        // GET: Admin/Service/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Service/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Service service)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _serviceRepository.AddAsync(service);
                    TempData["SuccessMessage"] = "Dịch vụ đã được thêm thành công.";
                    return RedirectToAction(nameof(Index));
                }
                return View(service);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo dịch vụ.";
                return View(service);
            }
        }

        // GET: Admin/Service/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var service = await _serviceRepository.GetByIdAsync(id);
                if (service == null)
                {
                    return NotFound();
                }
                return View(service);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải dịch vụ để chỉnh sửa.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Service/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PetService updatedStatus)
        {
            try
            {
                // Debug: Kiểm tra dữ liệu nhận được
                Console.WriteLine($"Received ID: {id}, Status: {updatedStatus?.Status}");

                if (id != updatedStatus.Id)
                {
                    return NotFound("ID không khớp với đối tượng được gửi.");
                }

                if (!ModelState.IsValid)
                {
                    // Debug: Kiểm tra lỗi trong ModelState
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine($"ModelState Error: {error.ErrorMessage}");
                    }
                    ViewBag.StatusList = new SelectList(Enum.GetValues(typeof(PetServiceStatus)), updatedStatus.Status);
                    return View(updatedStatus);
                }

                // Lấy đối tượng hiện tại từ cơ sở dữ liệu
                var existingPetService = await _petServiceRepository.GetByIdAsync(id);
                if (existingPetService == null)
                {
                    return NotFound("Không tìm thấy dịch vụ.");
                }

                // Chỉ cập nhật trạng thái
                existingPetService.Status = updatedStatus.Status;
                await _petServiceRepository.UpdateAsync(existingPetService);

                TempData["SuccessMessage"] = "Cập nhật trạng thái thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Edit: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật dịch vụ: " + ex.Message;
                ViewBag.StatusList = new SelectList(Enum.GetValues(typeof(PetServiceStatus)), updatedStatus.Status);
                return View(updatedStatus);
            }
        }
        // GET: Admin/Service/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var service = await _serviceRepository.GetByIdAsync(id);
                if (service == null)
                {
                    return NotFound();
                }
                return View(service);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải dịch vụ để xóa.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Service/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _serviceRepository.DeleteAsync(id);
                TempData["SuccessMessage"] = "Dịch vụ đã được xóa thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa dịch vụ.";
                return RedirectToAction(nameof(Index));
            }
        }
        // GET: Admin/PetServices/UpdateStatus/5
        public async Task<IActionResult> UpdateStatus(int id)
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
        public async Task<IActionResult> UpdateStatus(int id, PetServiceStatus status)
        {
            var booking = await _petServiceRepository.GetByIdAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            await _petServiceRepository.UpdateStatusAsync(id, status);
            return RedirectToAction(nameof(Index));
        }

       
    }
}
