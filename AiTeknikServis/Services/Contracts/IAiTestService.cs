using AiTeknikServis.Services.Contracts;

namespace AiTeknikServis.Services.Contracts
{
    public interface IAiTestService
    {
        /// <summary>
        /// AI servisinin temel fonksiyonlarını test eder
        /// </summary>
        /// <returns>Test sonuçları</returns>
        Task<AiTestResult> RunBasicTestsAsync();

        /// <summary>
        /// Farklı senaryolarla AI servisini test eder
        /// </summary>
        /// <returns>Detaylı test sonuçları</returns>
        Task<List<AiTestScenarioResult>> RunScenarioTestsAsync();

        /// <summary>
        /// AI servisinin performansını ölçer
        /// </summary>
        /// <returns>Performans metrikleri</returns>
        Task<AiPerformanceMetrics> MeasurePerformanceAsync();
    }

    /// <summary>
    /// AI test sonuçlarını içeren model
    /// </summary>
    public class AiTestResult
    {
        public bool IsHealthy { get; set; }
        public bool CategoryPredictionWorks { get; set; }
        public bool PriorityPredictionWorks { get; set; }
        public bool RecommendationGenerationWorks { get; set; }
        public bool TechnicianSuggestionWorks { get; set; }
        public bool ComprehensiveAnalysisWorks { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public DateTime TestDate { get; set; } = DateTime.UtcNow;
        public TimeSpan TotalTestDuration { get; set; }
    }

    /// <summary>
    /// AI test senaryosu sonuçları
    /// </summary>
    public class AiTestScenarioResult
    {
        public string ScenarioName { get; set; } = string.Empty;
        public string TestDescription { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
        public object? Result { get; set; }
    }

    /// <summary>
    /// AI performans metrikleri
    /// </summary>
    public class AiPerformanceMetrics
    {
        public double AverageResponseTime { get; set; }
        public double MinResponseTime { get; set; }
        public double MaxResponseTime { get; set; }
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public double SuccessRate { get; set; }
        public DateTime MeasurementDate { get; set; } = DateTime.UtcNow;
    }
}