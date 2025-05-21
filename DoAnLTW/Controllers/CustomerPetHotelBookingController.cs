using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnLTW.Models;
using DoAnLTW.ViewModels;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace DoAnLTW.Controllers
{
    public class CustomerPetHotelBookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CustomerPetHotelBookingController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /CustomerPetHotelBooking
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var bookings = await _context.PetHotelBookings
                .Include(b => b.Pet)
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .Where(b => b.UserId == userId)
                .ToListAsync();

            var rooms = await _context.HotelRooms
                .Include(r => r.RoomType)
                .Include(r => r.Bookings)
                .ToListAsync();

            var viewModel = new PetHotelBookingViewModel
            {
                Bookings = bookings,
                Rooms = rooms
            };

            return View(viewModel);
        }

        // GET: /CustomerPetHotelBooking/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Bạn cần đăng nhập để thực hiện thao tác này.");
            }

            var pets = await _context.Pets.Where(p => p.UserId == userId).ToListAsync();
            var rooms = await _context.HotelRooms
                .Include(r => r.RoomType)
                .Where(r => r.IsAvailable)
                .ToListAsync();

            if (!pets.Any())
            {
                TempData["Error"] = "Bạn chưa có thú cưng nào. Vui lòng thêm thú cưng trước khi đặt phòng.";
                return RedirectToAction("Index");
            }
            if (!rooms.Any())
            {
                TempData["Error"] = "Hiện không có phòng trống.";
                return RedirectToAction("Index");
            }

            ViewBag.Pets = pets;
            ViewBag.Rooms = rooms;
            ViewBag.UserId = userId; // Pass UserId to view for hidden input
            return View(new PetHotelBooking());
        }

        // POST: /CustomerPetHotelBooking/Create
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PetHotelBooking model)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Bạn cần đăng nhập để thực hiện thao tác này.");
            }

            // Set UserId immediately to avoid validation error
            model.UserId = userId;

            // Log received data for debugging
            Console.WriteLine($"Received: PetId={model.PetId}, RoomId={model.RoomId}, StartDate={model.StartDate}, EndDate={model.EndDate}, Address={model.Address}, Note={model.Note}, UserId={model.UserId}");

            // Repopulate ViewBag for error cases
            ViewBag.Pets = await _context.Pets.Where(p => p.UserId == userId).ToListAsync();
            ViewBag.Rooms = await _context.HotelRooms
                .Include(r => r.RoomType)
                .Where(r => r.IsAvailable)
                .ToListAsync();
            ViewBag.UserId = userId;

            // Explicitly validate required fields
            if (model.PetId <= 0)
            {
                ModelState.AddModelError("PetId", "Vui lòng chọn thú cưng.");
            }
            if (model.RoomId <= 0)
            {
                ModelState.AddModelError("RoomId", "Vui lòng chọn phòng.");
            }
            if (string.IsNullOrEmpty(model.UserId))
            {
                ModelState.AddModelError("UserId", "UserId không hợp lệ.");
            }
            if (string.IsNullOrEmpty(model.Address))
            {
                ModelState.AddModelError("Address", "Vui lòng nhập địa chỉ.");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["ValidationErrors"] = string.Join(", ", errors);
                return View(model);
            }

            // Validate pet ownership
            var pet = await _context.Pets.FirstOrDefaultAsync(p => p.PetId == model.PetId && p.UserId == userId);
            if (pet == null)
            {
                ModelState.AddModelError("PetId", "Thú cưng không tồn tại hoặc không thuộc về bạn.");
                return View(model);
            }

            // Check room availability
            var isRoomAvailable = await _context.PetHotelBookings
                .Where(b => b.RoomId == model.RoomId
                    && b.Status != PetHotelBookingStatus.DaHuy
                    && (model.StartDate <= b.EndDate && model.EndDate >= b.StartDate))
                .AnyAsync();
            if (isRoomAvailable)
            {
                ModelState.AddModelError("RoomId", "Phòng đã được đặt trong khoảng thời gian này.");
                return View(model);
            }

            // Validate room existence
            var room = await _context.HotelRooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomId == model.RoomId);
            if (room == null)
            {
                ModelState.AddModelError("RoomId", "Phòng không tồn tại.");
                return View(model);
            }

            // Validate dates
            var days = (model.EndDate - model.StartDate).Days;
            if (days <= 0)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu.");
                return View(model);
            }

            // Set remaining properties
            model.BookingDate = DateTime.Now;
            model.Status = PetHotelBookingStatus.ChoXacNhan;
            model.TotalPrice = days * room.RoomType.PricePerNight;

            try
            {
                _context.PetHotelBookings.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đặt phòng thành công.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi khi lưu dữ liệu: {ex.Message}");
                return View(model);
            }
        }

        // GET: /CustomerPetHotelBooking/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            var booking = await _context.PetHotelBookings
                .Include(b => b.Pet)
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.UserId == userId);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // GET: /CustomerPetHotelBooking/Cancel/5
        [Authorize]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User);
            var booking = await _context.PetHotelBookings
                .Include(b => b.Pet)
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.UserId == userId);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status != PetHotelBookingStatus.ChoXacNhan && booking.Status != PetHotelBookingStatus.DaXacNhan)
            {
                TempData["Error"] = "Không thể hủy đặt phòng ở trạng thái hiện tại.";
                return RedirectToAction("Index");
            }

            return View(booking);
        }

        // POST: /CustomerPetHotelBooking/Cancel/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var booking = await _context.PetHotelBookings
                .FirstOrDefaultAsync(b => b.BookingId == id && b.UserId == userId);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status == PetHotelBookingStatus.ChoXacNhan || booking.Status == PetHotelBookingStatus.DaXacNhan)
            {
                booking.Status = PetHotelBookingStatus.DaHuy;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Hủy đặt phòng thành công.";
            }
            else
            {
                TempData["Error"] = "Không thể hủy đặt phòng ở trạng thái hiện tại.";
            }

            return RedirectToAction("Index");
        }

        // GET: /CustomerPetHotelBooking/GetAvailableRooms
        [HttpGet]
        public async Task<IActionResult> GetAvailableRooms(DateTime startDate, DateTime endDate)
        {
            try
            {
                var availableRooms = await _context.HotelRooms
                    .Include(r => r.RoomType)
                    .Where(r => r.IsAvailable)
                    .Where(r => !_context.PetHotelBookings
                        .Any(b => b.RoomId == r.RoomId
                            && b.Status != PetHotelBookingStatus.DaHuy
                            && (startDate <= b.EndDate && endDate >= b.StartDate)))
                    .Select(r => new
                    {
                        roomId = r.RoomId,
                        roomNumber = r.RoomNumber,
                        roomTypeName = r.RoomType.Name,
                        pricePerNight = r.RoomType.PricePerNight
                    })
                    .ToListAsync();

                return Json(availableRooms);
            }
            catch (Exception ex)
            {
                return BadRequest("Lỗi khi lấy danh sách phòng trống: " + ex.Message);
            }
        }
    }
}