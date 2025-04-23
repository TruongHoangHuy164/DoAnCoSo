namespace DoAnLTW.Models.Momo
{
    public class MomoExecuteResponseModel
    {
        public string OrderID { get; set; }
        public string Amount { get; set; }
        public string OrderInfo { get; set; }
        public int ResultCode { get; set; } // Đây là thuộc tính đúng thay vì ErrorCode
    }
}
