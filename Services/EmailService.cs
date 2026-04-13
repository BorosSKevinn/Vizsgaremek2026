using System.Net;
using System.Net.Mail;
using Vizsgaremek2026.Models;

namespace Vizsgaremek2026.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<bool> SendOrderPlacedEmails(RentalOrder order)
        {
            var customerSubject = $"Foglalás rögzítve – #{order.Id}";
            var customerBody = BuildOrderEmailHtml(order);

            var adminEmail = _config["EmailSettings:ContactEmail"];
            var customerOk = await SendEmailInternal(order.CustomerEmail, customerSubject, customerBody);

            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                return customerOk;
            }

            var adminOk = await SendEmailInternal(adminEmail, $"[ADMIN] Új foglalás – #{order.Id}", customerBody);
            return customerOk && adminOk;
        }

        private async Task<bool> SendEmailInternal(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                return false;
            }

            var smtpServer = _config["EmailSettings:SmtpServer"];
            var senderEmail = _config["EmailSettings:SenderEmail"];
            var senderPassword = _config["EmailSettings:SenderPassword"];

            if (string.IsNullOrWhiteSpace(smtpServer) || string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(senderPassword))
            {
                WebLogger.Log("Email beállítások hiányosak (SmtpServer / SenderEmail / SenderPassword).");
                return false;
            }

            using var smtp = new SmtpClient(smtpServer)
            {
                Port = int.TryParse(_config["EmailSettings:SmtpPort"], out var port) ? port : 587,
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true
            };

            using var mail = new MailMessage
            {
                From = new MailAddress(senderEmail),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            mail.To.Add(new MailAddress(toEmail));

            try
            {
                await smtp.SendMailAsync(mail);
                return true;
            }
            catch (Exception ex)
            {
                WebLogger.Log("Email küldési hiba: " + ex.Message);
                return false;
            }
        }

        private static string BuildOrderEmailHtml(RentalOrder order)
        {
            static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

            var itemRows = string.Join("", order.Items.Select(i => $@"
<tr>
  <td style='padding:8px;border:1px solid #ddd'>{H(i.Name)}</td>
  <td style='padding:8px;border:1px solid #ddd;text-align:center'>{i.Quantity}</td>
  <td style='padding:8px;border:1px solid #ddd;text-align:right'>{i.Price:N0} Ft</td>
  <td style='padding:8px;border:1px solid #ddd;text-align:center'>{i.RentalStartDate:yyyy.MM.dd HH:mm} – {i.RentalEndDate:HH:mm}</td>
</tr>"));

            return $@"
<div style='font-family:Arial,sans-serif;background:#f7f7f9;padding:20px'>
  <div style='max-width:700px;margin:auto;background:#fff;border-radius:8px;padding:20px'>
    <h2 style='margin-top:0'>Foglalás rögzítve</h2>
    <p>Azonosító: <strong>#{order.Id}</strong></p>
    <p><strong>Név:</strong> {H(order.CustomerName)}</p>
    <p><strong>Email:</strong> {H(order.CustomerEmail)}</p>
    <p><strong>Telefon:</strong> {H(order.CustomerPhone)}</p>
    <p><strong>Cím:</strong> {H(order.CustomerPostalCode)} {H(order.CustomerCity)}, {H(order.CustomerAddress)}</p>

    <table style='width:100%;border-collapse:collapse;border:1px solid #ddd;margin:12px 0'>
      <thead>
        <tr style='background:#fafafa'>
          <th style='padding:8px;border:1px solid #ddd;text-align:left'>Tétel</th>
          <th style='padding:8px;border:1px solid #ddd;text-align:center'>Mennyiség</th>
          <th style='padding:8px;border:1px solid #ddd;text-align:right'>Egységár</th>
          <th style='padding:8px;border:1px solid #ddd;text-align:center'>Időpont</th>
        </tr>
      </thead>
      <tbody>{itemRows}</tbody>
    </table>

    <p style='margin:0'><strong>Nettó:</strong> {order.TotalAmount:N0} Ft</p>
    <p style='margin:6px 0'><strong>ÁFA:</strong> {(order.TotalAmount * 0.27m):N0} Ft</p>
    <p style='margin:0;font-size:18px'><strong>Végösszeg:</strong> {(order.TotalAmount * 1.27m):N0} Ft</p>
  </div>
</div>";
        }
    }
}
