using System.Text;
using System.Text.Json;
using AiTeknikServis.Data;
using AiTeknikServis.Entities.Models;
using AiTeknikServis.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace AiTeknikServis.Infrastructure.AI
{
    public class GeminiAiService : IAiPredictionService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GeminiAiService> _logger;

        /// <summary>
        /// GeminiAiService constructor - HTTP client, konfigürasyon ve veritabanı bağlamını enjekte eder
        /// </summary>
        public GeminiAiService(HttpClient httpClient, IConfiguration configuration, ApplicationDbContext context, ILogger<GeminiAiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Servis talebi açıklamasına göre kategori tahmini yapar
        /// </summary>
        public async Task<ServiceCategory> PredictCategoryAsync(string description)
        {
            try
            {
                var prompt = CreateCategoryPredictionPrompt(description);
                var response = await CallGeminiApiAsync(prompt);
                return ParseCategoryFromResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kategori tahmini yapılırken hata oluştu: {Description}", description);
                throw new AiServiceException("Kategori tahmini yapılamadı", ex);
            }
        }

        /// <summary>
        /// Servis talebi açıklaması ve kategorisine göre öncelik tahmini yapar
        /// </summary>
        public async Task<Priority> PredictPriorityAsync(string description, ServiceCategory category)
        {
            try
            {
                var prompt = CreatePriorityPredictionPrompt(description, category);
                var response = await CallGeminiApiAsync(prompt);
                return ParsePriorityFromResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öncelik tahmini yapılırken hata oluştu: {Description}, Kategori: {Category}", description, category);
                throw new AiServiceException("Öncelik tahmini yapılamadı", ex);
            }
        }

        /// <summary>
        /// Servis talebi için AI destekli öneri oluşturur
        /// </summary>
        public async Task<string> GenerateRecommendationAsync(string description, ServiceCategory category)
        {
            try
            {
                var prompt = CreateRecommendationPrompt(description, category);
                var response = await CallGeminiApiAsync(prompt);
                return ParseRecommendationFromResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öneri oluşturulurken hata oluştu: {Description}, Kategori: {Category}", description, category);
                throw new AiServiceException("Öneri oluşturulamadı", ex);
            }
        }

        /// <summary>
        /// Kategori ve önceliğe göre uygun teknisyenleri önerir
        /// </summary>

        // kullanılmıyor.
        public async Task<List<int>> SuggestTechniciansAsync(ServiceCategory category, Priority priority)
        {
            try
            {
                // Önce uygun teknisyenleri veritabanından getir
                var availableTechnicians = await GetAvailableTechniciansForCategoryAsync(category);
                
                if (!availableTechnicians.Any())
                {
                    _logger.LogWarning("Kategori {Category} için uygun teknisyen bulunamadı", category);
                    return new List<int>();
                }

                // AI'dan teknisyen önerisi al
                var prompt = CreateTechnicianSuggestionPrompt(category, priority, availableTechnicians);
                var response = await CallGeminiApiAsync(prompt);
                return ParseTechnicianSuggestionsFromResponse(response, availableTechnicians);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teknisyen önerisi yapılırken hata oluştu: Kategori: {Category}, Öncelik: {Priority}", category, priority);
                throw new AiServiceException("Teknisyen önerisi yapılamadı", ex);
            }
        }

        /// <summary>
        /// Servis talebi için kapsamlı AI analizi yapar ve sonuçları döndürür
        /// </summary>
        public async Task<AiAnalysisResult> AnalyzeServiceRequestAsync(string description, string title, string? productInfo = null)
        {
            try
            {
                var prompt = CreateComprehensiveAnalysisPrompt(description, title, productInfo);
                var response = await CallGeminiApiAsync(prompt);
                return ParseComprehensiveAnalysisFromResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kapsamlı analiz yapılırken hata oluştu: {Title}", title);
                throw new AiServiceException("Kapsamlı analiz yapılamadı", ex);
            }
        }

        /// <summary>
        /// AI servisinin sağlık durumunu kontrol eder
        /// </summary>
        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                var testPrompt = "Bu bir test mesajıdır. Sadece 'OK' yanıtı verin.";
                var response = await CallGeminiApiAsync(testPrompt);
                return !string.IsNullOrEmpty(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI servisi sağlık kontrolü başarısız");
                return false;
            }
        }

        /// <summary>
        /// Tamamlanan servis talebi için AI destekli rapor analizi yapar
        /// </summary>
        public async Task<string> GenerateReportAnalysisAsync(object reportData)
        {
            try
            {
                var prompt = CreateReportAnalysisPrompt(reportData);
                var response = await CallGeminiApiAsync(prompt);
                return ParseReportAnalysisFromResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rapor analizi yapılırken hata oluştu");
                
                // Fallback analiz döndür
                return GenerateFallbackReportAnalysis(reportData);
            }
        }

        /// <summary>
        /// AI tahmin sonucunu veritabanına kaydeder
        /// </summary>
        public async Task<AiPrediction> SavePredictionAsync(int serviceRequestId, AiAnalysisResult analysisResult)
        {
            try
            {
                // Recommendation metnini veritabanı limitine göre güvenli şekilde kısalt
                var truncatedRecommendation = analysisResult.Recommendation;
                if (!string.IsNullOrEmpty(truncatedRecommendation) && truncatedRecommendation.Length > 1900)
                {
                    // Kelime ortasında kesmemek için son boşluğu bul
                    var lastSpace = truncatedRecommendation.LastIndexOf(' ', 1900);
                    if (lastSpace > 1800) // Çok kısa olmasın
                    {
                        truncatedRecommendation = truncatedRecommendation.Substring(0, lastSpace) + "...";
                    }
                    else
                    {
                        truncatedRecommendation = truncatedRecommendation.Substring(0, 1900) + "...";
                    }
                }

                // RawAiResponse'u da kısalt
                var truncatedRawResponse = analysisResult.RawAiResponse?.Length > 4950
                    ? analysisResult.RawAiResponse.Substring(0, 4950) + "..."
                    : analysisResult.RawAiResponse;

                var aiPrediction = new AiPrediction
                {
                    ServiceRequestId = serviceRequestId,
                    PredictedCategory = analysisResult.PredictedCategory,
                    PredictedPriority = analysisResult.PredictedPriority,
                    Recommendation = truncatedRecommendation,
                    ConfidenceScore = analysisResult.ConfidenceScore,
                    RawAiResponse = truncatedRawResponse,
                    CreatedDate = DateTime.UtcNow
                };

                _context.AiPredictions.Add(aiPrediction);
                await _context.SaveChangesAsync();

                _logger.LogInformation("AI tahmini kaydedildi: ServiceRequestId: {ServiceRequestId}, Kategori: {Category}, Öncelik: {Priority}", 
                    serviceRequestId, analysisResult.PredictedCategory, analysisResult.PredictedPriority);

                return aiPrediction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI tahmini kaydedilirken hata oluştu: ServiceRequestId: {ServiceRequestId}", serviceRequestId);
                throw new AiServiceException("AI tahmini kaydedilemedi", ex);
            }
        }

        #region Private Methods

        /// <summary>
        /// Gemini API'ye HTTP isteği gönderir
        /// </summary>
        private async Task<string> CallGeminiApiAsync(string prompt)
        {
            var apiKey = _configuration["AiSettings:GeminiApiKey"];
            var apiUrl = _configuration["AiSettings:GeminiApiUrl"];

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiUrl))
            {
                throw new AiServiceException("Gemini API konfigürasyonu eksik");
            }

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var requestUrl = $"{apiUrl}?key={apiKey}";
            var response = await _httpClient.PostAsync(requestUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API hatası: {StatusCode}, İçerik: {Content}", response.StatusCode, errorContent);
                throw new AiServiceException($"Gemini API hatası: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Gemini API yanıtı alındı. Response uzunluğu: {Length}", responseContent.Length);
            return ExtractTextFromGeminiResponse(responseContent);
        }

        /// <summary>
        /// Gemini API yanıtından metin içeriğini çıkarır
        /// </summary>
        private string ExtractTextFromGeminiResponse(string jsonResponse)
        {
            try
            {
                using var document = JsonDocument.Parse(jsonResponse);
                var candidates = document.RootElement.GetProperty("candidates");
                var firstCandidate = candidates[0];
                var content = firstCandidate.GetProperty("content");
                var parts = content.GetProperty("parts");
                var firstPart = parts[0];
                return firstPart.GetProperty("text").GetString() ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini yanıtı parse edilirken hata: {Response}", jsonResponse);
                throw new AiServiceException("Gemini yanıtı parse edilemedi", ex);
            }
        }

        /// <summary>
        /// Kategori tahmini için prompt oluşturur
        /// </summary>
        private string CreateCategoryPredictionPrompt(string description)
        {
            return $@"
Aşağıdaki teknik servis talebini analiz et ve hangi kategoriye ait olduğunu belirle.

Mevcut kategoriler:
1. SoftwareIssue (Yazılım Sorunu)
2. HardwareIssue (Donanım Sorunu)  
3. Maintenance (Bakım)
4. SecurityIssue (Güvenlik Sorunu)
5. NetworkIssue (Ağ Bağlantı Sorunu)

Servis Talebi: {description}

Sadece kategori adını döndür (örnek: SoftwareIssue). Açıklama yapma.";
        }

        /// <summary>
        /// Öncelik tahmini için prompt oluşturur
        /// </summary>
        private string CreatePriorityPredictionPrompt(string description, ServiceCategory category)
        {
            return $@"
Aşağıdaki teknik servis talebinin öncelik seviyesini belirle.

Kategori: {category}
Servis Talebi: {description}

Öncelik seviyeleri:
1. Low (Düşük) - Acil olmayan, ertelenebilir işler
2. Normal - Standart işler, normal sürede çözülmeli
3. High (Yüksek) - Önemli işler, hızlı çözülmeli
4. Critical (Kritik) - Acil işler, hemen çözülmeli

Sadece öncelik seviyesini döndür (örnek: High). Açıklama yapma.";
        }

        /// <summary>
        /// Öneri oluşturma için prompt oluşturur
        /// </summary>
        private string CreateRecommendationPrompt(string description, ServiceCategory category)
        {
           

            return $@"
Sen bir profesyonel teknik servis şirketinin AI asistanısın. Aşağıdaki müşteri talebi için detaylı çözüm önerisi oluştur.

Kategori: {category}
Müşteri Talebi: {description}

ÖNEMLİ KURALLAR:
- Müşteriyi başka bir servise yönlendirme
- ""Yetkili servise götürün"" gibi ifadeler kullanma
- Kendi teknik servis ekibinizin uzmanlığını vurgula
- Müşteriyi hemen servise getirmeye teşvik et

Lütfen şu yapıda yanıt ver:
1. ACİL DURUM MÜDAHALESİ: İlk yapılması gerekenler (güvenlik önlemleri)
2. PROFESYONEL DEĞERLENDİRME: Neden teknik servisimize gelmesi gerektiği
3. SERVİS SÜRECİMİZ: Nasıl bir hizmet alacağı
4. ÖNLEYICI TEDBİRLER: Gelecekte nasıl korunacağı

ÖNEMLİ: Maksimum 250 kelime ile kısa, öz ve güven verici bir dil kullan.

Öneri:";
        }

        /// <summary>
        /// Teknisyen önerisi için prompt oluşturur
        /// </summary>
        
        // KULLANILMIYOR.
        private string CreateTechnicianSuggestionPrompt(ServiceCategory category, Priority priority, List<Technician> availableTechnicians)
        {
            var technicianInfo = string.Join("\n", availableTechnicians.Select(t => 
                $"ID: {t.Id}, Ad: {t.FirstName} {t.LastName}, Uzmanlık: {t.Specializations}, Aktif Görev: {t.Assignments?.Count(a => a.Status == WorkAssignmentStatus.Assigned || a.Status == WorkAssignmentStatus.InProgress) ?? 0}"));

            return $@"
Aşağıdaki teknik servis talebi için en uygun teknisyenleri öner.

Kategori: {category}
Öncelik: {priority}

Mevcut Teknisyenler:
{technicianInfo}

En uygun 3 teknisyenin ID'lerini virgülle ayırarak döndür (örnek: 1,3,5). Açıklama yapma.";
        }

        /// <summary>
        /// Kapsamlı analiz için prompt oluşturur
        /// </summary>
        private string CreateComprehensiveAnalysisPrompt(string description, string title, string? productInfo)
        {
            var productInfoText = !string.IsNullOrEmpty(productInfo) ? $"\nÜrün Bilgisi: {productInfo}" : "";

            return $@"
Sen bir profesyonel teknik servis şirketinin AI asistanısın. Aşağıdaki müşteri talebini analiz et ve JSON formatında yanıt ver.

Başlık: {title}
Açıklama: {description}{productInfoText}

ÖNEMLİ: Recommendation kısmında müşteriyi başka servise yönlendirme, kendi teknik servis ekibinizin uzmanlığını vurgula.

JSON formatı:
{{
    ""category"": ""SoftwareIssue|HardwareIssue|Maintenance|SecurityIssue|NetworkIssue"",
    ""priority"": ""Low|Normal|High|Critical"",
    ""recommendation"": ""Müşteriyi kendi servisinize yönlendiren, güven verici çözüm önerisi (max 200 kelime)"",
    ""confidenceScore"": 0.85,
    ""estimatedResolutionHours"": 4,
    ""urgencyExplanation"": ""Neden hemen servisinize gelmesi gerektiğinin açıklaması""
}}

Sadece JSON döndür, başka açıklama yapma.";
        }

        /// <summary>
        /// Belirli kategori için uygun teknisyenleri getirir
        /// </summary>
        private async Task<List<Technician>> GetAvailableTechniciansForCategoryAsync(ServiceCategory category)
        {
            var categoryKeyword = category switch
            {
                ServiceCategory.SoftwareIssue => "yazılım",
                ServiceCategory.HardwareIssue => "donanım",
                ServiceCategory.NetworkIssue => "ağ",
                ServiceCategory.SecurityIssue => "güvenlik",
                ServiceCategory.Maintenance => "bakım",
                _ => ""
            };

            return await _context.Technicians
                .Include(t => t.Assignments)
                .Where(t => t.IsActive)
                .Where(t => string.IsNullOrEmpty(categoryKeyword) || 
                           (t.Specializations != null && t.Specializations.ToLower().Contains(categoryKeyword)))
                .ToListAsync();
        }

        /// <summary>
        /// AI yanıtından kategori parse eder
        /// </summary>
        private ServiceCategory ParseCategoryFromResponse(string response)
        {
            var cleanResponse = response.Trim().ToLower();
            
            return cleanResponse switch
            {
                var s when s.Contains("software") || s.Contains("yazılım") => ServiceCategory.SoftwareIssue,
                var s when s.Contains("hardware") || s.Contains("donanım") => ServiceCategory.HardwareIssue,
                var s when s.Contains("network") || s.Contains("ağ") => ServiceCategory.NetworkIssue,
                var s when s.Contains("security") || s.Contains("güvenlik") => ServiceCategory.SecurityIssue,
                var s when s.Contains("maintenance") || s.Contains("bakım") => ServiceCategory.Maintenance,
                _ => ServiceCategory.SoftwareIssue // Varsayılan
            };
        }

        /// <summary>
        /// AI yanıtından öncelik parse eder
        /// </summary>
        private Priority ParsePriorityFromResponse(string response)
        {
            var cleanResponse = response.Trim().ToLower();
            
            return cleanResponse switch
            {
                var s when s.Contains("critical") || s.Contains("kritik") => Priority.Critical,
                var s when s.Contains("high") || s.Contains("yüksek") => Priority.High,
                var s when s.Contains("low") || s.Contains("düşük") => Priority.Low,
                _ => Priority.Normal // Varsayılan
            };
        }

        /// <summary>
        /// AI yanıtından öneri parse eder
        /// </summary>
        private string ParseRecommendationFromResponse(string response)
        {
            return response.Trim();
        }

        /// <summary>
        /// AI yanıtından teknisyen önerilerini parse eder
        /// </summary>
        private List<int> ParseTechnicianSuggestionsFromResponse(string response, List<Technician> availableTechnicians)
        {
            try
            {
                var ids = response.Trim()
                    .Split(',')
                    .Select(id => int.TryParse(id.Trim(), out var result) ? result : 0)
                    .Where(id => id > 0 && availableTechnicians.Any(t => t.Id == id))
                    .Take(3)
                    .ToList();

                return ids.Any() ? ids : availableTechnicians.Take(1).Select(t => t.Id).ToList();
            }
            catch
            {
                // Hata durumunda ilk uygun teknisyeni döndür
                return availableTechnicians.Take(1).Select(t => t.Id).ToList();
            }
        }

        /// <summary>
        /// AI yanıtından kapsamlı analiz sonucunu parse eder
        /// </summary>
        private AiAnalysisResult ParseComprehensiveAnalysisFromResponse(string response)
        {
            try
            {
                // Markdown code block işaretlerini temizle
                var cleanedResponse = response.Trim();
                if (cleanedResponse.StartsWith("```json"))
                {
                    cleanedResponse = cleanedResponse.Substring(7);
                }
                if (cleanedResponse.StartsWith("```"))
                {
                    cleanedResponse = cleanedResponse.Substring(3);
                }
                if (cleanedResponse.EndsWith("```"))
                {
                    cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
                }
                cleanedResponse = cleanedResponse.Trim();

                using var document = JsonDocument.Parse(cleanedResponse);
                var root = document.RootElement;

                return new AiAnalysisResult
                {
                    PredictedCategory = ParseCategoryFromResponse(root.GetProperty("category").GetString() ?? ""),
                    PredictedPriority = ParsePriorityFromResponse(root.GetProperty("priority").GetString() ?? ""),
                    Recommendation = root.GetProperty("recommendation").GetString() ?? "",
                    ConfidenceScore = (float)root.GetProperty("confidenceScore").GetDouble(),
                    EstimatedResolutionHours = root.TryGetProperty("estimatedResolutionHours", out var hours) ? hours.GetInt32() : null,
                    UrgencyExplanation = root.TryGetProperty("urgencyExplanation", out var urgency) ? urgency.GetString() ?? "" : "",
                    RawAiResponse = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kapsamlı analiz yanıtı parse edilirken hata. Response: {Response}, Hata: {Error}", response, ex.Message);
                
                // Fallback: Basit parse
                return new AiAnalysisResult
                {
                    PredictedCategory = ServiceCategory.SoftwareIssue,
                    PredictedPriority = Priority.Normal,
                    Recommendation = "AI analizi tamamlanamadı, manuel inceleme gerekli.",
                    ConfidenceScore = 0.5f,
                    RawAiResponse = response
                };
            }
        }

        /// <summary>
        /// Rapor analizi için prompt oluşturur
        /// </summary>
        private string CreateReportAnalysisPrompt(object reportData)
        {
            // reportData'yı JSON string'e çevir
            var reportJson = JsonSerializer.Serialize(reportData, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return $@"
Sen bir profesyonel teknik servis şirketinin uzman AI analistisisin. Aşağıdaki tamamlanmış servis talebi raporunu analiz et ve profesyonel bir değerlendirme yap.

RAPOR VERİLERİ:
{reportJson}

GÖREV: Bu raporu analiz ederek aşağıdaki konularda profesyonel bir değerlendirme yap:

1. SERVİS KALİTESİ ANALİZİ:
   - Tamamlanma süresinin değerlendirmesi
   - Maliyet etkinliği analizi
   - Teknisyen performansı

2. SÜREÇ VERİMLİLİĞİ:
   - Planlanan vs gerçekleşen süre/maliyet karşılaştırması
   - İyileştirme önerileri
   - Kategori bazlı performans değerlendirmesi

3. MÜŞTERİ MEMNUNİYETİ TAHMİNİ:
   - Hizmet kalitesi göstergeleri
   - İletişim etkinliği
   - Genel memnuniyet tahmini

4. GELECEKTEKİ ÖNERİLER:
   - Benzer sorunlar için öneriler
   - Önleyici bakım tavsiyeleri
   - Sistem iyileştirme önerileri

ÖNEMLI KURALLAR:
- Profesyonel ve objektif bir dil kullan
- Sayısal verilere dayalı analiz yap
- Yapıcı eleştiri ve öneriler sun
- Müşteri odaklı yaklaşım benimse
- Maksimum 400 kelime ile öz ve etkili bir analiz yap

YANIT FORMATINI KULLAN:
📊 SERVİS PERFORMANS ANALİZİ:
[Performans değerlendirmesi]

⚡ VERİMLİLİK DEĞERLENDİRMESİ:
[Verimlilik analizi]

😊 MÜŞTERİ MEMNUNİYETİ TAHMİNİ:
[Memnuniyet analizi]

🔮 GELECEKTEKİ ÖNERİLER:
[İyileştirme önerileri]

Analiz:";
        }

        /// <summary>
        /// AI yanıtından rapor analizini parse eder
        /// </summary>
        private string ParseReportAnalysisFromResponse(string response)
        {
            return response.Trim();
        }

        /// <summary>
        /// AI servisi çalışmadığında fallback analiz oluşturur
        /// </summary>
        private string GenerateFallbackReportAnalysis(object reportData)
        {
            try
            {
                // reportData'dan temel bilgileri çıkar
                var reportType = reportData.GetType();
                var completionDaysProperty = reportType.GetProperty("CompletionDays");
                var categoryProperty = reportType.GetProperty("Category");
                var estimatedHoursProperty = reportType.GetProperty("EstimatedHours");
                var actualHoursProperty = reportType.GetProperty("ActualHours");
                var estimatedCostProperty = reportType.GetProperty("EstimatedCost");
                var actualCostProperty = reportType.GetProperty("ActualCost");

                var completionDays = completionDaysProperty?.GetValue(reportData) as int? ?? 0;
                var category = categoryProperty?.GetValue(reportData)?.ToString() ?? "";
                var estimatedHours = estimatedHoursProperty?.GetValue(reportData) as int?;
                var actualHours = actualHoursProperty?.GetValue(reportData) as int?;
                var estimatedCost = estimatedCostProperty?.GetValue(reportData) as decimal?;
                var actualCost = actualCostProperty?.GetValue(reportData) as decimal?;

                var analysis = new StringBuilder();
                
                analysis.AppendLine("📊 SERVİS PERFORMANS ANALİZİ:");
                analysis.AppendLine($"Bu servis talebi {completionDays} günde tamamlanmıştır.");
                
                if (estimatedHours.HasValue && actualHours.HasValue)
                {
                    var efficiency = estimatedHours.Value > 0 
                        ? (double)actualHours.Value / (double)estimatedHours.Value 
                        : 1.0;
                    
                    if (efficiency <= 0.8)
                        analysis.AppendLine("Tahmini süreden daha hızlı tamamlanmış, verimli bir çalışma gerçekleştirilmiştir.");
                    else if (efficiency > 1.2)
                        analysis.AppendLine("Tahmini süreden daha uzun sürmüş, gelecekte daha iyi planlama yapılabilir.");
                    else
                        analysis.AppendLine("Tahmini süreye yakın bir zamanda tamamlanmıştır.");
                }

                analysis.AppendLine();
                analysis.AppendLine("⚡ VERİMLİLİK DEĞERLENDİRMESİ:");
                
                if (estimatedCost.HasValue && actualCost.HasValue)
                {
                    var costEfficiency = estimatedCost.Value > 0 
                        ? (double)actualCost.Value / (double)estimatedCost.Value 
                        : 1.0;
                    
                    if (costEfficiency <= 0.9)
                        analysis.AppendLine("Maliyet tahmini altında kalınmış, bütçe yönetimi başarılıdır.");
                    else if (costEfficiency > 1.1)
                        analysis.AppendLine("Maliyet tahmini aşılmış, gelecekte daha dikkatli bütçeleme yapılmalıdır.");
                }

                var categoryAnalysis = category switch
                {
                    "SoftwareIssue" => "Yazılım sorunları genellikle hızlı çözülebilir ancak detaylı analiz gerektirebilir.",
                    "HardwareIssue" => "Donanım sorunları fiziksel müdahale gerektirebilir ve parça temini süreyi etkileyebilir.",
                    "NetworkIssue" => "Ağ sorunları sistem genelinde etki yaratabilir, öncelikli çözüm gerektirir.",
                    "SecurityIssue" => "Güvenlik sorunları kritik öneme sahiptir ve acil müdahale gerektirir.",
                    "Maintenance" => "Bakım işlemleri düzenli yapıldığında sistem performansını artırır.",
                    _ => "Genel teknik destek sağlanmıştır."
                };
                
                analysis.AppendLine(categoryAnalysis);

                analysis.AppendLine();
                analysis.AppendLine("😊 MÜŞTERİ MEMNUNİYETİ TAHMİNİ:");
                analysis.AppendLine("Servis talebi başarıyla tamamlanmış, müşteri memnuniyeti yüksek olması beklenmektedir.");

                analysis.AppendLine();
                analysis.AppendLine("🔮 GELECEKTEKİ ÖNERİLER:");
                analysis.AppendLine("Benzer sorunların önlenmesi için düzenli bakım ve kontroller önerilir.");
                analysis.AppendLine("Müşteri eğitimi ile basit sorunların önüne geçilebilir.");

                return analysis.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallback rapor analizi oluşturulurken hata");
                return "Rapor analizi şu anda mevcut değil. Lütfen daha sonra tekrar deneyin.";
            }
        }

        #endregion
    }

    /// <summary>
    /// AI servisi ile ilgili hataları temsil eden özel exception sınıfı
    /// </summary>
    public class AiServiceException : Exception
    {
        public AiServiceException(string message) : base(message) { }
        public AiServiceException(string message, Exception innerException) : base(message, innerException) { }
    }
}