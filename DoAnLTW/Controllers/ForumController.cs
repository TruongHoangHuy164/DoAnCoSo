using DoAnLTW.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DoAnLTW.Controllers
{
    public class ForumController : Controller
    {
        private readonly RssService _rssService;

        public ForumController(RssService rssService)
        {
            _rssService = rssService;
        }

        public async Task<IActionResult> Index(string keyword = null, string rssUrl = null)
        {
            var posts = await _rssService.GetFeedAsync(rssUrl); // Lấy bài viết, đã lọc trong 5 ngày

            // Lọc bài viết theo từ khóa nếu có
            if (!string.IsNullOrEmpty(keyword))
            {
                posts = _rssService.FilterByKeyword(posts, keyword);
            }

            ViewBag.Keyword = keyword;
            ViewBag.RssUrl = rssUrl;
            return View(posts);
        }
    }
}