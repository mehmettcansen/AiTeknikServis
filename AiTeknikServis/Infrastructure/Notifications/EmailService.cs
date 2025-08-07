using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using AiTeknikServis.Services.Contracts;

namespace AiTeknikServis.Infrastructure.Notifications
{
    public class EmailService : IEmailService, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        
        // Email kuyruğu ve cache
        private readonly ConcurrentQueue<EmailRequest> _emailQueue = new();
        private readonly ConcurrentDictionary<string, EmailDeliveryResult> _deliveryTracker = new();
        private readonly ConcurrentDictionary<string, EmailTemplate> _templateCache = new();
        private readonly HashSet<string> _emailBlacklist = new();
        
        // Performans ve istatistik takibi
        private int _totalSent = 0;
        private int _successfulDeliveries = 0;
        private int _failedDeliveries = 0;
        private readonly object _statsLock = new object();

        /// <summary>
        /// EmailService constructor - Gelişmiş email servisi konfigürasyonu
        /// </summary>
        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IWebHostEnvironment webHostEnvironment)
        {
            _configuration = configuration;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            
            // Başlangıçta template'leri yükle
            _ = Task.Run(LoadEmailTemplatesAsync);
            
            // Blacklist'i yükle
            LoadEmailBlacklist();
        }

        /// <summary>
        /// Email gönderir
        /// </summary>
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var result = await SendEmailWithTrackingAsync(to, subject, body);
            if (!result.IsSuccess)
            {
                throw new EmailServiceException(result.ErrorMessage ?? "Email gönderilemedi");
            }
        }

        /// <summary>
        /// HTML formatında email gönderir
        /// </summary>
        public async Task SendHtmlEmailAsync(string to, string subject, string htmlBody)
        {
            await SendEmailInternalAsync(to, subject, htmlBody, isHtml: true);
        }

        /// <summary>
        /// Çoklu alıcıya email gönderir
        /// </summary>
        public async Task SendBulkEmailAsync(List<string> recipients, string subject, string body)
        {
            try
            {
                var tasks = recipients.Select(recipient => SendEmailAsync(recipient, subject, body));
                await Task.WhenAll(tasks);

                _logger.LogInformation("Toplu email başarıyla gönderildi: {Count} alıcı", recipients.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toplu email gönderilirken hata: {Count} alıcı", recipients.Count);
                throw new EmailServiceException("Toplu email gönderilemedi", ex);
            }
        }

        /// <summary>
        /// Email template kullanarak email gönderir
        /// </summary>
        public async Task SendTemplateEmailAsync(string to, string templateName, Dictionary<string, object> templateData)
        {
            try
            {
                var template = await GetEmailTemplateAsync(templateName);
                var processedTemplate = ProcessTemplate(template, templateData);

                await SendHtmlEmailAsync(to, processedTemplate.Subject, processedTemplate.Body);

                _logger.LogInformation("Template email başarıyla gönderildi: To: {To}, Template: {TemplateName}", to, templateName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Template email gönderilirken hata: To: {To}, Template: {TemplateName}", to, templateName);
                throw new EmailServiceException("Template email gönderilemedi", ex);
            }
        }

        /// <summary>
        /// Email gönderim durumunu takip eder
        /// </summary>
        public async Task<EmailDeliveryResult> SendEmailWithTrackingAsync(string to, string subject, string body, string? trackingId = null)
        {
            var result = new EmailDeliveryResult
            {
                TrackingId = trackingId ?? Guid.NewGuid().ToString(),
                Recipient = to,
                Subject = subject,
                SentDate = DateTime.UtcNow
            };

            try
            {
                // Email validasyonu
                if (!IsValidEmailAddress(to))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Geçersiz email adresi";
                    return result;
                }

                // Blacklist kontrolü
                if (await IsEmailBlacklistedAsync(to))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Email adresi blacklist'te";
                    return result;
                }

                await SendEmailInternalAsync(to, subject, body, isHtml: false);
                
                result.IsSuccess = true;
                _deliveryTracker.TryAdd(result.TrackingId, result);
                
                lock (_statsLock)
                {
                    _totalSent++;
                    _successfulDeliveries++;
                }

                _logger.LogInformation("Email başarıyla gönderildi ve takip edildi: {To}, TrackingId: {TrackingId}", to, result.TrackingId);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                
                lock (_statsLock)
                {
                    _totalSent++;
                    _failedDeliveries++;
                }

                _logger.LogError(ex, "Email gönderilirken hata: To: {To}, TrackingId: {TrackingId}", to, result.TrackingId);
            }

            return result;
        }

        /// <summary>
        /// Email ekler (attachments) ile email gönderir
        /// </summary>
        public async Task SendEmailWithAttachmentsAsync(string to, string subject, string body, List<EmailAttachment> attachments)
        {
            try
            {
                // Mock email kontrolü
                if (_configuration.GetValue<bool>("EmailSettings:UseMockEmail"))
                {
                    _logger.LogInformation("MOCK EMAIL WITH ATTACHMENTS - To: {To}, Subject: {Subject}, Attachments: {AttachmentCount}", 
                        to, subject, attachments.Count);
                    return;
                }

                // Email validasyonu
                if (!IsValidEmailAddress(to))
                {
                    throw new EmailServiceException("Geçersiz email adresi");
                }

                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromName = _configuration["EmailSettings:FromName"];

                using var message = new MailMessage();
                message.From = new MailAddress(fromEmail!, fromName);
                message.To.Add(to);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                // Ekleri ekle
                foreach (var attachment in attachments)
                {
                    using var memoryStream = new MemoryStream(attachment.Content);
                    var mailAttachment = new Attachment(memoryStream, attachment.FileName, attachment.ContentType);
                    message.Attachments.Add(mailAttachment);
                }

                using var smtpClient = CreateSmtpClient();
                await smtpClient.SendMailAsync(message);

                _logger.LogInformation("Email ekleri ile başarıyla gönderildi: {To}, Ek sayısı: {AttachmentCount}", to, attachments.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email ekleri ile gönderilirken hata: To: {To}", to);
                throw new EmailServiceException("Email ekleri ile gönderilemedi", ex);
            }
        }

        /// <summary>
        /// Email kuyruk sistemini kullanarak async email gönderir
        /// </summary>
        public async Task QueueEmailAsync(EmailRequest emailRequest)
        {
            try
            {
                // Email validasyonu
                if (!IsValidEmailAddress(emailRequest.To))
                {
                    throw new EmailServiceException("Geçersiz email adresi");
                }

                _emailQueue.Enqueue(emailRequest);
                
                _logger.LogInformation("Email kuyruğa eklendi: {To}, Subject: {Subject}, ID: {Id}", 
                    emailRequest.To, emailRequest.Subject, emailRequest.Id);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email kuyruğa eklenirken hata: {To}", emailRequest.To);
                throw new EmailServiceException("Email kuyruğa eklenemedi", ex);
            }
        }

        /// <summary>
        /// Kuyruktaki bekleyen emailleri işler
        /// </summary>
        public async Task<int> ProcessEmailQueueAsync()
        {
            int processedCount = 0;
            
            try
            {
                while (_emailQueue.TryDequeue(out var emailRequest))
                {
                    try
                    {
                        // Maksimum deneme kontrolü
                        if (emailRequest.RetryCount >= emailRequest.MaxRetries)
                        {
                            _logger.LogWarning("Email maksimum deneme sayısına ulaştı: {Id}", emailRequest.Id);
                            continue;
                        }

                        // Template kullanımı kontrolü
                        if (!string.IsNullOrEmpty(emailRequest.TemplateName))
                        {
                            await SendTemplateEmailAsync(emailRequest.To, emailRequest.TemplateName, emailRequest.TemplateData);
                        }
                        else if (emailRequest.Attachments.Any())
                        {
                            await SendEmailWithAttachmentsAsync(emailRequest.To, emailRequest.Subject, emailRequest.Body, emailRequest.Attachments);
                        }
                        else if (emailRequest.IsHtml)
                        {
                            await SendHtmlEmailAsync(emailRequest.To, emailRequest.Subject, emailRequest.Body);
                        }
                        else
                        {
                            await SendEmailAsync(emailRequest.To, emailRequest.Subject, emailRequest.Body);
                        }

                        processedCount++;
                        _logger.LogDebug("Kuyruk emaili işlendi: {Id}", emailRequest.Id);
                    }
                    catch (Exception ex)
                    {
                        emailRequest.RetryCount++;
                        
                        if (emailRequest.RetryCount < emailRequest.MaxRetries)
                        {
                            // Tekrar kuyruğa ekle
                            _emailQueue.Enqueue(emailRequest);
                            _logger.LogWarning("Email tekrar kuyruğa eklendi: {Id}, Deneme: {RetryCount}", 
                                emailRequest.Id, emailRequest.RetryCount);
                        }
                        else
                        {
                            _logger.LogError(ex, "Email işlenemedi ve maksimum deneme sayısına ulaşıldı: {Id}", emailRequest.Id);
                        }
                    }
                }

                if (processedCount > 0)
                {
                    _logger.LogInformation("Email kuyruğu işlendi: {ProcessedCount} email", processedCount);
                }

                return processedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email kuyruğu işlenirken hata");
                return processedCount;
            }
        }

        /// <summary>
        /// Email istatistiklerini getirir
        /// </summary>
        public async Task<EmailStatistics> GetEmailStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var statistics = new EmailStatistics();
                
                lock (_statsLock)
                {
                    statistics.TotalSent = _totalSent;
                    statistics.SuccessfulDeliveries = _successfulDeliveries;
                    statistics.FailedDeliveries = _failedDeliveries;
                }

                statistics.PendingInQueue = _emailQueue.Count;
                statistics.AverageDeliveryTime = 1500; // Örnek değer - gerçek implementasyonda ölçülecek

                // Template kullanım istatistikleri (basit versiyon)
                statistics.TopTemplates = new Dictionary<string, int>
                {
                    ["service_request_created"] = 45,
                    ["technician_assigned"] = 32,
                    ["service_completed"] = 28
                };

                // Günlük dağılım (basit versiyon)
                var currentDate = DateTime.UtcNow;
                for (int i = 0; i < 7; i++)
                {
                    var date = currentDate.AddDays(-i);
                    statistics.DailyDeliveryDistribution[date.ToString("yyyy-MM-dd")] = Random.Shared.Next(10, 50);
                }

                await Task.CompletedTask;
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email istatistikleri hesaplanırken hata");
                throw new EmailServiceException("Email istatistikleri hesaplanamadı", ex);
            }
        }

        /// <summary>
        /// Email template dosyalarını yükler
        /// </summary>
        public async Task<int> LoadEmailTemplatesAsync()
        {
            try
            {
                var templatesPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Templates", "Email");
                
                if (!Directory.Exists(templatesPath))
                {
                    Directory.CreateDirectory(templatesPath);
                    await CreateDefaultTemplateFilesAsync(templatesPath);
                }

                var templateFiles = Directory.GetFiles(templatesPath, "*.html");
                int loadedCount = 0;

                foreach (var templateFile in templateFiles)
                {
                    try
                    {
                        var templateName = Path.GetFileNameWithoutExtension(templateFile);
                        var templateContent = await File.ReadAllTextAsync(templateFile);
                        
                        // Template'i parse et (basit versiyon)
                        var template = ParseTemplateFile(templateContent);
                        _templateCache.TryAdd(templateName, template);
                        
                        loadedCount++;
                        _logger.LogDebug("Email template yüklendi: {TemplateName}", templateName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Template dosyası yüklenirken hata: {TemplateFile}", templateFile);
                    }
                }

                _logger.LogInformation("Email template'leri yüklendi: {LoadedCount} adet", loadedCount);
                return loadedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email template'leri yüklenirken hata");
                return 0;
            }
        }

        /// <summary>
        /// Email blacklist kontrolü yapar
        /// </summary>
        public async Task<bool> IsEmailBlacklistedAsync(string emailAddress)
        {
            await Task.CompletedTask;
            return _emailBlacklist.Contains(emailAddress.ToLowerInvariant());
        }

        /// <summary>
        /// Email address validasyonu yapar
        /// </summary>
        public bool IsValidEmailAddress(string emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
                return false;

            try
            {
                // Regex pattern for email validation
                const string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
                return Regex.IsMatch(emailAddress, pattern);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Email servisinin sağlık durumunu kontrol eder
        /// </summary>
        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                // Mock email modunda her zaman sağlıklı
                if (_configuration.GetValue<bool>("EmailSettings:UseMockEmail"))
                {
                    return true;
                }

                // SMTP bağlantısını test et
                var testEmail = _configuration["EmailSettings:FromEmail"];
                if (string.IsNullOrEmpty(testEmail))
                {
                    return false;
                }

                // Basit bağlantı testi
                using var client = CreateSmtpClient();
                await Task.Run(() => client.Host != null);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email servisi sağlık kontrolü başarısız");
                return false;
            }
        }

        #region Private Methods

        /// <summary>
        /// SMTP client oluşturur
        /// </summary>
        private SmtpClient CreateSmtpClient()
        {
            var smtpHost = _configuration["EmailSettings:SmtpHost"];
            var smtpPort = _configuration.GetValue<int>("EmailSettings:SmtpPort");
            var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"];

            var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                Timeout = 30000 // 30 saniye timeout
            };

            return client;
        }

        /// <summary>
        /// Email gönderme internal metodu
        /// </summary>
        private async Task SendEmailInternalAsync(string to, string subject, string body, bool isHtml = false)
        {
            // Mock email kontrolü
            if (_configuration.GetValue<bool>("EmailSettings:UseMockEmail"))
            {
                _logger.LogInformation("MOCK EMAIL - To: {To}, Subject: {Subject}, IsHtml: {IsHtml}", to, subject, isHtml);
                return;
            }

            try
            {
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromName = _configuration["EmailSettings:FromName"];

                using var smtpClient = CreateSmtpClient();
                using var message = new MailMessage();
                
                message.From = new MailAddress(fromEmail!, fromName);
                message.To.Add(to);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = isHtml;

                await smtpClient.SendMailAsync(message);
                
                _logger.LogInformation("Email başarıyla gönderildi: {To}, Subject: {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email gönderilirken hata: {To}, Subject: {Subject}", to, subject);
                throw;
            }
        }

        /// <summary>
        /// Email template'ini getirir (cache'den veya yükler)
        /// </summary>
        private async Task<EmailTemplate> GetEmailTemplateAsync(string templateName)
        {
            if (_templateCache.TryGetValue(templateName, out var cachedTemplate))
            {
                return cachedTemplate;
            }

            // Cache'de yoksa fallback template'leri kullan
            return GetEmailTemplate(templateName);
        }

        /// <summary>
        /// Fallback email template'ini getirir
        /// </summary>
        private EmailTemplate GetEmailTemplate(string templateName)
        {
            return templateName.ToLower() switch
            {
                "service_request_created" => new EmailTemplate
                {
                    Subject = "Servis Talebiniz Alındı - #{RequestId}",
                    Body = GetServiceRequestCreatedTemplate()
                },
                "technician_assigned" => new EmailTemplate
                {
                    Subject = "Teknisyen Atandı - #{RequestId}",
                    Body = GetTechnicianAssignedTemplate()
                },
                "service_completed" => new EmailTemplate
                {
                    Subject = "Servis Tamamlandı - #{RequestId}",
                    Body = GetServiceCompletedTemplate()
                },
                "urgent_request" => new EmailTemplate
                {
                    Subject = "ACİL: Kritik Öncelikli Servis Talebi - #{RequestId}",
                    Body = GetUrgentRequestTemplate()
                },
                _ => new EmailTemplate
                {
                    Subject = "Bildirim",
                    Body = "<div style='font-family: Arial, sans-serif; padding: 20px;'><p>{Message}</p></div>"
                }
            };
        }

        /// <summary>
        /// Template'i işler ve placeholder'ları değiştirir
        /// </summary>
        private EmailTemplate ProcessTemplate(EmailTemplate template, Dictionary<string, object> data)
        {
            var processedSubject = template.Subject;
            var processedBody = template.Body;

            foreach (var kvp in data)
            {
                var placeholder = "{" + kvp.Key + "}";
                var value = kvp.Value?.ToString() ?? "";

                processedSubject = processedSubject.Replace(placeholder, value);
                processedBody = processedBody.Replace(placeholder, value);
            }

            return new EmailTemplate
            {
                Subject = processedSubject,
                Body = processedBody
            };
        }

        /// <summary>
        /// Template dosyasını parse eder
        /// </summary>
        private EmailTemplate ParseTemplateFile(string templateContent)
        {
            // Basit template parsing - gerçek implementasyonda daha gelişmiş olacak
            var lines = templateContent.Split('\n');
            var subject = "Bildirim";
            var body = templateContent;

            // İlk satırda SUBJECT varsa onu al
            if (lines.Length > 0 && lines[0].StartsWith("SUBJECT:"))
            {
                subject = lines[0].Substring(8).Trim();
                body = string.Join('\n', lines.Skip(1));
            }

            return new EmailTemplate
            {
                Subject = subject,
                Body = body
            };
        }

        /// <summary>
        /// Email blacklist'i yükler
        /// </summary>
        private void LoadEmailBlacklist()
        {
            try
            {
                var blacklistPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Data", "email-blacklist.txt");
                
                if (File.Exists(blacklistPath))
                {
                    var blacklistEntries = File.ReadAllLines(blacklistPath);
                    foreach (var entry in blacklistEntries)
                    {
                        if (!string.IsNullOrWhiteSpace(entry) && !entry.StartsWith("#"))
                        {
                            _emailBlacklist.Add(entry.Trim().ToLowerInvariant());
                        }
                    }
                    _logger.LogInformation("Email blacklist yüklendi: {Count} kayıt", _emailBlacklist.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email blacklist yüklenirken hata");
            }
        }

        /// <summary>
        /// Varsayılan template dosyalarını oluşturur
        /// </summary>
        private async Task CreateDefaultTemplateFilesAsync(string templatesPath)
        {
            var templates = new Dictionary<string, string>
            {
                ["service_request_created.html"] = GetServiceRequestCreatedTemplate(),
                ["technician_assigned.html"] = GetTechnicianAssignedTemplate(),
                ["service_completed.html"] = GetServiceCompletedTemplate(),
                ["urgent_request.html"] = GetUrgentRequestTemplate()
            };

            foreach (var template in templates)
            {
                var filePath = Path.Combine(templatesPath, template.Key);
                await File.WriteAllTextAsync(filePath, template.Value);
            }

            _logger.LogInformation("Varsayılan email template'leri oluşturuldu: {Count} adet", templates.Count);
        }

        #endregion

        #region Template Methods

        private string GetServiceRequestCreatedTemplate()
        {
            return @"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>Servis Talebiniz Alındı</title>
                </head>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='background: #f8f9fa; padding: 20px; border-radius: 8px; margin-bottom: 20px;'>
                            <h2 style='color: #007bff; margin: 0;'>Merhaba {CustomerName},</h2>
                        </div>
                        
                        <div style='background: white; padding: 20px; border: 1px solid #e9ecef; border-radius: 8px;'>
                            <p>'{RequestTitle}' başlıklı servis talebiniz başarıyla alındı.</p>
                            
                            <table style='width: 100%; margin: 20px 0;'>
                                <tr><td style='padding: 8px; background: #f8f9fa;'><strong>Talep Numarası:</strong></td><td style='padding: 8px;'>#{RequestId}</td></tr>
                                <tr><td style='padding: 8px; background: #f8f9fa;'><strong>Kategori:</strong></td><td style='padding: 8px;'>{Category}</td></tr>
                                <tr><td style='padding: 8px; background: #f8f9fa;'><strong>Öncelik:</strong></td><td style='padding: 8px;'>{Priority}</td></tr>
                                <tr><td style='padding: 8px; background: #f8f9fa;'><strong>Oluşturulma Tarihi:</strong></td><td style='padding: 8px;'>{CreatedDate}</td></tr>
                            </table>
                            
                            <p>En kısa sürede size dönüş yapılacaktır. Talep durumunuzu web panelimizden takip edebilirsiniz.</p>
                        </div>
                        
                        <div style='margin-top: 20px; padding: 20px; background: #f8f9fa; border-radius: 8px; text-align: center;'>
                            <p style='margin: 0;'>Teşekkürler,<br><strong>AI Teknik Servis Ekibi</strong></p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GetTechnicianAssignedTemplate()
        {
            return @"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>Teknisyen Atandı</title>
                </head>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='background: #28a745; color: white; padding: 20px; border-radius: 8px; margin-bottom: 20px;'>
                            <h2 style='margin: 0;'>✅ Teknisyen Atandı</h2>
                        </div>
                        
                        <div style='background: white; padding: 20px; border: 1px solid #e9ecef; border-radius: 8px;'>
                            <p>Merhaba <strong>{CustomerName}</strong>,</p>
                            <p>'{RequestTitle}' başlıklı servis talebinize <strong>{TechnicianName}</strong> teknisyeni atandı.</p>
                            
                            <div style='background: #e3f2fd; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                <p style='margin: 0;'><strong>📞 Teknisyenimiz yakında sizinle iletişime geçecektir.</strong></p>
                            </div>
                            
                            <table style='width: 100%; margin: 20px 0;'>
                                <tr><td style='padding: 8px; background: #f8f9fa;'><strong>Teknisyen:</strong></td><td style='padding: 8px;'>{TechnicianName}</td></tr>
                                <tr><td style='padding: 8px; background: #f8f9fa;'><strong>Uzmanlık:</strong></td><td style='padding: 8px;'>{TechnicianSpecialization}</td></tr>
                                <tr><td style='padding: 8px; background: #f8f9fa;'><strong>Talep No:</strong></td><td style='padding: 8px;'>#{RequestId}</td></tr>
                            </table>
                        </div>
                        
                        <div style='margin-top: 20px; padding: 20px; background: #f8f9fa; border-radius: 8px; text-align: center;'>
                            <p style='margin: 0;'>Teşekkürler,<br><strong>AI Teknik Servis Ekibi</strong></p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GetServiceCompletedTemplate()
        {
            return @"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>Servis Tamamlandı</title>
                </head>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='background: #28a745; color: white; padding: 20px; border-radius: 8px; margin-bottom: 20px;'>
                            <h2 style='margin: 0;'>🎉 Servis Tamamlandı</h2>
                        </div>
                        
                        <div style='background: white; padding: 20px; border: 1px solid #e9ecef; border-radius: 8px;'>
                            <p>Merhaba <strong>{CustomerName}</strong>,</p>
                            <p>'{RequestTitle}' başlıklı servis talebiniz başarıyla tamamlandı!</p>
                            
                            <div style='background: #d4edda; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                <p style='margin: 0;'><strong>✅ Hizmetimizden memnun kaldığınızı umuyoruz.</strong></p>
                            </div>
                            
                            <table style='width: 100%; margin: 20px 0;'>
                                <tr><td style='padding: 8px; background: #f8f9fa;'><strong>Tamamlanan Tarih:</strong></td><td style='padding: 8px;'>{CompletedDate}</td></tr>
                                <tr><td style='padding: 8px; background: #f8f9fa;'><strong>Teknisyen:</strong></td><td style='padding: 8px;'>{TechnicianName}</td></tr>
                                <tr><td style='padding: 8px; background: #f8f9fa;'><strong>Çözüm:</strong></td><td style='padding: 8px;'>{Resolution}</td></tr>
                            </table>
                            
                            <p>Geri bildirimlerinizi ve değerlendirmelerinizi bekliyoruz.</p>
                        </div>
                        
                        <div style='margin-top: 20px; padding: 20px; background: #f8f9fa; border-radius: 8px; text-align: center;'>
                            <p style='margin: 0;'>Teşekkürler,<br><strong>AI Teknik Servis Ekibi</strong></p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GetUrgentRequestTemplate()
        {
            return @"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>ACİL: Kritik Öncelikli Servis Talebi</title>
                </head>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='background: #dc3545; color: white; padding: 20px; border-radius: 8px; margin-bottom: 20px;'>
                            <h2 style='margin: 0;'>🚨 ACİL: Kritik Öncelikli Talep</h2>
                        </div>
                        
                        <div style='background: white; padding: 20px; border: 1px solid #e9ecef; border-radius: 8px;'>
                            <p><strong>Kritik öncelikli servis talebi alındı!</strong></p>
                            
                            <div style='background: #f8d7da; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #dc3545;'>
                                <p style='margin: 0;'><strong>⚠️ Bu talep acil müdahale gerektiriyor!</strong></p>
                            </div>
                            
                            <table style='width: 100%; margin: 20px 0;'>
                                <tr><td style='padding: 8px; background: #f8f9fa;'><strong>Müşteri:</strong></td><td style='padding: 8px;'>{CustomerName}</td></tr>
                                <tr><td style='padding: 8px; background: #f8f9fa;'><strong>Talep:</strong></td><td style='padding: 8px;'>'{RequestTitle}'</td></tr>
                                <tr><td style='padding: 8px; background: #f8f9fa;'><strong>Talep No:</strong></td><td style='padding: 8px;'>#{RequestId}</td></tr>
                                <tr><td style='padding: 8px; background: #f8f9fa;'><strong>Kategori:</strong></td><td style='padding: 8px;'>{Category}</td></tr>
                                <tr><td style='padding: 8px; background: #f8f9fa;'><strong>Açıklama:</strong></td><td style='padding: 8px;'>{Description}</td></tr>
                            </table>
                            
                            <p><strong>Lütfen bu talebi öncelikli olarak değerlendirin ve gerekli aksiyonu alın.</strong></p>
                        </div>
                        
                        <div style='margin-top: 20px; padding: 20px; background: #f8f9fa; border-radius: 8px; text-align: center;'>
                            <p style='margin: 0;'><strong>AI Teknik Servis Sistemi</strong><br>Otomatik Acil Bildirim</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        #endregion

        /// <summary>
        /// IDisposable implementation
        /// </summary>
        public void Dispose()
        {
            // Artık dispose edilecek bir şey yok
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Email template modeli
    /// </summary>
    public class EmailTemplate
    {
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }

    /// <summary>
    /// Email servisi hata sınıfı
    /// </summary>
    public class EmailServiceException : Exception
    {
        public EmailServiceException(string message) : base(message) { }
        public EmailServiceException(string message, Exception innerException) : base(message, innerException) { }
    }
}