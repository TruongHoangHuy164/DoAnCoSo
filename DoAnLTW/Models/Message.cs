using System.ComponentModel.DataAnnotations;
namespace DoAnLTW.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string SenderId { get; set; } // UserId của người gửi (Customer hoặc Employee)
        public string ReceiverId { get; set; } // UserId của người nhận
        public string Content { get; set; } // Nội dung tin nhắn
        public DateTime Timestamp { get; set; } // Thời gian gửi
        public bool IsRead { get; set; } // Trạng thái đã đọc
    }
}
