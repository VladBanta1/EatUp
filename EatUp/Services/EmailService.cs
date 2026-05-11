using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EatUp.Services;

public class EmailService : IEmailService
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _user;
    private readonly string _password;
    private readonly string _displayName;
    private readonly bool _configured;

    public EmailService(IConfiguration config)
    {
        _host        = config["Email:Host"] ?? "";
        _port        = int.TryParse(config["Email:Port"], out var p) ? p : 587;
        _user        = config["Email:User"] ?? "";
        _password    = config["Email:Password"] ?? "";
        _displayName = config["Email:DisplayName"] ?? "EatUp";
        _configured  = !_user.Contains("REPLACE") && !string.IsNullOrWhiteSpace(_user);
    }

    private async Task CoreSendAsync(string to, string toName, string subject, string html)
    {
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_displayName, _user));
        msg.To.Add(new MailboxAddress(toName, to));
        msg.Subject = subject;
        msg.Body = new TextPart("html") { Text = html };

        using var client = new SmtpClient();
        await client.ConnectAsync(_host, _port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_user, _password);
        await client.SendAsync(msg);
        await client.DisconnectAsync(true);
    }

    private void Fire(string to, string toName, string subject, string html)
    {
        if (!_configured) return;
        _ = Task.Run(async () =>
        {
            try { await CoreSendAsync(to, toName, subject, html); }
            catch { // fire-and-forget: swallow exceptions so email failures never surface to the caller
            }
        });
    }

    public void SendOrderPlaced(string email, string name, int orderId, string restaurantName,
                                string itemsSummary, decimal total, int estimatedMinutes)
        => Fire(email, name, $"EatUp — Comanda #{orderId} a fost plasată",
            Wrap(name,
                $@"<h2 style='margin:0 0 8px;font-size:22px;'>Comanda ta a fost plasată! 🎉</h2>
                   <p style='color:#475569;margin:0 0 20px;'>
                     <strong>{restaurantName}</strong> a primit comanda ta și o va confirma în curând.
                   </p>
                   <div style='background:#f8fafc;border-radius:10px;padding:16px 20px;margin-bottom:20px;'>
                     <div style='font-size:13px;color:#64748b;margin-bottom:8px;font-weight:600;'>PRODUSE COMANDATE</div>
                     <div style='font-size:14px;line-height:1.7;'>{itemsSummary}</div>
                     <div style='border-top:1px solid #e2e8f0;margin-top:12px;padding-top:12px;'>
                       <strong>Total: {total:0.00} RON</strong>
                     </div>
                   </div>
                   <p style='color:#475569;font-size:14px;'>
                     ⏱ Timp estimat de livrare: <strong>{estimatedMinutes} minute</strong>
                   </p>",
                orderId, "Vezi comanda"));

    public void SendOrderAccepted(string email, string name, int orderId, string restaurantName)
        => Fire(email, name, $"EatUp — Comanda #{orderId} a fost acceptată ✅",
            Wrap(name,
                $@"<h2 style='margin:0 0 8px;font-size:22px;'>Comanda ta a fost acceptată! ✅</h2>
                   <p style='color:#475569;margin:0 0 20px;'>
                     <strong>{restaurantName}</strong> a acceptat comanda #{orderId} și începe pregătirea.
                   </p>",
                orderId, "Urmărește comanda"));

    public void SendOrderPreparing(string email, string name, int orderId, string restaurantName)
        => Fire(email, name, $"EatUp — Comanda #{orderId} se prepară 👨‍🍳",
            Wrap(name,
                $@"<h2 style='margin:0 0 8px;font-size:22px;'>Se prepară comanda ta! 👨‍🍳</h2>
                   <p style='color:#475569;margin:0 0 20px;'>
                     Bucătarii de la <strong>{restaurantName}</strong> lucrează acum la comanda ta #{orderId}.
                   </p>",
                orderId, "Urmărește comanda"));

    public void SendOrderOutForDelivery(string email, string name, int orderId, string restaurantName)
        => Fire(email, name, $"EatUp — Comanda #{orderId} este în drum spre tine 🛵",
            Wrap(name,
                $@"<h2 style='margin:0 0 8px;font-size:22px;'>Comanda e în drum spre tine! 🛵</h2>
                   <p style='color:#475569;margin:0 0 20px;'>
                     Curierul a preluat comanda #{orderId} de la <strong>{restaurantName}</strong> și se îndreaptă spre tine.
                   </p>",
                orderId, "Urmărește curierul"));

    public void SendOrderDelivered(string email, string name, int orderId, string restaurantName)
        => Fire(email, name, $"EatUp — Comanda #{orderId} a fost livrată 🎉",
            Wrap(name,
                $@"<h2 style='margin:0 0 8px;font-size:22px;'>Poftă bună! 🎉</h2>
                   <p style='color:#475569;margin:0 0 20px;'>
                     Comanda #{orderId} de la <strong>{restaurantName}</strong> a fost livrată.
                     Sperăm că îți place!
                   </p>
                   <p style='color:#475569;font-size:14px;margin-bottom:20px;'>
                     Lasă o recenzie și ajută-i pe alți clienți să descopere restaurante bune. 🌟
                   </p>",
                orderId, "Lasă o recenzie"));

    public void SendOrderRejected(string email, string name, int orderId, string restaurantName, string? reason)
        => Fire(email, name, $"EatUp — Comanda #{orderId} a fost respinsă",
            Wrap(name,
                $@"<h2 style='margin:0 0 8px;font-size:22px;color:#dc2626;'>Comandă respinsă ❌</h2>
                   <p style='color:#475569;margin:0 0 16px;'>
                     Din păcate, <strong>{restaurantName}</strong> nu poate onora comanda #{orderId}.
                   </p>
                   {(string.IsNullOrEmpty(reason) ? "" : $@"
                   <div style='background:#fef2f2;border-left:4px solid #dc2626;padding:12px 16px;border-radius:6px;margin-bottom:20px;'>
                     <strong>Motiv:</strong> {reason}
                   </div>")}
                   <p style='color:#475569;font-size:14px;'>
                     Dacă ai plătit cu cardul, suma va fi returnată în 3-5 zile lucrătoare.
                   </p>",
                null, null));

    public void SendRestaurantApproved(string email, string name, string restaurantName)
        => Fire(email, name, $"EatUp — Restaurantul tău a fost aprobat! 🎉",
            Wrap(name,
                $@"<h2 style='margin:0 0 8px;font-size:22px;'>Felicitări! Ești live pe EatUp! 🎉</h2>
                   <p style='color:#475569;margin:0 0 16px;'>
                     Restaurantul <strong>{restaurantName}</strong> a fost aprobat și este acum vizibil pentru clienți.
                   </p>
                   <p style='color:#475569;font-size:14px;margin-bottom:20px;'>
                     Intră în contul tău pentru a gestiona meniul, a primi comenzi și a vedea statisticile.
                   </p>",
                null, "Mergi la dashboard", "/restaurant/dashboard"));

    public void SendRestaurantRejected(string email, string name, string restaurantName, string reason)
        => Fire(email, name, "EatUp — Aplicația ta a fost respinsă",
            Wrap(name,
                $@"<h2 style='margin:0 0 8px;font-size:22px;'>Aplicație respinsă</h2>
                   <p style='color:#475569;margin:0 0 16px;'>
                     Din păcate, aplicația pentru restaurantul <strong>{restaurantName}</strong> nu a putut fi aprobată.
                   </p>
                   <div style='background:#fef2f2;border-left:4px solid #dc2626;padding:12px 16px;border-radius:6px;margin-bottom:20px;'>
                     <strong>Motiv:</strong> {reason}
                   </div>
                   <p style='color:#475569;font-size:14px;'>
                     Poți crea o nouă aplicație după ce rezolvi problemele semnalate.
                   </p>",
                null, null));

    public void SendChangeApproved(string email, string name, string restaurantName, string itemName, string changeType)
        => Fire(email, name, $"EatUp — Modificare de meniu aprobată ✅",
            Wrap(name,
                $@"<h2 style='margin:0 0 8px;font-size:22px;'>Modificare aprobată! ✅</h2>
                   <p style='color:#475569;margin:0 0 16px;'>
                     Cererea de <strong>{changeType.ToLower()}</strong> pentru produsul
                     <strong>{itemName}</strong> de la <strong>{restaurantName}</strong>
                     a fost aprobată și este acum activă în meniu.
                   </p>",
                null, "Vezi meniul", "/restaurant/menu"));

    public void SendChangeRejected(string email, string name, string restaurantName, string itemName, string? adminNote)
        => Fire(email, name, "EatUp — Modificare de meniu respinsă",
            Wrap(name,
                $@"<h2 style='margin:0 0 8px;font-size:22px;'>Modificare respinsă</h2>
                   <p style='color:#475569;margin:0 0 16px;'>
                     Cererea pentru produsul <strong>{itemName}</strong> de la
                     <strong>{restaurantName}</strong> a fost respinsă.
                   </p>
                   {(string.IsNullOrEmpty(adminNote) ? "" : $@"
                   <div style='background:#fef2f2;border-left:4px solid #dc2626;padding:12px 16px;border-radius:6px;margin-bottom:20px;'>
                     <strong>Notă admin:</strong> {adminNote}
                   </div>")}",
                null, "Cereri meniu", "/restaurant/change-requests"));

    private static string Wrap(string recipientName, string bodyHtml,
                               int? orderId, string? btnText, string? btnHref = null)
    {
        string trackUrl = orderId.HasValue ? $"/orders/{orderId}" : (btnHref ?? "#");
        string button = (btnText != null)
            ? $@"<a href='https://localhost{{trackUrl}}'
                    style='display:inline-block;background:#f97316;color:white;font-weight:700;
                           text-decoration:none;padding:12px 28px;border-radius:10px;font-size:15px;'>
                    {btnText}
                 </a>".Replace("{trackUrl}", trackUrl)
            : "";

        return $@"<!DOCTYPE html>
<html lang='ro'>
<head><meta charset='utf-8'/></head>
<body style='margin:0;padding:0;background:#f1f5f9;font-family:Arial,Helvetica,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='padding:32px 16px;'>
    <tr><td align='center'>
      <table width='100%' style='max-width:560px;border-radius:16px;overflow:hidden;
                                  box-shadow:0 4px 24px rgba(0,0,0,.08);'>
        <!-- Header -->
        <tr>
          <td style='background:#0f172a;padding:24px 32px;'>
            <span style='color:#f97316;font-size:28px;font-weight:900;
                         letter-spacing:-0.5px;font-family:Arial,sans-serif;'>EatUp</span>
          </td>
        </tr>
        <!-- Body -->
        <tr>
          <td style='background:white;padding:32px;'>
            <p style='color:#64748b;font-size:14px;margin:0 0 20px;'>
              Salut, <strong>{recipientName}</strong>!
            </p>
            {bodyHtml}
            {(button != "" ? $"<div style='margin-top:24px;'>{button}</div>" : "")}
          </td>
        </tr>
        <!-- Footer -->
        <tr>
          <td style='background:#f8fafc;padding:16px 32px;text-align:center;
                     color:#94a3b8;font-size:12px;border-top:1px solid #e2e8f0;'>
            &copy; {DateTime.Now.Year} EatUp &mdash; Mâncare livrată rapid
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";
    }
}
