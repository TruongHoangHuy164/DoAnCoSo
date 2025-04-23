using DoAnLTW.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            foreach (var product in products)
            {
                product.ImageUrl = product.Images.FirstOrDefault()?.ImageUrl ?? "/img/default-product.jpg";
            }

            return products;
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductSizes)
                    .ThenInclude(ps => ps.Size)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id); // Sửa từ CategoryId thành ProductId

            if (product != null)
            {
                product.ImageUrl = product.Images.FirstOrDefault()?.ImageUrl ?? "/img/default-product.jpg";
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
            var product = await _context.Products
                .Include(p => p.ProductSizes)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product != null)
            {
                _context.ProductSizes.RemoveRange(product.ProductSizes);
                _context.ProductImages.RemoveRange(product.Images);
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddProductSizeAsync(ProductSize productSize)
        {
            _context.ProductSizes.Add(productSize);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteProductSizesAsync(int productId)
        {
            var productSizes = await _context.ProductSizes
                .Where(ps => ps.ProductId == productId)
                .ToListAsync();

            _context.ProductSizes.RemoveRange(productSizes);
            await _context.SaveChangesAsync();
        }

        public async Task AddProductImageAsync(Product_Images productImage)
        {
            _context.ProductImages.Add(productImage);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteProductImagesAsync(int productId)
        {
            var productImages = await _context.ProductImages
                .Where(pi => pi.ProductId == productId)
                .ToListAsync();

            _context.ProductImages.RemoveRange(productImages);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteProductImageAsync(int imageId)
        {
            var productImage = await _context.ProductImages
                .FirstOrDefaultAsync(pi => pi.Product_ImagesId == imageId);

            if (productImage != null)
            {
                _context.ProductImages.Remove(productImage);
                await _context.SaveChangesAsync();
            }
        }
    }
}