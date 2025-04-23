// Hubs/ChatHub.cs
using DoAnLTW.Services;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

public class ChatHub : Hub
{
    private readonly IChatService _chatService;

    public ChatHub(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task SendMessageToGroup(string senderId, string receiverId, string message)
    {
        string groupName = GetGroupName(senderId, receiverId);
        await Clients.Group(groupName).SendAsync("ReceiveMessage", senderId, message);

        // Cập nhật danh sách hỗ trợ
        string senderName = Context.User?.Identity?.Name ?? "Customer " + senderId;
        _chatService.AddOrUpdateRequest(senderId, senderName, message);

        // Thông báo cho admin có yêu cầu hỗ trợ mới
        await Clients.Group("AdminGroup").SendAsync("NewSupportRequest", senderId);
    }

    public async Task JoinChat(string userId, string role)
    {
        if (role.ToLower() == "customer")
        {
            string staffId = await GetAvailableStaff();
            if (staffId != null)
            {
                string groupName = GetGroupName(userId, staffId);
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                _chatService.AddOrUpdateRequest(userId, "Customer " + userId, "Started chat");
            }
        }
        else if (role.ToLower() == "admin" || role.ToLower() == "employee")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "AdminGroup");
        }
    }

    public async Task JoinSpecificChat(string userId, string customerId)
    {
        string groupName = GetGroupName(userId, customerId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    private string GetGroupName(string senderId, string receiverId)
    {
        return string.Compare(senderId, receiverId) < 0
            ? $"{senderId}-{receiverId}"
            : $"{receiverId}-{senderId}";
    }

    private async Task<string> GetAvailableStaff()
    {
        // Logic tìm staff online, tạm trả về một giá trị mẫu
        return "admin1";
    }
}