using DoAnLTW.Models;
using DoAnLTW.Models.Momo;
using Org.BouncyCastle.Asn1.X9;

namespace DoAnLTW.Services.Momo
{
    public interface IMomoService
    {
        Task<MomoCreatePaymentResponseModel> CreatePaymentAsync(Order model);
        MomoExecuteResponseModel PaymentExecuteAsync(IQueryCollection collection);
    }
}

