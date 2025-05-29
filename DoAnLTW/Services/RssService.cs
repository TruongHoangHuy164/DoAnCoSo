using CodeHollow.FeedReader;
using DoAnLTW.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnLTW.Services
{
    public class RssService
    {
        private readonly IMemoryCache _cache;
        private readonly List<string> _rssUrls = new List<string>
{
    "https://pethealth.vn/feed",
    "https://www.petmart.vn/feed",
    "https://thepet.vn/feed"
};

        public RssService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public async Task<List<PetForumPost>> GetFeedAsync(string specificUrl = null)
        {
            const string cacheKey = "PetForumPosts";
            if (_cache.TryGetValue(cacheKey, out List<PetForumPost> cachedItems) && specificUrl == null)
            {
                // Lọc lại dữ liệu từ cache để chỉ lấy bài viết trong 5 ngày
                return FilterByDateRange(cachedItems);
            }

            var allItems = new List<PetForumPost>();
            var urlsToProcess = specificUrl != null ? new List<string> { specificUrl } : _rssUrls;

            foreach (var url in urlsToProcess)
            {
                try
                {
                    var feed = await FeedReader.ReadAsync(url);
                    var feedItems = feed.Items
                        .Take(10) // Lấy 10 bài viết mới nhất từ mỗi nguồn
                        .Select(item => new PetForumPost
                        {
                            FeedItem = item,
                            FeedTitle = feed.Title ?? url // Sử dụng URL làm tiêu đề nếu feed.Title null
                        });
                    allItems.AddRange(feedItems);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading RSS feed from {url}: {ex.Message}");
                }
            }

            // Lọc bài viết trong 5 ngày
            var filteredItems = FilterByDateRange(allItems);

            // Sắp xếp bài viết theo ngày xuất bản (mới nhất trước)
            var sortedItems = filteredItems
                .OrderByDescending(item => item.FeedItem.PublishingDate ?? DateTime.MinValue)
                .Take(30) // Giới hạn tổng số bài viết
                .ToList();

            // Lưu vào cache nếu không yêu cầu URL cụ thể
            if (specificUrl == null)
            {
                _cache.Set(cacheKey, sortedItems, TimeSpan.FromHours(1));
            }

            return sortedItems;
        }

        // Phương thức lọc bài viết trong 5 ngày
        private List<PetForumPost> FilterByDateRange(List<PetForumPost> items)
        {
            var currentDate = DateTime.Now; // Ngày hiện tại: 28/05/2025
            var fiveDaysAgo = currentDate.AddDays(-5); // 23/05/2025

            return items
                .Where(item => item.FeedItem.PublishingDate.HasValue &&
                               item.FeedItem.PublishingDate.Value >= fiveDaysAgo &&
                               item.FeedItem.PublishingDate.Value <= currentDate)
                .ToList();
        }

        public List<PetForumPost> FilterByKeyword(List<PetForumPost> items, string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
                return items;

            return items
                .Where(item =>
                    (item.FeedItem.Title?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (item.FeedItem.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }
    }
}