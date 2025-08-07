using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Services.Contracts
{
    public interface IAiPredictionService
    {
        /// <summary>
        /// Servis talebi açıklamasına göre kategori tahmini yapar
        /// </summary>
        /// <param name="description">Servis talebi açıklaması</param>
        /// <returns>Tahmin edilen kategori</returns>
        Task<ServiceCategory> PredictCategoryAsync(string description);

        /// <summary>
        /// Servis talebi açıklaması ve kategorisine göre öncelik tahmini yapar
        /// </summary>
        /// <param name="description">Servis talebi açıklaması</param>
        /// <param name="category">Servis kategorisi</param>
        /// <returns>Tahmin edilen öncelik</returns>
        Task<Priority> PredictPriorityAsync(string description, ServiceCategory category);

        /// <summary>
        /// Servis talebi için AI destekli öneri oluşturur
        /// </summary>
        /// <param name="description">Servis talebi açıklaması</param>
        /// <param name="category">Servis kategorisi</param>
        /// <returns>AI tarafından oluşturulan öneri</returns>
        Task<string> GenerateRecommendationAsync(string description, ServiceCategory category);

        /// <summary>
        /// Kategori ve önceliğe göre uygun teknisyenleri önerir
        /// </summary>
        /// <param name="category">Servis kategorisi</param>
        /// <param name="priority">Öncelik seviyesi</param>
        /// <returns>Önerilen teknisyen ID'leri listesi</returns>
        Task<List<int>> SuggestTechniciansAsync(ServiceCategory category, Priority priority);

        /// <summary>
        /// Servis talebi için kapsamlı AI analizi yapar ve sonuçları döndürür
        /// </summary>
        /// <param name="description">Servis talebi açıklaması</param>
        /// <param name="title">Servis talebi başlığı</param>
        /// <param name="productInfo">Ürün bilgisi (opsiyonel)</param>
        /// <returns>AI analiz sonuçları</returns>
        Task<AiAnalysisResult> AnalyzeServiceRequestAsync(string description, string title, string? productInfo = null);

        /// <summary>
        /// AI servisinin sağlık durumunu kontrol eder
        /// </summary>
        /// <returns>AI servisi çalışıyor mu?</returns>
        Task<bool> IsHealthyAsync();

        /// <summary>
        /// AI tahmin sonucunu veritabanına kaydeder
        /// </summary>
        /// <param name="serviceRequestId">Servis talebi ID'si</param>
        /// <param name="analysisResult">AI analiz sonucu</param>
        /// <returns>Kaydedilen AI tahmini</returns>
        Task<AiPrediction> SavePredictionAsync(int serviceRequestId, AiAnalysisResult analysisResult);

        /// <summary>
        /// Tamamlanan servis talebi için AI destekli rapor analizi yapar
        /// </summary>
        /// <param name="reportData">Rapor verileri</param>
        /// <returns>AI tarafından oluşturulan profesyonel rapor analizi</returns>
        Task<string> GenerateReportAnalysisAsync(object reportData);
    }

    /// <summary>
    /// AI analiz sonuçlarını içeren model
    /// </summary>
    public class AiAnalysisResult
    {
        /// <summary>
        /// Tahmin edilen kategori
        /// </summary>
        public ServiceCategory PredictedCategory { get; set; }

        /// <summary>
        /// Tahmin edilen öncelik
        /// </summary>
        public Priority PredictedPriority { get; set; }

        /// <summary>
        /// AI tarafından oluşturulan öneri
        /// </summary>
        public string Recommendation { get; set; } = string.Empty;

        /// <summary>
        /// Tahmin güven skoru (0-1 arası)
        /// </summary>
        public float ConfidenceScore { get; set; }

        /// <summary>
        /// Önerilen teknisyen ID'leri
        /// </summary>
        public List<int> SuggestedTechnicianIds { get; set; } = new List<int>();

        /// <summary>
        /// Ham AI yanıtı (debug için)
        /// </summary>
        public string RawAiResponse { get; set; } = string.Empty;

        /// <summary>
        /// Tahmini çözüm süresi (saat)
        /// </summary>
        public int? EstimatedResolutionHours { get; set; }

        /// <summary>
        /// Aciliyet seviyesi açıklaması
        /// </summary>
        public string UrgencyExplanation { get; set; } = string.Empty;
    }
}