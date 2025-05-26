using DoAnLTW.Models;

namespace DoAnLTW.ViewModels
{
    public class HomeViewModel
    {
        public List<Category> Categories { get; set; }
        public List<Brand> Brands { get; set; }
        public List<Product> Products { get; set; }
        public List<Product> RecentProducts { get; set; }
        public List<PromotionCode> PromotionCodes { get; set; }
        public List<ProductWithMinPrice> ProductsWithMinPrice { get; set; }
    }
}