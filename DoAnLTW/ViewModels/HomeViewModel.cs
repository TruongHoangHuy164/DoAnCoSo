using DoAnLTW.Models;

public class HomeViewModel
{
    public List<Category> Categories { get; set; }
    public List<Brand> Brands { get; set; }
    public List<Product> Products { get; set; }
    public List<Product> RecentProducts { get; set; }

    // New property to store products with their minimum prices
    public List<ProductWithMinPrice> ProductsWithMinPrice { get; set; }
}
