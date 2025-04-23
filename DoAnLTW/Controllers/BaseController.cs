using DoAnLTW.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DoAnLTW.Controllers
{
    public class BaseController : Controller
    {
        protected const string CART_KEY = "Cart";

        protected List<CartItem> GetCartItems()
        {
            var session = HttpContext.Session;
            string jsonCart = session.GetString(CART_KEY);

            if (!string.IsNullOrEmpty(jsonCart))
            {
                return JsonConvert.DeserializeObject<List<CartItem>>(jsonCart);
            }

            return new List<CartItem>();
        }

        protected void SetCartCount()
        {
            var cart = GetCartItems();
            ViewBag.CartCount = cart.Sum(item => item.Quantity);
        }
    }
} 