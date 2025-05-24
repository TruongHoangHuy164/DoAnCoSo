using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnLTW.Models;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using DoAnLTW.Extensions;
using DoAnLTW.ViewModels;

namespace DoAnLTW.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PetHotelBookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PetHotelBookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/PetHotelBooking
        public IActionResult Index(int? year, int? month)
        {
            var selectedDate = DateTime.Now;
            if (year.HasValue && month.HasValue && year >= 1900 && year <= 9999 && month >= 1 && month <= 12)
            {
                try
                {
                    selectedDate = new DateTime(year.Value, month.Value, 1);
                }
                catch
                {
                    // Fallback to current date if invalid
                    selectedDate = DateTime.Now;
                }
            }

            var startOfMonth = new DateTime(selectedDate.Year, selectedDate.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var model = new PetHotelBookingCalendarViewModel
            {
                Rooms = _context.HotelRooms
                    .Include(r => r.RoomType)
                    .ToList(),
                Bookings = _context.PetHotelBookings
                    .Include(b => b.Pet)
                    .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                    .Where(b => b.StartDate <= endOfMonth && b.EndDate >= startOfMonth)
                    .ToList()
            };

            ViewBag.SelectedDate = selectedDate;
            return View(model);
        }

        // GET: Admin/PetHotelBooking/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.PetHotelBookings
                .Include(b => b.Pet)
                .ThenInclude(p => p.Images)
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // GET: Admin/PetHotelBooking/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.PetHotelBookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            ViewBag.Statuses = Enum.GetValues(typeof(PetHotelBookingStatus))
                .Cast<PetHotelBookingStatus>()
                .Select(s => new SelectListItem
                {
                    Value = s.ToString(),
                    Text = s.GetDisplayName()
                }).ToList();

            return View(booking);
        }

        // POST: Admin/PetHotelBooking/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BookingId,Status")] PetHotelBooking booking)
        {
            if (id != booking.BookingId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingBooking = await _context.PetHotelBookings.FindAsync(id);
                    if (existingBooking == null)
                    {
                        return NotFound();
                    }

                    existingBooking.Status = booking.Status;
                    _context.Update(existingBooking);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật trạng thái đặt phòng thành công.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PetHotelBookingExists(booking.BookingId))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            ViewBag.Statuses = Enum.GetValues(typeof(PetHotelBookingStatus))
                .Cast<PetHotelBookingStatus>()
                .Select(s => new SelectListItem
                {
                    Value = s.ToString(),
                    Text = s.GetDisplayName()
                }).ToList();

            return View(booking);
        }

        // GET: Admin/PetHotelBooking/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.PetHotelBookings
                .Include(b => b.Pet)
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // POST: Admin/PetHotelBooking/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.PetHotelBookings.FindAsync(id);
            if (booking != null)
            {
                _context.PetHotelBookings.Remove(booking);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa đặt phòng thành công.";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/PetHotelBooking/UpdateBookingStatus/5
        public async Task<IActionResult> UpdateBookingStatus(int id)
        {
            try
            {
                var booking = await _context.PetHotelBookings
                    .Include(b => b.Pet)
                    .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                    .FirstOrDefaultAsync(b => b.BookingId == id);
                if (booking == null)
                {
                    return NotFound();
                }

                ViewBag.StatusList = Enum.GetValues(typeof(PetHotelBookingStatus))
                    .Cast<PetHotelBookingStatus>()
                    .Select(s => new SelectListItem
                    {
                        Value = s.ToString(),
                        Text = s.GetDisplayName(),
                        Selected = s == booking.Status
                    }).ToList();

                return View(booking);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải đặt phòng.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/PetHotelBooking/UpdateBookingStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookingStatus(int id, PetHotelBookingStatus status)
        {
            try
            {
                var booking = await _context.PetHotelBookings.FindAsync(id);
                if (booking == null)
                {
                    return NotFound();
                }

                booking.Status = status;
                _context.Update(booking);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật trạng thái đặt phòng thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật trạng thái đặt phòng.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/PetHotelBooking/RoomType
        public async Task<IActionResult> RoomTypeIndex()
        {
            var roomTypes = await _context.RoomTypes.ToListAsync();
            return View(roomTypes);
        }

        // GET: Admin/PetHotelBooking/RoomType/Create
        public IActionResult RoomTypeCreate()
        {
            return View();
        }

        // POST: Admin/PetHotelBooking/RoomType/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RoomTypeCreate([Bind("Name,Description,PricePerNight")] RoomType roomType)
        {
            if (ModelState.IsValid)
            {
                _context.Add(roomType);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tạo loại phòng thành công.";
                return RedirectToAction(nameof(RoomTypeIndex));
            }
            return View(roomType);
        }

        // GET: Admin/PetHotelBooking/RoomType/Edit/5
        public async Task<IActionResult> RoomTypeEdit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var roomType = await _context.RoomTypes.FindAsync(id);
            if (roomType == null)
            {
                return NotFound();
            }
            return View(roomType);
        }

        // POST: Admin/PetHotelBooking/RoomType/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RoomTypeEdit(int id, [Bind("RoomTypeId,Name,Description,PricePerNight")] RoomType roomType)
        {
            if (id != roomType.RoomTypeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(roomType);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật loại phòng thành công.";
                    return RedirectToAction(nameof(RoomTypeIndex));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoomTypeExists(roomType.RoomTypeId))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }
            return View(roomType);
        }

        // GET: Admin/PetHotelBooking/RoomType/Delete/5
        public async Task<IActionResult> RoomTypeDelete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var roomType = await _context.RoomTypes
                .Include(rt => rt.Rooms)
                .FirstOrDefaultAsync(rt => rt.RoomTypeId == id);

            if (roomType == null)
            {
                return NotFound();
            }

            return View(roomType);
        }

        // POST: Admin/PetHotelBooking/RoomType/Delete/5
        [HttpPost, ActionName("RoomTypeDelete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RoomTypeDeleteConfirmed(int id)
        {
            var roomType = await _context.RoomTypes
                .Include(rt => rt.Rooms)
                .FirstOrDefaultAsync(rt => rt.RoomTypeId == id);

            if (roomType == null)
            {
                return NotFound();
            }

            if (roomType.Rooms.Any())
            {
                TempData["Error"] = "Không thể xóa loại phòng vì có phòng đang sử dụng loại này.";
                return RedirectToAction(nameof(RoomTypeIndex));
            }

            _context.RoomTypes.Remove(roomType);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa loại phòng thành công.";
            return RedirectToAction(nameof(RoomTypeIndex));
        }

        // GET: Admin/PetHotelBooking/HotelRoom
        public async Task<IActionResult> HotelRoomIndex()
        {
            var hotelRooms = await _context.HotelRooms
                .Include(r => r.RoomType)
                .ToListAsync();
            return View(hotelRooms);
        }

        // GET: Admin/PetHotelBooking/HotelRoom/Create
        public async Task<IActionResult> HotelRoomCreate()
        {
            var roomTypes = await _context.RoomTypes.ToListAsync();
            if (!roomTypes.Any())
            {
                TempData["Error"] = "Không có loại phòng nào. Vui lòng tạo loại phòng trước.";
                return RedirectToAction(nameof(RoomTypeCreate));
            }
            ViewBag.RoomTypes = new SelectList(roomTypes, "RoomTypeId", "Name");
            return View(new HotelRoom());
        }

        // POST: Admin/PetHotelBooking/HotelRoom/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HotelRoomCreate([Bind("RoomTypeId,RoomNumber,IsAvailable")] HotelRoom hotelRoom)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            }

            // Kiểm tra RoomTypeId
            if (hotelRoom.RoomTypeId <= 0)
            {
                ModelState.AddModelError("RoomTypeId", "Vui lòng chọn loại phòng.");
                TempData["Error"] = "Vui lòng chọn loại phòng.";
            }
            else if (!await _context.RoomTypes.AnyAsync(rt => rt.RoomTypeId == hotelRoom.RoomTypeId))
            {
                ModelState.AddModelError("RoomTypeId", "Loại phòng không tồn tại.");
                TempData["Error"] = "Loại phòng không tồn tại.";
            }
            // Kiểm tra RoomNumber trùng lặp
            else if (await _context.HotelRooms.AnyAsync(r => r.RoomNumber == hotelRoom.RoomNumber))
            {
                ModelState.AddModelError("RoomNumber", "Số phòng đã tồn tại.");
                TempData["Error"] = "Số phòng đã tồn tại.";
            }
            else
            {
                try
                {
                    _context.Add(hotelRoom);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Tạo phòng thành công.";
                    return RedirectToAction(nameof(HotelRoomIndex));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Lỗi khi tạo phòng: {ex.Message}";
                }
            }

            // Nạp lại ViewBag.RoomTypes để dropdown không bị mất dữ liệu
            var roomTypes = await _context.RoomTypes.ToListAsync();
            ViewBag.RoomTypes = new SelectList(roomTypes, "RoomTypeId", "Name", hotelRoom.RoomTypeId);
            return View(hotelRoom);
        }

        // GET: Admin/PetHotelBooking/HotelRoom/Edit/5
        public async Task<IActionResult> HotelRoomEdit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hotelRoom = await _context.HotelRooms.FindAsync(id);
            if (hotelRoom == null)
            {
                return NotFound();
            }

            ViewBag.RoomTypes = new SelectList(await _context.RoomTypes.ToListAsync(), "RoomTypeId", "Name", hotelRoom.RoomTypeId);
            return View(hotelRoom);
        }

        // POST: Admin/PetHotelBooking/HotelRoom/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HotelRoomEdit(int id, [Bind("RoomId,RoomTypeId,RoomNumber,IsAvailable")] HotelRoom hotelRoom)
        {
            if (id != hotelRoom.RoomId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            }

            // Kiểm tra RoomTypeId
            if (hotelRoom.RoomTypeId <= 0)
            {
                ModelState.AddModelError("RoomTypeId", "Vui lòng chọn loại phòng.");
                TempData["Error"] = "Vui lòng chọn loại phòng.";
            }
            else if (!await _context.RoomTypes.AnyAsync(rt => rt.RoomTypeId == hotelRoom.RoomTypeId))
            {
                ModelState.AddModelError("RoomTypeId", "Loại phòng không tồn tại.");
                TempData["Error"] = "Loại phòng không tồn tại.";
            }
            // Kiểm tra RoomNumber trùng lặp (bỏ qua chính phòng đang chỉnh sửa)
            else if (await _context.HotelRooms.AnyAsync(r => r.RoomNumber == hotelRoom.RoomNumber && r.RoomId != hotelRoom.RoomId))
            {
                ModelState.AddModelError("RoomNumber", "Số phòng đã tồn tại.");
                TempData["Error"] = "Số phòng đã tồn tại.";
            }
            else
            {
                try
                {
                    _context.Update(hotelRoom);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật phòng thành công.";
                    return RedirectToAction(nameof(HotelRoomIndex));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HotelRoomExists(hotelRoom.RoomId))
                    {
                        return NotFound();
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Lỗi khi cập nhật phòng: {ex.Message}";
                }
            }

            ViewBag.RoomTypes = new SelectList(await _context.RoomTypes.ToListAsync(), "RoomTypeId", "Name", hotelRoom.RoomTypeId);
            return View(hotelRoom);
        }

        // GET: Admin/PetHotelBooking/HotelRoom/Delete/5
        public async Task<IActionResult> HotelRoomDelete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hotelRoom = await _context.HotelRooms
                .Include(r => r.RoomType)
                .Include(r => r.Bookings)
                .FirstOrDefaultAsync(r => r.RoomId == id);

            if (hotelRoom == null)
            {
                return NotFound();
            }

            return View(hotelRoom);
        }

        // POST: Admin/PetHotelBooking/HotelRoom/Delete/5
        [HttpPost, ActionName("HotelRoomDelete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HotelRoomDeleteConfirmed(int id)
        {
            var hotelRoom = await _context.HotelRooms
                .Include(r => r.Bookings)
                .FirstOrDefaultAsync(r => r.RoomId == id);

            if (hotelRoom == null)
            {
                return NotFound();
            }

            if (hotelRoom.Bookings.Any())
            {
                TempData["Error"] = "Không thể xóa phòng vì có đặt phòng liên quan.";
                return RedirectToAction(nameof(HotelRoomIndex));
            }

            _context.HotelRooms.Remove(hotelRoom);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa phòng thành công.";
            return RedirectToAction(nameof(HotelRoomIndex));
        }

        private bool PetHotelBookingExists(int id)
        {
            return _context.PetHotelBookings.Any(e => e.BookingId == id);
        }

        private bool RoomTypeExists(int id)
        {
            return _context.RoomTypes.Any(e => e.RoomTypeId == id);
        }

        private bool HotelRoomExists(int id)
        {
            return _context.HotelRooms.Any(e => e.RoomId == id);
        }
    }

    // Helper class for pagination
    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            this.AddRange(items);
        }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync();
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}