using DoAnLTW.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DoAnLTW.Models.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(int id);

        Task AddProductSizeAsync(ProductSize productSize);
        Task DeleteProductSizesAsync(int productId);
        Task AddProductImageAsync(Product_Images productImage);
        Task DeleteProductImagesAsync(int productId);
        Task DeleteProductImageAsync(int imageId);
    }
}