using DoAnLTW.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnLTW.Models.Repositories
{
    public class PetHotelBookingRepository : IPetHotelBookingRepository
    {
        private readonly ApplicationDbContext _context;

        public PetHotelBookingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PetHotelBooking>> GetAllAsync()
        {
            return await _context.PetHotelBookings
                .Include(b => b.Pet)
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .ToListAsync();
        }

        public async Task<PetHotelBooking> GetByIdAsync(int id)
        {
            return await _context.PetHotelBookings
                .Include(b => b.Pet)
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .FirstOrDefaultAsync(b => b.BookingId == id);
        }

        public async Task<IEnumerable<PetHotelBooking>> GetByUserIdAsync(string userId)
        {
            return await _context.PetHotelBookings
                .Where(b => b.UserId == userId)
                .Include(b => b.Pet)
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .ToListAsync();
        }

        public async Task AddAsync(PetHotelBooking booking)
        {
            await _context.PetHotelBookings.AddAsync(booking);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(int id, PetHotelBookingStatus status)
        {
            var booking = await _context.PetHotelBookings.FindAsync(id);
            if (booking != null)
            {
                booking.Status = status;
                await _context.SaveChangesAsync();
            }
        }
        public async Task UpdateAsync(PetHotelBooking booking)
        {
            var existingBooking = await _context.PetHotelBookings.FindAsync(booking.BookingId);
            if (existingBooking != null)
            {
                existingBooking.PetId = booking.PetId;
                existingBooking.RoomId = booking.RoomId;
                existingBooking.UserId = booking.UserId;
                existingBooking.StartDate = booking.StartDate;
                existingBooking.EndDate = booking.EndDate;
                existingBooking.Address = booking.Address;
                existingBooking.Note = booking.Note;
                existingBooking.Status = booking.Status;
                existingBooking.TotalPrice = booking.TotalPrice;

                _context.PetHotelBookings.Update(existingBooking);
                await _context.SaveChangesAsync();
            }
        }
    }
}