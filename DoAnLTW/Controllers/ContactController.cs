using DoAnLTW.Models;
using Microsoft.AspNetCore.Mvc;

namespace DoAnLTW.Controllers
{
    public class ContactController : BaseController
    {
        public IActionResult Index()
        {
            SetCartCount();
            return View();
        }
    }
}
