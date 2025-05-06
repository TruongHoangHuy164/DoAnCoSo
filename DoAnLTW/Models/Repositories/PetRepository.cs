using DoAnLTW.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnLTW.Models.Repositories
{
    public class PetRepository : IPetRepository
    {
        private readonly ApplicationDbContext _context;

        public PetRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Pet>> GetByUserIdAsync(string userId)
        {
            return await _context.Pets
                .Where(p => p.UserId == userId)
                .ToListAsync();
        }

        public async Task DeleteAsync(int petId)
        {
            var pet = await _context.Pets.FindAsync(petId);
            if (pet != null)
            {
                _context.Pets.Remove(pet);
                await _context.SaveChangesAsync();
            }
        }
        public async Task AddAsync(Pet pet)
        {
            try
            {
                // Giả sử bạn dùng Entity Framework
                _context.Pets.Add(pet);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving pet: {ex.Message}");
                throw; // Ném lại ngoại lệ để controller xử lý
            }
        }

        public async Task<IEnumerable<Pet>> GetAllByUserIdAsync(string userId)
        {
            return await _context.Pets
                .Where(p => p.UserId == userId)
                .ToListAsync();
        }
        public async Task<IEnumerable<Pet>> GetAllAsync()
        {
            // Lấy tất cả thú cưng từ database
            return await _context.Pets.ToListAsync();
        }

        public async Task<Pet> GetByIdAsync(int id)
        {
            // Lấy một thú cưng theo id
            return await _context.Pets.FindAsync(id);
        }



        public async Task UpdateAsync(Pet pet)
        {
            try
            {
                _context.Pets.Update(pet);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating pet: {ex.Message}");
                throw;
            }
        }

        public Task GetByIdsAsync(List<int> petIds)
        {
            throw new NotImplementedException();
        }
    }
}
