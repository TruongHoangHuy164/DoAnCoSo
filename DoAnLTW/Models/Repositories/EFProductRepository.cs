using DoAnLTW.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace DoAnLTW.Models.Repositories
{
    public class EFProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public EFProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductSizes)
                    .ThenInclude(ps => ps.Size)
                .Include(p => p.Images)
                .ToListAsync();

            // Gán ImageUrl từ hình ảnh đầu tiên của mỗi sản phẩm
            foreach (var product in products)
            {
                var firstImage = product.Images.FirstOrDefault();
                product.ImageUrl = firstImage?.ImageUrl ?? "/img/default-product.jpg";
            }

            return products;
        }

        public async Task<Product> GetByIdAsync(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductSizes)
                    .ThenInclude(ps => ps.Size)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.CategoryId == id);

            if (product != null)
            {
                var firstImage = product.Images.FirstOrDefault();
                product.ImageUrl = firstImage?.ImageUrl ?? "/img/default-product.jpg";
            }

            return product;
        }

        public async Task AddAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
    }
}