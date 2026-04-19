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
  <td style='padding:12px 10px;border-bottom:1px solid #e9edf3;color:#1f2937;font-size:14px;vertical-align:top'>
    <strong>{H(i.Name)}</strong>
  </td>
  <td style='padding:12px 10px;border-bottom:1px solid #e9edf3;text-align:center;color:#374151;font-size:14px;vertical-align:top'>
    {i.Quantity}
  </td>
  <td style='padding:12px 10px;border-bottom:1px solid #e9edf3;text-align:right;color:#374151;font-size:14px;vertical-align:top;white-space:nowrap'>
    {(i.Price / i.Quantity):N0} Ft
  </td>
  <td style='padding:12px 10px;border-bottom:1px solid #e9edf3;text-align:center;color:#374151;font-size:14px;vertical-align:top;white-space:nowrap'>
    {i.RentalStartDate:yyyy.MM.dd HH:mm}<br />
    <span style='color:#6b7280;font-size:13px'>– {i.RentalEndDate:HH:mm}</span>
  </td>
</tr>"));

            return $@"
<div style='margin:0;padding:0;background:#f3f6fb;font-family:Arial,Helvetica,sans-serif;color:#1f2937'>
  <div style='width:100%;padding:24px 12px;box-sizing:border-box'>
    <div style='max-width:720px;margin:0 auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:18px;overflow:hidden;box-shadow:0 10px 30px rgba(15,23,42,0.08)'>

      <div style='background:linear-gradient(135deg,#0f172a 0%,#1e3a8a 100%);padding:28px 24px;color:#ffffff'>
        <div style='font-size:12px;letter-spacing:0.08em;text-transform:uppercase;opacity:0.85;margin-bottom:8px'>
          FITO Hungary Kft.
        </div>
        <h1 style='margin:0;font-size:26px;line-height:1.25;font-weight:700'>
          Foglalás rögzítve
        </h1>
        <p style='margin:10px 0 0 0;font-size:14px;line-height:1.6;opacity:0.92'>
          Köszönjük a foglalást! Az alábbiakban összefoglaltuk a rendelésed adatait.
        </p>
      </div>

      <div style='padding:24px'>
        <div style='background:#f8fafc;border:1px solid #e5e7eb;border-radius:14px;padding:16px 18px;margin-bottom:20px'>
          <p style='margin:0 0 10px 0;font-size:14px;color:#475569'>
            <strong>Foglalási azonosító:</strong> #{order.Id}
          </p>
          <p style='margin:0;font-size:14px;color:#475569;line-height:1.7'>
            Ha kérdésed van, erre az azonosítóra hivatkozva könnyebben tudunk segíteni.
          </p>
        </div>

        <h2 style='margin:0 0 12px 0;font-size:18px;color:#111827'>Kapcsolattartási adatok</h2>

        <div style='background:#ffffff;border:1px solid #e5e7eb;border-radius:14px;padding:16px 18px;margin-bottom:22px'>
          <p style='margin:0 0 8px 0;font-size:14px;line-height:1.7'><strong>Név:</strong> {H(order.CustomerName)}</p>
          <p style='margin:0 0 8px 0;font-size:14px;line-height:1.7'><strong>E-mail:</strong> {H(order.CustomerEmail)}</p>
          <p style='margin:0 0 8px 0;font-size:14px;line-height:1.7'><strong>Telefon:</strong> {H(order.CustomerPhone)}</p>
          <p style='margin:0;font-size:14px;line-height:1.7'>
            <strong>Cím:</strong> {H(order.CustomerPostalCode)} {H(order.CustomerCity)}, {H(order.CustomerAddress)}
          </p>
        </div>

        <h2 style='margin:0 0 12px 0;font-size:18px;color:#111827'>Foglalási tételek</h2>

        <div style='border:1px solid #e5e7eb;border-radius:14px;overflow:hidden;margin-bottom:22px;background:#ffffff'>
          <div style='width:100%;overflow-x:auto'>
            <table style='width:100%;min-width:560px;border-collapse:collapse;background:#ffffff'>
              <thead>
                <tr style='background:#f8fafc'>
                  <th style='padding:12px 10px;border-bottom:1px solid #e5e7eb;text-align:left;font-size:13px;color:#475569'>Tétel</th>
                  <th style='padding:12px 10px;border-bottom:1px solid #e5e7eb;text-align:center;font-size:13px;color:#475569'>Mennyiség</th>
                  <th style='padding:12px 10px;border-bottom:1px solid #e5e7eb;text-align:right;font-size:13px;color:#475569'>Egységár</th>
                  <th style='padding:12px 10px;border-bottom:1px solid #e5e7eb;text-align:center;font-size:13px;color:#475569'>Időpont</th>
                </tr>
              </thead>
              <tbody>
                {itemRows}
              </tbody>
            </table>
          </div>
        </div>

        <div style='background:#f8fafc;border:1px solid #e5e7eb;border-radius:14px;padding:18px'>
          <div style='display:block'>
            <p style='margin:0 0 8px 0;font-size:14px;color:#475569'>
              <strong>Nettó összeg:</strong> {order.TotalAmount:N0} Ft
            </p>
            <p style='margin:0 0 8px 0;font-size:14px;color:#475569'>
              <strong>ÁFA (27%):</strong> {(order.TotalAmount * 0.27m):N0} Ft
            </p>
            <p style='margin:0;font-size:20px;color:#111827;font-weight:700'>
              Végösszeg: {(order.TotalAmount * 1.27m):N0} Ft
            </p>
          </div>
        </div>

        <div style='margin-top:22px;padding-top:18px;border-top:1px solid #e5e7eb'>
          <p style='margin:0 0 8px 0;font-size:14px;line-height:1.7;color:#475569'>
            Köszönjük, hogy a <strong>FITO Hungary Kft.</strong> szolgáltatását választottad.
          </p>
          <p style='margin:0;font-size:13px;line-height:1.7;color:#6b7280'>
            Ez egy automatikus visszaigazoló e-mail, kérjük ne válaszolj rá.
          </p>
        </div>
      </div>
    </div>
  </div>
</div>";
        }
    }
}
