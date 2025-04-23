using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnLTW.Models
{
    public class FavouriteProduct 
    {
        public int Id { get; set; }
        public string UserId { get; set; } // Liên kết với bảng AspNetUsers
        public int ProductId { get; set; } // Liên kết với bảng Products

        // Navigation properties
        public virtual Product Product { get; set; }
    }
}
