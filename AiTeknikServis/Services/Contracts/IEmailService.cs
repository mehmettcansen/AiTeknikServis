namespace AiTeknikServis.Services.Contracts
{
    public interface IEmailService
    {
        /// <summary>
        /// Email gönderir
        /// </summary>
        /// <param name="to">Alıcı email adresi</param>
        /// <param name="subject">Email konusu</param>
        /// <param name="body">Email içeriği</param>
        /// <returns>Task</returns>
        Task SendEmailAsync(string to, string subject, string body);

        /// <summary>
        /// HTML formatında email gönderir
        /// </summary>
        /// <param name="to">Alıcı email adresi</param>
        /// <param name="subject">Email konusu</param>
        /// <param name="htmlBody">HTML email içeriği</param>
        /// <returns>Task</returns>
        Task SendHtmlEmailAsync(string to, string subject, string htmlBody);

        /// <summary>
        /// Çoklu alıcıya email gönderir
        /// </summary>
        /// <param name="recipients">Alıcı email adresleri</param>
        /// <param name="subject">Email konusu</param>
        /// <param name="body">Email içeriği</param>
        /// <returns>Task</returns>
        Task SendBulkEmailAsync(List<string> recipients, string subject, string body);

        /// <summary>
        /// Email template kullanarak email gönderir
        /// </summary>
        /// <param name="to">Alıcı email adresi</param>
        /// <param name="templateName">Template adı</param>
        /// <param name="templateData">Template verileri</param>
        /// <returns>Task</returns>
        Task SendTemplateEmailAsync(string to, string templateName, Dictionary<string, object> templateData);

        /// <summary>
        /// Email servisinin sağlık durumunu kontrol eder
        /// </summary>
        /// <returns>Email servisi çalışıyor mu?</returns>
        Task<bool> IsHealthyAsync();

        /// <summary>
        /// Email gönderim durumunu takip eder
        /// </summary>
        /// <param name="to">Alıcı email adresi</param>
        /// <param name="subject">Email konusu</param>
        /// <param name="body">Email içeriği</param>
        /// <param name="trackingId">Takip ID'si</param>
        /// <returns>Email gönderim sonucu</returns>
        Task<EmailDeliveryResult> SendEmailWithTrackingAsync(string to, string subject, string body, string? trackingId = null);

        /// <summary>
        /// Email ekler (attachments) ile email gönderir
        /// </summary>
        /// <param name="to">Alıcı email adresi</param>
        /// <param name="subject">Email konusu</param>
        /// <param name="body">Email içeriği</param>
        /// <param name="attachments">Ek dosyalar</param>
        /// <returns>Task</returns>
        Task SendEmailWithAttachmentsAsync(string to, string subject, string body, List<EmailAttachment> attachments);

        /// <summary>
        /// Email kuyruk sistemini kullanarak async email gönderir
        /// </summary>
        /// <param name="emailRequest">Email isteği</param>
        /// <returns>Task</returns>
        Task QueueEmailAsync(EmailRequest emailRequest);

        /// <summary>
        /// Kuyruktaki bekleyen emailleri işler
        /// </summary>
        /// <returns>İşlenen email sayısı</returns>
        Task<int> ProcessEmailQueueAsync();

        /// <summary>
        /// Email istatistiklerini getirir
        /// </summary>
        /// <param name="startDate">Başlangıç tarihi</param>
        /// <param name="endDate">Bitiş tarihi</param>
        /// <returns>Email istatistikleri</returns>
        Task<EmailStatistics> GetEmailStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Email template dosyalarını yükler
        /// </summary>
        /// <returns>Yüklenen template sayısı</returns>
        Task<int> LoadEmailTemplatesAsync();

        /// <summary>
        /// Email blacklist kontrolü yapar
        /// </summary>
        /// <param name="emailAddress">Kontrol edilecek email adresi</param>
        /// <returns>Email blacklist'te mi?</returns>
        Task<bool> IsEmailBlacklistedAsync(string emailAddress);

        /// <summary>
        /// Email address validasyonu yapar
        /// </summary>
        /// <param name="emailAddress">Validasyon yapılacak email adresi</param>
        /// <returns>Email adresi geçerli mi?</returns>
        bool IsValidEmailAddress(string emailAddress);
    }

    /// <summary>
    /// Email gönderim sonucunu içeren model
    /// </summary>
    public class EmailDeliveryResult
    {
        /// <summary>
        /// Gönderim başarılı mı?
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Takip ID'si
        /// </summary>
        public string TrackingId { get; set; } = string.Empty;

        /// <summary>
        /// Gönderim zamanı
        /// </summary>
        public DateTime SentDate { get; set; }

        /// <summary>
        /// Hata mesajı (varsa)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Alıcı email adresi
        /// </summary>
        public string Recipient { get; set; } = string.Empty;

        /// <summary>
        /// Email konusu
        /// </summary>
        public string Subject { get; set; } = string.Empty;
    }

    /// <summary>
    /// Email eki modeli
    /// </summary>
    public class EmailAttachment
    {
        /// <summary>
        /// Dosya adı
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Dosya içeriği (byte array)
        /// </summary>
        public byte[] Content { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// İçerik türü (MIME type)
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// Dosya boyutu
        /// </summary>
        public long FileSize => Content.Length;
    }

    /// <summary>
    /// Email isteği modeli
    /// </summary>
    public class EmailRequest
    {
        /// <summary>
        /// Benzersiz ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Alıcı email adresi
        /// </summary>
        public string To { get; set; } = string.Empty;

        /// <summary>
        /// Email konusu
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Email içeriği
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// HTML email mi?
        /// </summary>
        public bool IsHtml { get; set; } = false;

        /// <summary>
        /// Template adı (varsa)
        /// </summary>
        public string? TemplateName { get; set; }

        /// <summary>
        /// Template verileri
        /// </summary>
        public Dictionary<string, object> TemplateData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Ek dosyalar
        /// </summary>
        public List<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();

        /// <summary>
        /// Öncelik seviyesi
        /// </summary>
        public EmailPriority Priority { get; set; } = EmailPriority.Normal;

        /// <summary>
        /// Oluşturulma zamanı
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Deneme sayısı
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// Maksimum deneme sayısı
        /// </summary>
        public int MaxRetries { get; set; } = 3;
    }

    /// <summary>
    /// Email öncelik seviyeleri
    /// </summary>
    public enum EmailPriority
    {
        Low = 1,
        Normal = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>
    /// Email istatistikleri modeli
    /// </summary>
    public class EmailStatistics
    {
        /// <summary>
        /// Toplam gönderilen email sayısı
        /// </summary>
        public int TotalSent { get; set; }

        /// <summary>
        /// Başarılı gönderimler
        /// </summary>
        public int SuccessfulDeliveries { get; set; }

        /// <summary>
        /// Başarısız gönderimler
        /// </summary>
        public int FailedDeliveries { get; set; }

        /// <summary>
        /// Kuyrukta bekleyen emailler
        /// </summary>
        public int PendingInQueue { get; set; }

        /// <summary>
        /// Başarı oranı (%)
        /// </summary>
        public double SuccessRate => TotalSent > 0 ? (double)SuccessfulDeliveries / TotalSent * 100 : 0;

        /// <summary>
        /// Ortalama gönderim süresi (milisaniye)
        /// </summary>
        public double AverageDeliveryTime { get; set; }

        /// <summary>
        /// En çok kullanılan template'ler
        /// </summary>
        public Dictionary<string, int> TopTemplates { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Günlük gönderim dağılımı
        /// </summary>
        public Dictionary<string, int> DailyDeliveryDistribution { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// İstatistik hesaplama tarihi
        /// </summary>
        public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;
    }
}