using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DoAnLTW.Models
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product_Images> ProductImages { get; set; }
        public DbSet<Size> Sizes { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<ProductSize> ProductSizes { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<FavouriteProduct> FavouriteProducts { get; set; }
        public DbSet<WishProductList> WishProductLists { get; set; }
        // Thêm các DbSet mới
        public DbSet<Pet> Pets { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<PetService> PetServices { get; set; }
        public DbSet<PetImages> PetImages { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Gọi phương thức OnModelCreating của lớp cha
            base.OnModelCreating(modelBuilder);

            // Cấu hình mối quan hệ nhiều-nhiều giữa Product và Size qua ProductSize
            modelBuilder.Entity<ProductSize>()
                .HasKey(ps => new { ps.ProductId, ps.SizeId });

            modelBuilder.Entity<ProductSize>()
                .HasOne(ps => ps.Product)
                .WithMany(p => p.ProductSizes)
                .HasForeignKey(ps => ps.ProductId);

            modelBuilder.Entity<ProductSize>()
                .HasOne(ps => ps.Size)
                .WithMany(s => s.ProductSizes)
                .HasForeignKey(ps => ps.SizeId);

            // Cấu hình mối quan hệ giữa Product và Category
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId);

            // Cấu hình mối quan hệ giữa Product và Brand
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId);

            // Cấu hình mối quan hệ giữa Product_Images và Product
            modelBuilder.Entity<Product_Images>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId);

            // Cấu hình mối quan hệ giữa OrderDetail và Order
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId);

            // Cấu hình mối quan hệ giữa OrderDetail và Product
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Product)
                .WithMany()
                .HasForeignKey(od => od.ProductId);

            // Cấu hình mối quan hệ giữa FavouriteProduct và Product
            modelBuilder.Entity<FavouriteProduct>()
                .HasOne(fp => fp.Product)
                .WithMany()
                .HasForeignKey(fp => fp.ProductId);

            // Cấu hình mối quan hệ giữa WishProductList và Product
            modelBuilder.Entity<WishProductList>()
                .HasOne(wp => wp.Product)
                .WithMany()
                .HasForeignKey(wp => wp.ProductId);

            // Cấu hình các thuộc tính
            modelBuilder.Entity<Product>()
                .Property(p => p.Name)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Category>()
                .Property(c => c.Name)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<Brand>()
                .Property(b => b.Name)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<Size>()
                .Property(s => s.size)
                .HasMaxLength(30)
                .IsRequired();

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OrderDetail>()
                .Property(od => od.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ProductSize>()
                .Property(ps => ps.Price)
                .HasColumnType("decimal(18,2)");
            // Cấu hình mối quan hệ giữa Pet và PetService
            modelBuilder.Entity<PetService>()
                .HasOne(ps => ps.Pet)
                .WithMany(p => p.PetServices)
                .HasForeignKey(ps => ps.PetId);

            // Cấu hình mối quan hệ giữa Service và PetService
            modelBuilder.Entity<PetService>()
                .HasOne(ps => ps.Service)
                .WithMany(s => s.PetServices)
                .HasForeignKey(ps => ps.ServiceId);
            // Cấu hình mối quan hệ giữa PetImages và Pet
            modelBuilder.Entity<PetImages>()
                .HasOne(pi => pi.Pet)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.PetId);

            // Cấu hình thuộc tính
            modelBuilder.Entity<PetImages>()
                .Property(pi => pi.ImageUrl)
                .IsRequired();
            // Cấu hình các thuộc tính
            modelBuilder.Entity<Pet>(entity =>
            {
                entity.HasKey(e => e.PetId);
                entity.Property(e => e.UserId).IsRequired(); // Đảm bảo UserId là bắt buộc trong DB
            });


            modelBuilder.Entity<Service>()
                .Property(s => s.Name)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Service>()
                .Property(s => s.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<PetService>()
                .Property(ps => ps.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Pet>(entity =>
            {
                entity.HasKey(e => e.PetId);
                entity.Property(e => e.UserId).IsRequired(); // Đảm bảo UserId là bắt buộc trong DB
            });

        }
    }
}