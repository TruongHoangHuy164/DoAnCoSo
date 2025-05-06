using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DoAnLTW.Models.Repositories
{
    public class PetServiceRepository : IPetServiceRepository
    {
        private readonly ApplicationDbContext _context;

        public PetServiceRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<List<PetService>> GetByUserIdAsync(string userId)
        {
            return await _context.PetServices
                .Include(ps => ps.Pet) // Bao gồm Pet
                .Include(ps => ps.Service) // Bao gồm Service
                .Where(ps => ps.UserId == userId)
                .ToListAsync();
        }
        public async Task<List<PetService>> GetAllAsync()
        {
            // Sử dụng Include để lấy dữ liệu liên quan đến Pet và Service
            return await _context.PetServices
                .Include(b => b.Pet)   // Bao gồm đối tượng Pet
                .Include(b => b.Service) // Bao gồm đối tượng Service
                .ToListAsync();  // Chuyển đổi thành danh sách
        }

        public async Task<PetService> GetByIdAsync(int id)
        {
            return await _context.PetServices
                .Include(ps => ps.Pet)
                .Include(ps => ps.Service)
                .FirstOrDefaultAsync(ps => ps.Id == id);
        }

        public async Task AddAsync(PetService petService)
        {
            await _context.PetServices.AddAsync(petService);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PetService petService)
        {
            _context.PetServices.Update(petService); // Đánh dấu đối tượng là đã thay đổi
            await _context.SaveChangesAsync(); // Lưu thay đổi vào cơ sở dữ liệu
        }

        public async Task UpdateStatusAsync(int id, PetServiceStatus status)
        {
            var petService = await _context.PetServices.FindAsync(id);
            if (petService != null)
            {
                petService.Status = status;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int id)
        {
            var petService = await _context.PetServices.FindAsync(id);
            if (petService != null)
            {
                _context.PetServices.Remove(petService);
                await _context.SaveChangesAsync();
            }
        }

       
    }
}
