using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System.IO;

namespace DoAnLTW.Services
{
    public class SendMail : IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Pet-Lover", ConstantHelper.emailSender));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlMessage
                };

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(ConstantHelper.hostEmail, ConstantHelper.portEmail, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(ConstantHelper.emailSender, ConstantHelper.passwordSender);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần (có thể sử dụng ILogger)
                throw; // Ném lại exception để CheckoutController xử lý
            }
        }
    }
}