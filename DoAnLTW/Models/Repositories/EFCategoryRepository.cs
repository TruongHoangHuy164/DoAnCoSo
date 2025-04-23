using DoAnLTW.Models;
using DoAnLTW.Models.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DoAnLTW.Models.Repositories
{
    public class EFCategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _context;
        public EFCategoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        // Lấy tất cả danh mục
        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _context.Categories.ToListAsync();
        }

        // Lấy danh mục theo ID
        public async Task<Category> GetByIdAsync(int id)
        {
            return await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.CategoryId == id);
        }


        // Thêm danh mục mới
        public async Task AddAsync(Category category)
        {
            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();
        }

        // Cập nhật danh mục
        public async Task UpdateAsync(Category category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }

        // Xóa danh mục
        public async Task DeleteAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<bool> HasProductsAsync(int categoryId)
        {
            return await _context.Products.AnyAsync(p => p.CategoryId == categoryId);
        }

    }
}
