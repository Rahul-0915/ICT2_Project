using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace SVM_API.Services
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string toEmail, string otp);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        public EmailService(IConfiguration config) => _config = config;

        public async Task SendOtpEmailAsync(string toEmail, string otp)
        {
            var smtpServer = _config["EmailSettings:SmtpServer"];
            var port = int.Parse(_config["EmailSettings:SmtpPort"]);
            var senderEmail = _config["EmailSettings:SenderEmail"];
            var senderPassword = _config["EmailSettings:SenderPassword"];

            using var client = new SmtpClient(smtpServer, port);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(senderEmail, senderPassword);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail),
                Subject = "Password Reset OTP",
                Body = $@"
                    <h2>Password Reset Request</h2>
                    <p>Your OTP for password reset is: <b>{otp}</b></p>
                    <p>This OTP is valid for 15 minutes.</p>
                    <p>If you didn't request this, please ignore.</p>",
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }
    }
}