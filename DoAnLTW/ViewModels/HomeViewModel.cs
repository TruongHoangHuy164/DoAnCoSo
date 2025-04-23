using DoAnLTW.Models;

public class HomeViewModel
{
    public List<Category>? Categories { get; set; }
    public List<Product>? Products { get; set; }
    public List<Product>? RecentProducts { get; set; }
    public List<Brand>? Brands { get; set; } 
}
