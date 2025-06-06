using System.Threading.Tasks;

namespace DoAnLTW.Models.Repositories
{
    public interface IOrderRepository
    {
        Task AddAsync(Order order); // Thêm phương thức AddAsync
        Task<bool> HasOrdersForProductAsync(int productId);
        Task<Order> GetByIdAsync(int id);
        Task<IEnumerable<Order>> GetAllAsync();
        Task DeleteAsync(int id);
        Task UpdateAsync(Order order);
    }
}