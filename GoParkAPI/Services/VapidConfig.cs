using WebPush;

namespace GoParkAPI.Services
{
    public class VapidConfig
    {
        // VAPID 公鑰和私鑰
        public string PublicKey { get; }
        public string PrivateKey { get; }
        public string Subject { get; }

        // 建構子，用來初始化 VAPID 的配置
        public VapidConfig(string publicKey, string privateKey, string subject)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;
            Subject = subject;
        }

        // 取得 VapidDetails 物件，包含推播服務所需的 VAPID 配置
        public VapidDetails GetVapidDetails()
        {
            return new VapidDetails(Subject, PublicKey, PrivateKey);
        }
    }
}
