using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
public class MailService
{
    private readonly IConfiguration _configuration;
    public MailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    //註冊信
    public async Task SendEmailAsync(string Email, string subject, string body)
    {
        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("MyGoParking團隊", _configuration["EmailSettings:SenderEmail"]));   
            email.To.Add(MailboxAddress.Parse(Email));
            email.Subject = subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_configuration["EmailSettings:SmtpServer"],
            int.Parse(_configuration["EmailSettings:SmtpPort"]), SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_configuration["EmailSettings:Username"], _configuration["EmailSettings:Password"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    //忘記密碼驗證信
    public async Task SendForgotEmailAsync(string email, string subject, string body, string link)
    {
        try
        {
            // 構建郵件內容
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("MyGoParking團隊", _configuration["EmailSettings:SenderEmail"]));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = subject;

            // 郵件內容包括重設密碼的連結
            var bodyWithLink = $@"
            <h1>重設密碼請求</h1>
            <p>我們收到了您重設密碼的請求，請點擊以下連結來重設您的密碼：</p>
            <a href='{link}'>點擊此處重設密碼</a>
            <p>請於一小時內完成，此連結於一小時後失效。</p>
            <p>如果您沒有請求重設密碼，請忽略這封郵件。</p>";

            // 設定郵件格式為 HTML
            message.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = bodyWithLink
            };

            using var smtp = new SmtpClient();
            // 連接 SMTP 伺服器
            await smtp.ConnectAsync(_configuration["EmailSettings:SmtpServer"],
            int.Parse(_configuration["EmailSettings:SmtpPort"]), SecureSocketOptions.StartTls);

            // 驗證 SMTP 伺服器
            await smtp.AuthenticateAsync(_configuration["EmailSettings:Username"], _configuration["EmailSettings:Password"]);

            // 發送郵件
            await smtp.SendAsync(message);

            // 斷開連接
            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            throw;
        }
    }


    // 新增模板讀取和佔位符替換方法
    public async Task<string> LoadEmailTemplateAsync(string templatePath, Dictionary<string, string> placeholders)
    {
        // 確認模板文件的完整路徑
        string fullTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), templatePath);

        // 讀取 HTML 模板內容
        string emailBody = await File.ReadAllTextAsync(fullTemplatePath);

        // 替換模板中的佔位符
        foreach (var placeholder in placeholders)
        {
            // 構造佔位符名稱，例如將 "username" 轉為 "{{username}}"
            string placeholderKey = $"{{{{{placeholder.Key}}}}}";
            emailBody = emailBody.Replace(placeholderKey, placeholder.Value);
        }

        return emailBody;
    }

}
