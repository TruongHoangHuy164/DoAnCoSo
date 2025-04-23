using DoAnLTW.Models;

namespace DoAnLTW.Controllers
{
    internal class HomeViewModel
    {
        public List<Category> Categories { get; set; }
        public List<Product> Products { get; set; }
        public List<Product> RecentProducts { get; set; }
    }
}