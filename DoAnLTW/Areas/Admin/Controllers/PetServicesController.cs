
using DoAnLTW.Models;
using DoAnLTW.Models.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace DoAnLTW.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class PetServicesController : Controller
    {
        private readonly IPetServiceRepository _petServiceRepository;
        private readonly IPetRepository _petRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<PetServicesController> _logger;

        public PetServicesController(
            IPetServiceRepository petServiceRepository,
            IPetRepository petRepository,
            IServiceRepository serviceRepository,
            UserManager<IdentityUser> userManager,
            IEmailSender emailSender,
            ILogger<PetServicesController> logger)
        {
            _petServiceRepository = petServiceRepository;
            _petRepository = petRepository;
            _serviceRepository = serviceRepository;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        // GET: Admin/PetServices
        public async Task<IActionResult> Index()
        {
            var petServices = await _petServiceRepository.GetAllAsync();
            return View(petServices);
        }

        // POST: Admin/PetServices/SendServiceReminders
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendServiceReminders()
        {
            try
            {
                var bookings = await _petServiceRepository.GetAllAsync();
                if (!bookings.Any())
                {
                    TempData["ErrorMessage"] = "Không có lịch đặt dịch vụ nào để gửi nhắc nhở.";
                    return RedirectToAction(nameof(Index));
                }

                var userBookings = bookings
                    .GroupBy(b => b.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        LastServiceDate = g.Max(b => b.AppointmentDate)
                    });

                int reminderCount = 0;
                const int daysThreshold = 30;

                foreach (var userBooking in userBookings)
                {
                    var daysSinceLastService = (DateTime.Now - userBooking.LastServiceDate).Days;
                    if (daysSinceLastService >= daysThreshold)
                    {
                        var user = await _userManager.FindByIdAsync(userBooking.UserId);
                        if (user?.Email == null) continue;

                        var subject = "Nhắc nhở: Đặt lại dịch vụ cho thú cưng của bạn";
                        var htmlMessage = $@"
                            <h3>Xin chào {user.UserName},</h3>
                            <p>Đã {daysSinceLastService} ngày kể từ lần cuối bạn sử dụng dịch vụ cho thú cưng tại Pet Lover.</p>
                            <p>Hãy đặt lịch ngay hôm nay để đảm bảo thú cưng của bạn luôn được chăm sóc tốt nhất!</p>
                            <p><a href='https://localhost:5134/PetServices'>Đặt dịch vụ ngay</a></p>
                            <p>Trân trọng,<br>Đội ngũ Pet Lover</p>";

                        try
                        {
                            await _emailSender.SendEmailAsync(user.Email, subject, htmlMessage);
                            reminderCount++;
                            _logger.LogInformation($"Đã gửi email nhắc nhở tới {user.Email} (UserId: {userBooking.UserId}).");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Lỗi khi gửi email nhắc nhở tới {user.Email}");
                        }
                    }
                }

                TempData["SuccessMessage"] = $"Đã gửi {reminderCount} email nhắc nhở thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email nhắc nhở dịch vụ");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi gửi email nhắc nhở.";
                return RedirectToAction(nameof(Index));
            }
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
            TempData["SuccessMessage"] = "Cập nhật trạng thái thành công!";
            return RedirectToAction(nameof(Index));
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

                if (!string.IsNullOrEmpty(petService.UserId))
                {
                    var user = await _userManager.FindByIdAsync(petService.UserId);
                    ViewData["UserEmail"] = user != null ? user.Email : "Không tìm thấy người dùng";
                }
                else
                {
                    ViewData["UserEmail"] = "Không có thông tin người dùng";
                }

                return View(petService);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tải chi tiết dịch vụ ID: {id}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải chi tiết dịch vụ.";
                return RedirectToAction(nameof(Index));
            }
        }

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách thú cưng hoặc dịch vụ");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách thú cưng hoặc dịch vụ.";
                return RedirectToAction(nameof(Index));
            }
        }

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

                await LoadViewBagData(petService);
                return View(petService);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo dịch vụ");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo dịch vụ.";
                return View(petService);
            }
        }

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
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tải thông tin xóa dịch vụ ID: {id}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin xóa dịch vụ.";
                return RedirectToAction(nameof(Index));
            }
        }

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
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xóa dịch vụ ID: {id}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa dịch vụ.";
                return RedirectToAction(nameof(Index));
            }
        }

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
