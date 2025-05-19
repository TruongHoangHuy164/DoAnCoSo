using DoAnLTW.Models;
using DoAnLTW.Models.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnLTW.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class PetHotelManagementController : Controller
    {
        private readonly IRoomTypeRepository _roomTypeRepository;
        private readonly IHotelRoomRepository _roomRepository;
        private readonly IPetHotelBookingRepository _bookingRepository;
        private readonly ILogger<PetHotelManagementController> _logger;

        public PetHotelManagementController(
            IRoomTypeRepository roomTypeRepository,
            IHotelRoomRepository roomRepository,
            IPetHotelBookingRepository bookingRepository,
            ILogger<PetHotelManagementController> logger)
        {
            _roomTypeRepository = roomTypeRepository;
            _roomRepository = roomRepository;
            _bookingRepository = bookingRepository;
            _logger = logger;
        }

        // GET: Admin/PetHotelManagement/RoomTypes
        public async Task<IActionResult> RoomTypes()
        {
            try
            {
                var roomTypes = await _roomTypeRepository.GetAllAsync();
                return View(roomTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách loại phòng");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải danh sách loại phòng.";
                return View(new List<RoomType>());
            }
        }

        // GET: Admin/PetHotelManagement/CreateRoomType
        public IActionResult CreateRoomType()
        {
            return View(new RoomType());
        }

        // POST: Admin/PetHotelManagement/CreateRoomType
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRoomType(RoomType roomType)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _roomTypeRepository.AddAsync(roomType);
                    TempData["SuccessMessage"] = "Tạo loại phòng thành công!";
                    return RedirectToAction(nameof(RoomTypes));
                }
                return View(roomType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo loại phòng");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tạo loại phòng.";
                return View(roomType);
            }
        }

        // GET: Admin/PetHotelManagement/EditRoomType/5
        public async Task<IActionResult> EditRoomType(int id)
        {
            try
            {
                var roomType = await _roomTypeRepository.GetByIdAsync(id);
                if (roomType == null)
                {
                    return NotFound();
                }
                return View(roomType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tải loại phòng ID: {id}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải loại phòng.";
                return RedirectToAction(nameof(RoomTypes));
            }
        }

        // POST: Admin/PetHotelManagement/EditRoomType/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoomType(int id, RoomType roomType)
        {
            if (id != roomType.RoomTypeId)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    await _roomTypeRepository.UpdateAsync(roomType);
                    TempData["SuccessMessage"] = "Cập nhật loại phòng thành công!";
                    return RedirectToAction(nameof(RoomTypes));
                }
                return View(roomType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật loại phòng ID: {id}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật loại phòng.";
                return View(roomType);
            }
        }

        // GET: Admin/PetHotelManagement/DeleteRoomType/5
        public async Task<IActionResult> DeleteRoomType(int id)
        {
            try
            {
                var roomType = await _roomTypeRepository.GetByIdAsync(id);
                if (roomType == null)
                {
                    return NotFound();
                }
                return View(roomType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tải loại phòng để xóa, ID: {id}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải loại phòng.";
                return RedirectToAction(nameof(RoomTypes));
            }
        }

        // POST: Admin/PetHotelManagement/DeleteRoomTypeConfirmed/5
        [HttpPost, ActionName("DeleteRoomTypeConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRoomTypeConfirmed(int id)
        {
            try
            {
                await _roomTypeRepository.DeleteAsync(id);
                TempData["SuccessMessage"] = "Xóa loại phòng thành công!";
                return RedirectToAction(nameof(RoomTypes));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xóa loại phòng ID: {id}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xóa loại phòng.";
                return RedirectToAction(nameof(RoomTypes));
            }
        }

        // GET: Admin/PetHotelManagement/HotelRooms
        public async Task<IActionResult> HotelRooms()
        {
            try
            {
                var rooms = await _roomRepository.GetAllAsync();
                return View(rooms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách phòng");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải danh sách phòng.";
                return View(new List<HotelRoom>());
            }
        }

        // GET: Admin/PetHotelManagement/CreateHotelRoom
        public async Task<IActionResult> CreateHotelRoom()
        {
            try
            {
                ViewBag.RoomTypes = new SelectList(await _roomTypeRepository.GetAllAsync(), "RoomTypeId", "Name");
                return View(new HotelRoom());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải form tạo phòng");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải form tạo phòng.";
                return RedirectToAction(nameof(HotelRooms));
            }
        }

        // POST: Admin/PetHotelManagement/CreateHotelRoom
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateHotelRoom(HotelRoom hotelRoom)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _roomRepository.AddAsync(hotelRoom);
                    TempData["SuccessMessage"] = "Tạo phòng thành công!";
                    return RedirectToAction(nameof(HotelRooms));
                }
                ViewBag.RoomTypes = new SelectList(await _roomTypeRepository.GetAllAsync(), "RoomTypeId", "Name", hotelRoom.RoomTypeId);
                return View(hotelRoom);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo phòng");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tạo phòng.";
                ViewBag.RoomTypes = new SelectList(await _roomTypeRepository.GetAllAsync(), "RoomTypeId", "Name", hotelRoom.RoomTypeId);
                return View(hotelRoom);
            }
        }

        // GET: Admin/PetHotelManagement/EditHotelRoom/5
        public async Task<IActionResult> EditHotelRoom(int id)
        {
            try
            {
                var hotelRoom = await _roomRepository.GetByIdAsync(id);
                if (hotelRoom == null)
                {
                    return NotFound();
                }
                ViewBag.RoomTypes = new SelectList(await _roomTypeRepository.GetAllAsync(), "RoomTypeId", "Name", hotelRoom.RoomTypeId);
                return View(hotelRoom);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tải phòng để chỉnh sửa, ID: {id}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải phòng.";
                return RedirectToAction(nameof(HotelRooms));
            }
        }

        // POST: Admin/PetHotelManagement/EditHotelRoom/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditHotelRoom(int id, HotelRoom hotelRoom)
        {
            if (id != hotelRoom.RoomId)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    await _roomRepository.UpdateAsync(hotelRoom);
                    TempData["SuccessMessage"] = "Cập nhật phòng thành công!";
                    return RedirectToAction(nameof(HotelRooms));
                }
                ViewBag.RoomTypes = new SelectList(await _roomTypeRepository.GetAllAsync(), "RoomTypeId", "Name", hotelRoom.RoomTypeId);
                return View(hotelRoom);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật phòng ID: {id}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật phòng.";
                ViewBag.RoomTypes = new SelectList(await _roomTypeRepository.GetAllAsync(), "RoomTypeId", "Name", hotelRoom.RoomTypeId);
                return View(hotelRoom);
            }
        }

        // GET: Admin/PetHotelManagement/DeleteHotelRoom/5
        public async Task<IActionResult> DeleteHotelRoom(int id)
        {
            try
            {
                var hotelRoom = await _roomRepository.GetByIdAsync(id);
                if (hotelRoom == null)
                {
                    return NotFound();
                }
                return View(hotelRoom);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tải phòng để xóa, ID: {id}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải phòng.";
                return RedirectToAction(nameof(HotelRooms));
            }
        }

        // POST: Admin/PetHotelManagement/DeleteHotelRoomConfirmed/5
        [HttpPost, ActionName("DeleteHotelRoomConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHotelRoomConfirmed(int id)
        {
            try
            {
                await _roomRepository.DeleteAsync(id);
                TempData["SuccessMessage"] = "Xóa phòng thành công!";
                return RedirectToAction(nameof(HotelRooms));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xóa phòng ID: {id}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xóa phòng.";
                return RedirectToAction(nameof(HotelRooms));
            }
        }

        // GET: Admin/PetHotelManagement/Bookings
        public async Task<IActionResult> Bookings()
        {
            try
            {
                var bookings = await _bookingRepository.GetAllAsync();
                return View(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách đặt phòng");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải danh sách đặt phòng.";
                return View(new List<PetHotelBooking>());
            }
        }

        // GET: Admin/PetHotelManagement/BookingDetails/5
        public async Task<IActionResult> BookingDetails(int id)
        {
            try
            {
                var booking = await _bookingRepository.GetByIdAsync(id);
                if (booking == null)
                {
                    return NotFound();
                }
                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy chi tiết đặt phòng ID: {id}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải chi tiết đặt phòng.";
                return RedirectToAction(nameof(Bookings));
            }
        }

        // GET: Admin/PetHotelManagement/UpdateBookingStatus/5
        public async Task<IActionResult> UpdateBookingStatus(int id)
        {
            try
            {
                var booking = await _bookingRepository.GetByIdAsync(id);
                if (booking == null)
                {
                    return NotFound();
                }
                ViewBag.StatusList = new SelectList(Enum.GetValues(typeof(PetHotelBookingStatus)), booking.Status);
                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tải đặt phòng để cập nhật trạng thái, ID: {id}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải đặt phòng.";
                return RedirectToAction(nameof(Bookings));
            }
        }

        // POST: Admin/PetHotelManagement/UpdateBookingStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookingStatus(int id, PetHotelBookingStatus status)
        {
            try
            {
                var booking = await _bookingRepository.GetByIdAsync(id);
                if (booking == null)
                {
                    return NotFound();
                }
                await _bookingRepository.UpdateStatusAsync(id, status);
                TempData["SuccessMessage"] = "Cập nhật trạng thái đặt phòng thành công!";
                return RedirectToAction(nameof(Bookings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật trạng thái đặt phòng ID: {id}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật trạng thái đặt phòng.";
                return RedirectToAction(nameof(Bookings));
            }
        }
    }
}