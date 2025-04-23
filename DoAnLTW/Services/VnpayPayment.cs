namespace DoAnLTW.Services;

using VNPAY.NET;

public class VnpayPayment
{
    private readonly IVnpay _vnpay;
    private readonly IConfiguration _configuration;

    public VnpayPayment(IVnpay vnpay, IConfiguration configuration)
    {
        _vnpay = vnpay;
        _configuration = configuration;

        _vnpay.Initialize(_configuration["Vnpay:TmnCode"], _configuration["Vnpay:HashSecret"], _configuration["Vnpay:BaseUrl"], _configuration["Vnpay:CallbackUrl"]);
    }
}
