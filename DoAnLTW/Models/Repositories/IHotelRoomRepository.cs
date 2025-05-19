using DoAnLTW.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DoAnLTW.Models.Repositories
{
    public interface IHotelRoomRepository
    {
        Task<IEnumerable<HotelRoom>> GetAllAsync();
        Task<HotelRoom> GetByIdAsync(int id);
        Task<IEnumerable<HotelRoom>> GetAvailableRoomsAsync(DateTime startDate, DateTime endDate, int roomTypeId);
        Task AddAsync(HotelRoom hotelRoom);
        Task UpdateAsync(HotelRoom hotelRoom);
        Task DeleteAsync(int id);
    }
}