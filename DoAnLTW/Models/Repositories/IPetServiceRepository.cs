using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DoAnLTW.Models.Repositories
{
    public interface IPetServiceRepository
    {
        Task<PetService> GetByIdAsync(int id);
        Task<List<PetService>> GetByUserIdAsync(string userId);
        Task<List<PetService>> GetAllAsync(); // Lấy tất cả dịch vụ đặt
      
        Task AddAsync(PetService petService); // Thêm dịch vụ
        Task UpdateAsync(PetService petService);// Cập nhật dịch vụ
        Task UpdateStatusAsync(int id, PetServiceStatus status); // Cập nhật trạng thái dịch vụ
        Task DeleteAsync(int id); // Xóa dịch vụ
        

    }
}
