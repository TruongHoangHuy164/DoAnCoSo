using DoAnLTW.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnLTW.Models.Repositories
{
    public class HotelRoomRepository : IHotelRoomRepository
    {
        private readonly ApplicationDbContext _context;

        public HotelRoomRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<HotelRoom>> GetAllAsync()
        {
            return await _context.HotelRooms.Include(r => r.RoomType).ToListAsync();
        }

        public async Task<HotelRoom> GetByIdAsync(int id)
        {
            return await _context.HotelRooms.Include(r => r.RoomType).FirstOrDefaultAsync(r => r.RoomId == id);
        }

        public async Task<IEnumerable<HotelRoom>> GetAvailableRoomsAsync(DateTime startDate, DateTime endDate, int roomTypeId)
        {
            var bookedRoomIds = await _context.PetHotelBookings
                .Where(b => b.Status != PetHotelBookingStatus.DaHuy &&
                            b.StartDate <= endDate && b.EndDate >= startDate)
                .Select(b => b.RoomId)
                .ToListAsync();

            return await _context.HotelRooms
                .Where(r => r.RoomTypeId == roomTypeId &&
                            !bookedRoomIds.Contains(r.RoomId) &&
                            r.IsAvailable)
                .Include(r => r.RoomType)
                .ToListAsync();
        }

        public async Task AddAsync(HotelRoom hotelRoom)
        {
            await _context.HotelRooms.AddAsync(hotelRoom);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(HotelRoom hotelRoom)
        {
            _context.HotelRooms.Update(hotelRoom);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var hotelRoom = await _context.HotelRooms.FindAsync(id);
            if (hotelRoom != null)
            {
                _context.HotelRooms.Remove(hotelRoom);
                await _context.SaveChangesAsync();
            }
        }
    }
}