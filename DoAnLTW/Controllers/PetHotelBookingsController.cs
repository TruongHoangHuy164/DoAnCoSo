using DoAnLTW.Controllers.ViewModels;
using DoAnLTW.Models;
using DoAnLTW.Models.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnLTW.Controllers
{
    [Authorize]
    public class PetHotelBookingsController : Controller
    {
        private readonly IPetHotelBookingRepository _bookingRepository;
        private readonly IPetRepository _petRepository;
        private readonly IRoomTypeRepository _roomTypeRepository;
        private readonly IHotelRoomRepository _roomRepository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<PetHotelBookingsController> _logger;

        public PetHotelBookingsController(
            IPetHotelBookingRepository bookingRepository,
            IPetRepository petRepository,
            IRoomTypeRepository roomTypeRepository,
            IHotelRoomRepository roomRepository,
            UserManager<IdentityUser> userManager,
            ILogger<PetHotelBookingsController> logger)
        {
            _bookingRepository = bookingRepository;
            _petRepository = petRepository;
            _roomTypeRepository = roomTypeRepository;
            _roomRepository = roomRepository;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: PetHotelBookings/MyBookings
        public async Task<IActionResult> MyBookings()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var bookings = await _bookingRepository.GetByUserIdAsync(userId);
                return View(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách đặt phòng khách sạn");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải lịch đặt phòng.";
                return View(new List<PetHotelBooking>());
            }
        }

        // GET: PetHotelBookings/Book
        public async Task<IActionResult> Book()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var userPets = await _petRepository.GetByUserIdAsync(userId);
                var roomTypes = await _roomTypeRepository.GetAllAsync();

                if (!userPets.Any())
                {
                    TempData["ErrorMessage"] = "Bạn cần thêm thú cưng trước khi đặt phòng.";
                    return RedirectToAction("Create", "UserPets");
                }

                var viewModel = new BookHotelViewModel
                {
                    StartDate = DateTime.Today.AddDays(1),
                    EndDate = DateTime.Today.AddDays(2),
                    UserPets = userPets.ToList(),
                    RoomTypes = roomTypes.ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chuẩn bị đặt phòng khách sạn");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi chuẩn bị đặt phòng.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: PetHotelBookings/Book
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(BookHotelViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            try
            {
                if (!ModelState.IsValid)
                {
                    return await PrepareBookViewModel(model, userId);
                }

                // Kiểm tra ngày hợp lệ
                if (model.StartDate < DateTime.Today || model.EndDate <= model.StartDate)
                {
                    ModelState.AddModelError("", "Ngày bắt đầu và kết thúc không hợp lệ.");
                    return await PrepareBookViewModel(model, userId);
                }

                // Kiểm tra thú cưng
                var pet = await _petRepository.GetByIdAsync(model.PetId);
                if (pet == null || pet.UserId != userId)
                {
                    ModelState.AddModelError("PetId", "Thú cưng không hợp lệ.");
                    return await PrepareBookViewModel(model, userId);
                }

                // Kiểm tra phòng
                var room = await _roomRepository.GetByIdAsync(model.RoomId);
                if (room == null || room.RoomTypeId != model.RoomTypeId)
                {
                    ModelState.AddModelError("RoomId", "Phòng không hợp lệ.");
                    return await PrepareBookViewModel(model, userId);
                }

                // Kiểm tra phòng có trống
                var availableRooms = await _roomRepository.GetAvailableRoomsAsync(model.StartDate, model.EndDate, model.RoomTypeId);
                if (!availableRooms.Any(r => r.RoomId == model.RoomId))
                {
                    ModelState.AddModelError("RoomId", "Phòng đã được đặt trong khoảng thời gian này.");
                    return await PrepareBookViewModel(model, userId);
                }

                // Tính tổng giá
                var roomType = await _roomTypeRepository.GetByIdAsync(model.RoomTypeId);
                var nights = (model.EndDate - model.StartDate).Days;
                var totalPrice = roomType.PricePerNight * nights;

                // Tạo booking
                var booking = new PetHotelBooking
                {
                    PetId = model.PetId,
                    RoomId = model.RoomId,
                    UserId = userId,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    Address = model.Address,
                    Note = model.Note,
                    BookingDate = DateTime.Now,
                    Status = PetHotelBookingStatus.ChoXacNhan,
                    TotalPrice = totalPrice,
                    Pet = pet,
                    Room = room
                };

                await _bookingRepository.AddAsync(booking);
                TempData["SuccessMessage"] = "Đặt phòng khách sạn thành công!";
                return RedirectToAction(nameof(MyBookings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đặt phòng khách sạn");
                ModelState.AddModelError("", "Có lỗi xảy ra khi đặt phòng.");
                return await PrepareBookViewModel(model, userId);
            }
        }

        // GET: PetHotelBookings/Cancel/5
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var booking = await _bookingRepository.GetByIdAsync(id);

                if (booking == null || booking.UserId != userId)
                {
                    return NotFound();
                }

                if (booking.Status != PetHotelBookingStatus.ChoXacNhan && booking.Status != PetHotelBookingStatus.DaXacNhan)
                {
                    TempData["ErrorMessage"] = "Không thể hủy đơn đặt phòng này.";
                    return RedirectToAction(nameof(MyBookings));
                }

                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi chuẩn bị hủy đặt phòng ID: {id}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi chuẩn bị hủy đặt phòng.";
                return RedirectToAction(nameof(MyBookings));
            }
        }

        // POST: PetHotelBookings/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var booking = await _bookingRepository.GetByIdAsync(id);

                if (booking == null || booking.UserId != userId)
                {
                    return NotFound();
                }

                if (booking.Status != PetHotelBookingStatus.ChoXacNhan && booking.Status != PetHotelBookingStatus.DaXacNhan)
                {
                    TempData["ErrorMessage"] = "Không thể hủy đơn đặt phòng này.";
                    return RedirectToAction(nameof(MyBookings));
                }

                await _bookingRepository.UpdateStatusAsync(id, PetHotelBookingStatus.DaHuy);
                TempData["SuccessMessage"] = "Hủy đơn đặt phòng thành công.";
                return RedirectToAction(nameof(MyBookings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi hủy đặt phòng ID: {id}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi hủy đặt phòng.";
                return RedirectToAction(nameof(MyBookings));
            }
        }

        private async Task<IActionResult> PrepareBookViewModel(BookHotelViewModel model, string userId)
        {
            model.UserPets = (await _petRepository.GetByUserIdAsync(userId)).ToList();
            model.RoomTypes = (await _roomTypeRepository.GetAllAsync()).ToList();
            model.AvailableRooms = (await _roomRepository.GetAvailableRoomsAsync(model.StartDate, model.EndDate, model.RoomTypeId)).ToList();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableRooms(int roomTypeId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var rooms = await _roomRepository.GetAvailableRoomsAsync(startDate, endDate, roomTypeId);
                return Json(rooms.Select(r => new { r.RoomId, r.RoomNumber }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách phòng trống");
                return BadRequest("Lỗi khi lấy danh sách phòng.");
            }
        }
    }
}