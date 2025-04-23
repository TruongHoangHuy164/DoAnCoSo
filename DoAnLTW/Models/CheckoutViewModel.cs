using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DoAnLTW.Models;

namespace DoAnLTW.Models
{
    public class CheckoutViewModel
    {
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public decimal ShippingFee { get; set; } = 10000;

        // Thông tin đơn hàng
        public Order Order { get; set; } = new Order();

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán!")]
        public string PaymentMethod { get; set; }
    }
}
