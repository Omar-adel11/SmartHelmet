using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace BLL.Services.Helper.Email
{
    public class EmailService(IConfiguration _configuration) : IEmailService
    {
        public async Task SendEmailAsync(Email email)
        {
            var emailSettings = _configuration.GetSection("EmailSettings").Get<EmailSettings>();
            var SmtpServer = emailSettings.SmtpServer;
            var SmtpPort = emailSettings.SmtpPort;
            var SenderName = emailSettings.SenderName;
            var password = emailSettings.Password;
            var senderEmail = emailSettings.SenderEmail;

            try
            {
                using (var client = new SmtpClient(SmtpServer, SmtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(senderEmail, password);

                    MailMessage mailMessage = new MailMessage
                    {
                        From = new MailAddress(emailSettings.SenderEmail, emailSettings.SenderName),
                        Subject = email.Subject,
                        Body = email.Body,
                        IsBodyHtml = true,
                    };

                    mailMessage.To.Add(email.To);
                    await client.SendMailAsync(mailMessage);
                }
            }



            catch (Exception ex)
            {
                throw new Exception("The email service is temporarily unavailable. Please try again later.", ex);
            }

            await Task.CompletedTask;
        }
    }
}
