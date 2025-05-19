using DoAnLTW.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DoAnLTW.Models.Repositories
{
    public interface IPetHotelBookingRepository
    {
        Task<IEnumerable<PetHotelBooking>> GetAllAsync();
        Task<PetHotelBooking> GetByIdAsync(int id);
        Task<IEnumerable<PetHotelBooking>> GetByUserIdAsync(string userId);
        Task AddAsync(PetHotelBooking booking);
        Task UpdateStatusAsync(int id, PetHotelBookingStatus status);
        Task UpdateAsync(PetHotelBooking booking);
    }
}