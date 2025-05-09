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

namespace DoAnLTW.Controllers
{
    [Authorize] // Yêu cầu đăng nhập
    public class PetServicesController : Controller
    {
        private readonly IPetServiceRepository _petServiceRepository;
        private readonly IPetRepository _petRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<PetServicesController> _logger;

        public PetServicesController(
        IPetServiceRepository petServiceRepository,
        IPetRepository petRepository,
        IServiceRepository serviceRepository,
        UserManager<IdentityUser> userManager,
        ILogger<PetServicesController> logger)
        {
            _petServiceRepository = petServiceRepository;
            _petRepository = petRepository;
            _serviceRepository = serviceRepository;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: PetServices
        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để thực hiện thao tác này.";
                return Redirect("/Identity/Account/Login?ReturnUrl=" + Url.Action("Index", "TênController"));
            }

            try
            {
                var services = await _serviceRepository.GetAllAsync();
                return View(services);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách dịch vụ");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách dịch vụ.";
                return View(new List<Service>());
            }
        }
        // GET: PetServices/MyBookings
        public async Task<IActionResult> MyBookings()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var bookings = await _petServiceRepository.GetByUserIdAsync(userId);
                return View(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách đặt dịch vụ của người dùng");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải lịch đặt dịch vụ.";
                return View(new List<PetService>());
            }
        }

        // GET: PetServices/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var booking = await _petServiceRepository.GetByIdAsync(id);

                if (booking == null || booking.UserId != userId)
                {
                    return NotFound();
                }

                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy chi tiết đặt dịch vụ ID: {id}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải chi tiết đặt dịch vụ.";
                return RedirectToAction(nameof(MyBookings));
            }
        }

        // GET: PetServices/Book/5
        public async Task<IActionResult> Book(int id)
        {
            try
            {
                var service = await _serviceRepository.GetByIdAsync(id);
                if (service == null)
                {
                    _logger.LogWarning($"Không tìm thấy dịch vụ với ID: {id}");
                    return NotFound();
                }

                var userId = _userManager.GetUserId(User);
                var userPets = await _petRepository.GetByUserIdAsync(userId);

                if (!userPets.Any())
                {
                    _logger.LogInformation($"Người dùng {userId} chưa có thú cưng nào");
                    TempData["ErrorMessage"] = "Bạn cần thêm thú cưng trước khi đặt dịch vụ.";
                    return RedirectToAction("Create", "Pets");
                }

                var viewModel = new BookServiceViewModel
                {
                    ServiceId = service.ServiceId,
                    SelectedService = service,
                    UserPets = userPets.ToList(),
                    AppointmentDate = DateTime.Today.AddDays(1)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi chuẩn bị đặt dịch vụ ID: {id}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi chuẩn bị đặt dịch vụ.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(BookServiceViewModel model)
        {
            string userId = _userManager.GetUserId(User);

            try
            {
                // Kiểm tra ModelState
                if (!ModelState.IsValid)
                {
                    // Ghi log lỗi validation
                    foreach (var key in ModelState.Keys)
                    {
                        var state = ModelState[key];
                        if (state.Errors.Count > 0)
                        {
                            foreach (var error in state.Errors)
                            {
                                Console.WriteLine($"Lỗi validation: {key} - {error.ErrorMessage}");
                            }
                        }
                    }

                    // Lấy lại dữ liệu để hiển thị form
                    var userPets = await _petRepository.GetByUserIdAsync(userId);
                    var selectedService = await _serviceRepository.GetByIdAsync(model.ServiceId);

                    model.UserPets = userPets.ToList();
                    model.SelectedService = selectedService;

                    return View(model);
                }

                // Lấy thông tin dịch vụ
                var service = await _serviceRepository.GetByIdAsync(model.ServiceId);
                if (service == null)
                {
                    ModelState.AddModelError("ServiceId", "Dịch vụ không tồn tại");
                    return await PrepareBookViewModel(model, userId);
                }

                // Xử lý thời gian
                DateTime appointmentTime;
                if (!DateTime.TryParse(model.AppointmentTimeString, out appointmentTime))
                {
                    ModelState.AddModelError("AppointmentTimeString", "Giờ hẹn không hợp lệ");
                    return await PrepareBookViewModel(model, userId);
                }
                // Lấy thông tin thú cưng từ repository dựa trên PetId
                var pet = await _petRepository.GetByIdAsync(model.PetId);
                if (pet == null)
                {
                    // Xử lý khi không tìm thấy thú cưng
                    ModelState.AddModelError("PetId", "Không tìm thấy thú cưng.");
                    return View(model);
                }

                // Tạo đối tượng PetService với thông tin thú cưng và dịch vụ
                var petService = new PetService
                {
                    PetId = model.PetId,
                    ServiceId = model.ServiceId,
                    UserId = userId,
                    AppointmentDate = model.AppointmentDate,
                    AppointmentTime = new DateTime(
                        model.AppointmentDate.Year,
                        model.AppointmentDate.Month,
                        model.AppointmentDate.Day,
                        appointmentTime.Hour,
                        appointmentTime.Minute,
                        0),
                    Address = model.Address,
                    Note = model.Note ?? string.Empty,
                    BookingDate = DateTime.Now,
                    Status = PetServiceStatus.ChoXacNhan,
                    Price = service.Price,
                    Pet = pet,    // Gán đối tượng pet vào PetService
                    Service = service // Gán dịch vụ vào PetService
                };

                // Ghi log thông tin trước khi lưu
                Console.WriteLine($"Đang lưu PetService: PetId={petService.PetId}, ServiceId={petService.ServiceId}, UserId={petService.UserId}, AppointmentDate={petService.AppointmentDate}, AppointmentTime={petService.AppointmentTime}, Address={petService.Address}, Note={petService.Note}, BookingDate={petService.BookingDate}, Status={petService.Status}, Price={petService.Price}");

                // Lưu đối tượng PetService vào cơ sở dữ liệu
                await _petServiceRepository.AddAsync(petService);

                TempData["SuccessMessage"] = "Đặt dịch vụ thành công! Chúng tôi sẽ liên hệ với bạn sớm.";
                return RedirectToAction(nameof(MyBookings));



                // Ghi log thông tin trước khi lưu
                Console.WriteLine($"Đang lưu PetService: PetId={petService.PetId}, ServiceId={petService.ServiceId}, UserId={petService.UserId}, AppointmentDate={petService.AppointmentDate}, AppointmentTime={petService.AppointmentTime}, Address={petService.Address}, Note={petService.Note}, BookingDate={petService.BookingDate}, Status={petService.Status}, Price={petService.Price}");

                // Lưu đặt dịch vụ
                await _petServiceRepository.AddAsync(petService);

                TempData["SuccessMessage"] = "Đặt dịch vụ thành công! Chúng tôi sẽ liên hệ với bạn sớm.";
                return RedirectToAction(nameof(MyBookings));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi: {ex.Message}");
                ModelState.AddModelError("", $"Có lỗi xảy ra khi đặt dịch vụ: {ex.Message}");

                // Lấy lại dữ liệu để hiển thị form
                var userPets = await _petRepository.GetByUserIdAsync(userId);
                var selectedService = await _serviceRepository.GetByIdAsync(model.ServiceId);

                model.UserPets = userPets.ToList();
                model.SelectedService = selectedService;

                return View(model);
            }
        }

        private async Task<IActionResult> PrepareBookViewModel(BookServiceViewModel model, string userId)
        {
            var userPets = await _petRepository.GetByUserIdAsync(userId);
            var selectedService = await _serviceRepository.GetByIdAsync(model.ServiceId);

            model.UserPets = userPets.ToList();
            model.SelectedService = selectedService;

            return View(model);
        }

        // GET: PetServices/Cancel/5
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var booking = await _petServiceRepository.GetByIdAsync(id);

                if (booking == null || booking.UserId != userId)
                {
                    _logger.LogWarning($"Không tìm thấy đặt dịch vụ ID: {id} hoặc người dùng {userId} không có quyền");
                    return NotFound();
                }

                // Chỉ cho phép hủy đơn đang chờ xác nhận hoặc đã xác nhận
                if (booking.Status != PetServiceStatus.ChoXacNhan && booking.Status != PetServiceStatus.DaXacNhan)
                {
                    _logger.LogWarning($"Không thể hủy đặt dịch vụ ID: {id} với trạng thái: {booking.Status}");
                    TempData["ErrorMessage"] = "Không thể hủy đơn đặt dịch vụ này.";
                    return RedirectToAction(nameof(MyBookings));
                }

                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi chuẩn bị hủy đặt dịch vụ ID: {id}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi chuẩn bị hủy đặt dịch vụ.";
                return RedirectToAction(nameof(MyBookings));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var booking = await _petServiceRepository.GetByIdAsync(id);

                if (booking == null || booking.UserId != userId)
                {
                    _logger.LogWarning($"Không tìm thấy đặt dịch vụ ID: {id} hoặc người dùng {userId} không có quyền");
                    return NotFound();
                }

                // Chỉ cho phép hủy đơn đang chờ xác nhận hoặc đã xác nhận
                if (booking.Status != PetServiceStatus.ChoXacNhan && booking.Status != PetServiceStatus.DaXacNhan)
                {
                    _logger.LogWarning($"Không thể hủy đặt dịch vụ ID: {id} với trạng thái: {booking.Status}");
                    TempData["ErrorMessage"] = "Không thể hủy đơn đặt dịch vụ này.";
                    return RedirectToAction(nameof(MyBookings));
                }

                await _petServiceRepository.UpdateStatusAsync(id, PetServiceStatus.DaHuy);
                _logger.LogInformation($"Đã hủy đặt dịch vụ ID: {id} thành công");

                TempData["SuccessMessage"] = "Hủy đơn đặt dịch vụ thành công.";
                return RedirectToAction(nameof(MyBookings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi hủy đặt dịch vụ ID: {id}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi hủy đặt dịch vụ.";
                return RedirectToAction(nameof(MyBookings));
            }
        }

        // Phương thức kiểm tra để debug
        public async Task<IActionResult> TestBookService()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Content("Không tìm thấy User ID. Đảm bảo bạn đã đăng nhập.");
                }

                // Lấy thú cưng đầu tiên của người dùng
                var userPets = await _petRepository.GetByUserIdAsync(userId);
                if (!userPets.Any())
                {
                    return Content("Bạn chưa có thú cưng nào. Hãy thêm thú cưng trước.");
                }
                var pet = userPets.First();

                // Lấy dịch vụ đầu tiên
                var services = await _serviceRepository.GetAllAsync();
                if (!services.Any())
                {
                    return Content("Không có dịch vụ nào trong hệ thống.");
                }
                var service = services.First();

                // Tạo đối tượng PetService
                var petService = new PetService
                {
                    PetId = pet.PetId,
                    ServiceId = service.ServiceId,
                    UserId = userId,
                    AppointmentDate = DateTime.Today.AddDays(1),
                    AppointmentTime = DateTime.Now,
                    Address = "Địa chỉ test",
                    Note = "Ghi chú test",
                    BookingDate = DateTime.Now,
                    Status = PetServiceStatus.ChoXacNhan,
                    Price = service.Price
                };

                // Lưu đặt dịch vụ
                await _petServiceRepository.AddAsync(petService);

                return Content($"Đặt dịch vụ test thành công! ID: {petService.Id}, PetId: {petService.PetId}, ServiceId: {petService.ServiceId}");
            }
            catch (Exception ex)
            {
                return Content($"Lỗi: {ex.Message}\n{ex.StackTrace}");
            }
        }
        // GET: PetServices / TestSave
        public async Task<IActionResult> TestSave()
        {
            try
            {
                // Lấy thông tin người dùng hiện tại
                var userId = _userManager.GetUserId(User);

                // Lấy thú cưng đầu tiên của người dùng
                var pets = await _petRepository.GetByUserIdAsync(userId);
                if (!pets.Any())
                {
                    return Content("Bạn chưa có thú cưng nào. Hãy thêm thú cưng trước.");
                }
                var pet = pets.First();

                // Lấy dịch vụ đầu tiên
                var services = await _serviceRepository.GetAllAsync();
                if (!services.Any())
                {
                    return Content("Không có dịch vụ nào trong hệ thống.");
                }
                var service = services.First();

                // Tạo đối tượng PetService mới
                var petService = new PetService
                {
                    PetId = pet.PetId,
                    ServiceId = service.ServiceId,
                    UserId = userId,
                    AppointmentDate = DateTime.Today.AddDays(1),
                    AppointmentTime = DateTime.Now,
                    Address = "123 Test Street",
                    Note = "Test booking",
                    BookingDate = DateTime.Now,
                    Status = PetServiceStatus.ChoXacNhan,
                    Price = service.Price
                };

                // Lưu vào cơ sở dữ liệu
                await _petServiceRepository.AddAsync(petService);

                return Content($"Đặt dịch vụ thành công! ID: {petService.Id}");
            }
            catch (Exception ex)
            {
                return Content($"Lỗi: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}