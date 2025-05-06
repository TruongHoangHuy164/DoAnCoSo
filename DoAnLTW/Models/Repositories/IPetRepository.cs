using DoAnLTW.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DoAnLTW.Models.Repositories
{
    public interface IPetRepository
    {
        Task<IEnumerable<Pet>> GetAllAsync();
        Task<IEnumerable<Pet>> GetByUserIdAsync(string userId);
        Task<IEnumerable<Pet>> GetAllByUserIdAsync(string userId);
        Task<Pet> GetByIdAsync(int id);
        Task AddAsync(Pet pet);
        Task UpdateAsync(Pet pet);
        Task DeleteAsync(int id);
        Task GetByIdsAsync(List<int> petIds);
    }
}
