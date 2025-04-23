//using DoAnLTW.Services;

//namespace DoAnLTW.Models.Repositories
//{
//    public class EFChatService : IChatService
//    {
//        private static readonly List<Message> _supportRequests = new();

//        public IEnumerable<Message> GetActiveSupportRequests()
//        {
//            return _supportRequests.Where(r => r.IsActive).OrderByDescending(r => r.LastMessageTime);
//        }

//        public void AddOrUpdateRequest(string customerId, string customerName, string message)
//        {
//            var existing = _supportRequests.FirstOrDefault(r => r.CustomerId == customerId);
//            if (existing != null)
//            {
//                existing.LastMessage = message;
//                existing.LastMessageTime = DateTime.Now;
//            }
//            else
//            {
//                _supportRequests.Add(new Message
//                {
//                    CustomerId = customerId,
//                    CustomerName = customerName,
//                    LastMessage = message,
//                    LastMessageTime = DateTime.Now,
//                    IsActive = true
//                });
//            }
//        }

       
//    }
//}
