using DoAnLTW.Models;

namespace DoAnLTW.Services
{
    // Services/IChatService.cs
    public interface IChatService
    {
        IEnumerable<Message> GetActiveSupportRequests();
        void AddOrUpdateRequest(string customerId, string customerName, string message);
    }



}
