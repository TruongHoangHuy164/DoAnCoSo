using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using DoAnLTW.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DoAnLTW.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ChatHub(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("User ID is null. User may not be authenticated.");
                return;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                Console.WriteLine($"User with ID {userId} not found.");
                return;
            }

            var roles = await _userManager.GetRolesAsync(user);

            // Thêm người dùng vào nhóm dựa trên vai trò
            if (roles.Contains("Customer"))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Customers");
            }
            else if (roles.Contains("Employee"))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Employees");
            }

            // Cập nhật danh sách khách hàng trực tuyến cho nhân viên
            if (roles.Contains("Employee"))
            {
                await UpdateOnlineCustomers();
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("User ID is null on disconnect.");
                return;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                Console.WriteLine($"User with ID {userId} not found on disconnect.");
                return;
            }

            var roles = await _userManager.GetRolesAsync(user);

            // Xóa khỏi nhóm khi ngắt kết nối
            if (roles.Contains("Customer"))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Customers");
            }
            else if (roles.Contains("Employee"))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Employees");
            }

            // Cập nhật danh sách khách hàng trực tuyến
            if (roles.Contains("Employee"))
            {
                await UpdateOnlineCustomers();
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string receiverId, string content)
        {
            try
            {
                var senderId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(senderId))
                {
                    Console.WriteLine("Sender ID is null. User may not be authenticated.");
                    return;
                }

                if (string.IsNullOrEmpty(receiverId) || string.IsNullOrEmpty(content))
                {
                    Console.WriteLine("Invalid input: receiverId or content is empty.");
                    return;
                }

                var sender = await _userManager.FindByIdAsync(senderId);
                var receiver = await _userManager.FindByIdAsync(receiverId);

                if (receiver == null)
                {
                    Console.WriteLine($"Receiver with ID {receiverId} not found.");
                    return;
                }

                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = content,
                    Timestamp = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Message saved: ID={message.Id}, Content={content}");

                await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, sender.UserName, content, message.Timestamp);
                await Clients.Caller.SendAsync("ReceiveMessage", senderId, sender.UserName, content, message.Timestamp);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendMessage: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task GetConversation(string otherUserId)
        {
            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var messages = await _context.Messages
                .Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                            (m.SenderId == otherUserId && m.ReceiverId == userId))
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    m.SenderId,
                    SenderName = _context.Users.Where(u => u.Id == m.SenderId).Select(u => u.UserName).FirstOrDefault(),
                    m.Content,
                    m.Timestamp
                })
                .ToListAsync();

            await Clients.Caller.SendAsync("LoadConversation", messages);
        }

        private async Task UpdateOnlineCustomers()
        {
            // Query users with the "Customer" role by joining AspNetUsers and AspNetUserRoles
            var onlineCustomers = await _context.Users
                .Join(_context.UserRoles,
                    user => user.Id,
                    userRole => userRole.UserId,
                    (user, userRole) => new { User = user, UserRole = userRole })
                .Join(_context.Roles,
                    ur => ur.UserRole.RoleId,
                    role => role.Id,
                    (ur, role) => new { ur.User, RoleName = role.Name })
                .Where(ur => ur.RoleName == "Customer")
                .Select(ur => new { ur.User.Id, ur.User.UserName })
                .ToListAsync();

            await Clients.Group("Employees").SendAsync("UpdateOnlineCustomers", onlineCustomers);
        }

        public async Task MarkAsRead(string otherUserId)
        {
            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var unreadMessages = await _context.Messages
                .Where(m => m.SenderId == otherUserId && m.ReceiverId == userId && !m.IsRead)
                .ToListAsync();

            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
            }
            await _context.SaveChangesAsync();
        }
    }
}