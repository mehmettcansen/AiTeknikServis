using System.Diagnostics;
using AiTeknikServis.Entities.Models;
using AiTeknikServis.Services.Contracts;

namespace AiTeknikServis.Services
{
    public class AiTestService : IAiTestService
    {
        private readonly IAiPredictionService _aiPredictionService;
        private readonly ILogger<AiTestService> _logger;

        /// <summary>
        /// AiTestService constructor - AI prediction servisi ve logger'ı enjekte eder
        /// </summary>
        public AiTestService(IAiPredictionService aiPredictionService, ILogger<AiTestService> logger)
        {
            _aiPredictionService = aiPredictionService;
            _logger = logger;
        }

        /// <summary>
        /// AI servisinin temel fonksiyonlarını test eder
        /// </summary>
        public async Task<AiTestResult> RunBasicTestsAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new AiTestResult();

            _logger.LogInformation("AI servisi temel testleri başlatılıyor...");

            try
            {
                // 1. Sağlık kontrolü
                result.IsHealthy = await TestHealthCheckAsync(result.Errors);

                // 2. Kategori tahmini testi
                result.CategoryPredictionWorks = await TestCategoryPredictionAsync(result.Errors);

                // 3. Öncelik tahmini testi
                result.PriorityPredictionWorks = await TestPriorityPredictionAsync(result.Errors);

                // 4. Öneri oluşturma testi
                result.RecommendationGenerationWorks = await TestRecommendationGenerationAsync(result.Errors);

                // 5. Teknisyen önerisi testi
                result.TechnicianSuggestionWorks = await TestTechnicianSuggestionAsync(result.Errors);

                // 6. Kapsamlı analiz testi
                result.ComprehensiveAnalysisWorks = await TestComprehensiveAnalysisAsync(result.Errors);

                stopwatch.Stop();
                result.TotalTestDuration = stopwatch.Elapsed;

                _logger.LogInformation("AI servisi temel testleri tamamlandı. Süre: {Duration}ms", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI servisi temel testleri sırasında beklenmeyen hata");
                result.Errors.Add($"Beklenmeyen hata: {ex.Message}");
                stopwatch.Stop();
                result.TotalTestDuration = stopwatch.Elapsed;
            }

            return result;
        }

        /// <summary>
        /// Farklı senaryolarla AI servisini test eder
        /// </summary>
        public async Task<List<AiTestScenarioResult>> RunScenarioTestsAsync()
        {
            var scenarios = new List<AiTestScenarioResult>();

            _logger.LogInformation("AI servisi senaryo testleri başlatılıyor...");

            // Senaryo 1: Yazılım sorunu
            scenarios.Add(await RunScenarioAsync("Yazılım Sorunu", 
                "Bilgisayarımda program açılmıyor, sürekli hata veriyor", 
                async () => await _aiPredictionService.PredictCategoryAsync("Bilgisayarımda program açılmıyor, sürekli hata veriyor")));

            // Senaryo 2: Donanım sorunu
            scenarios.Add(await RunScenarioAsync("Donanım Sorunu", 
                "Bilgisayarım açılmıyor, güç düğmesine bastığımda hiçbir şey olmuyor", 
                async () => await _aiPredictionService.PredictCategoryAsync("Bilgisayarım açılmıyor, güç düğmesine bastığımda hiçbir şey olmuyor")));

            // Senaryo 3: Ağ sorunu
            scenarios.Add(await RunScenarioAsync("Ağ Sorunu", 
                "İnternete bağlanamıyorum, wifi görünüyor ama bağlanmıyor", 
                async () => await _aiPredictionService.PredictCategoryAsync("İnternete bağlanamıyorum, wifi görünüyor ama bağlanmıyor")));

            // Senaryo 4: Güvenlik sorunu
            scenarios.Add(await RunScenarioAsync("Güvenlik Sorunu", 
                "Bilgisayarımda virüs var gibi, yavaş çalışıyor ve garip pencereler açılıyor", 
                async () => await _aiPredictionService.PredictCategoryAsync("Bilgisayarımda virüs var gibi, yavaş çalışıyor ve garip pencereler açılıyor")));

            // Senaryo 5: Bakım
            scenarios.Add(await RunScenarioAsync("Bakım", 
                "Bilgisayarımın genel bakımını yaptırmak istiyorum", 
                async () => await _aiPredictionService.PredictCategoryAsync("Bilgisayarımın genel bakımını yaptırmak istiyorum")));

            // Senaryo 6: Öncelik testi - Kritik
            scenarios.Add(await RunScenarioAsync("Kritik Öncelik", 
                "Sunucumuz çöktü, tüm sistem durdu, acil müdahale gerekiyor", 
                async () => await _aiPredictionService.PredictPriorityAsync("Sunucumuz çöktü, tüm sistem durdu, acil müdahale gerekiyor", ServiceCategory.HardwareIssue)));

            // Senaryo 7: Öncelik testi - Düşük
            scenarios.Add(await RunScenarioAsync("Düşük Öncelik", 
                "Yazıcımın mürekkebi bitiyor, yenisini değiştirmek istiyorum", 
                async () => await _aiPredictionService.PredictPriorityAsync("Yazıcımın mürekkebi bitiyor, yenisini değiştirmek istiyorum", ServiceCategory.Maintenance)));

            // Senaryo 8: Kapsamlı analiz
            scenarios.Add(await RunScenarioAsync("Kapsamlı Analiz", 
                "E-posta programım çalışmıyor", 
                async () => await _aiPredictionService.AnalyzeServiceRequestAsync("E-posta programım çalışmıyor", "Email Sorunu", "Microsoft Outlook")));

            _logger.LogInformation("AI servisi senaryo testleri tamamlandı. Toplam senaryo: {Count}", scenarios.Count);

            return scenarios;
        }

        /// <summary>
        /// AI servisinin performansını ölçer
        /// </summary>
        public async Task<AiPerformanceMetrics> MeasurePerformanceAsync()
        {
            var metrics = new AiPerformanceMetrics();
            var responseTimes = new List<double>();
            var testCount = 10; // 10 test isteği gönder

            _logger.LogInformation("AI servisi performans ölçümü başlatılıyor... Test sayısı: {Count}", testCount);

            for (int i = 0; i < testCount; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    // Basit bir kategori tahmini yap
                    await _aiPredictionService.PredictCategoryAsync($"Test mesajı {i + 1}: Bilgisayarım çalışmıyor");
                    
                    stopwatch.Stop();
                    responseTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
                    metrics.SuccessfulRequests++;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(ex, "Performans testi {Index} başarısız", i + 1);
                    metrics.FailedRequests++;
                }

                // Testler arası kısa bekleme
                await Task.Delay(100);
            }

            metrics.TotalRequests = testCount;
            
            if (responseTimes.Any())
            {
                metrics.AverageResponseTime = responseTimes.Average();
                metrics.MinResponseTime = responseTimes.Min();
                metrics.MaxResponseTime = responseTimes.Max();
            }

            metrics.SuccessRate = metrics.TotalRequests > 0 
                ? (double)metrics.SuccessfulRequests / metrics.TotalRequests * 100 
                : 0;

            _logger.LogInformation("AI servisi performans ölçümü tamamlandı. Başarı oranı: {SuccessRate}%, Ortalama yanıt süresi: {AvgTime}ms", 
                metrics.SuccessRate, metrics.AverageResponseTime);

            return metrics;
        }

        #region Private Test Methods

        /// <summary>
        /// AI servisinin sağlık durumunu test eder
        /// </summary>
        private async Task<bool> TestHealthCheckAsync(List<string> errors)
        {
            try
            {
                var isHealthy = await _aiPredictionService.IsHealthyAsync();
                if (!isHealthy)
                {
                    errors.Add("AI servisi sağlık kontrolü başarısız");
                }
                return isHealthy;
            }
            catch (Exception ex)
            {
                errors.Add($"Sağlık kontrolü hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Kategori tahmin fonksiyonunu test eder
        /// </summary>
        private async Task<bool> TestCategoryPredictionAsync(List<string> errors)
        {
            try
            {
                var category = await _aiPredictionService.PredictCategoryAsync("Bilgisayarım açılmıyor");
                return Enum.IsDefined(typeof(ServiceCategory), category);
            }
            catch (Exception ex)
            {
                errors.Add($"Kategori tahmini hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Öncelik tahmin fonksiyonunu test eder
        /// </summary>
        private async Task<bool> TestPriorityPredictionAsync(List<string> errors)
        {
            try
            {
                var priority = await _aiPredictionService.PredictPriorityAsync("Acil durum", ServiceCategory.HardwareIssue);
                return Enum.IsDefined(typeof(Priority), priority);
            }
            catch (Exception ex)
            {
                errors.Add($"Öncelik tahmini hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Öneri oluşturma fonksiyonunu test eder
        /// </summary>
        private async Task<bool> TestRecommendationGenerationAsync(List<string> errors)
        {
            try
            {
                var recommendation = await _aiPredictionService.GenerateRecommendationAsync("Yazılım sorunu", ServiceCategory.SoftwareIssue);
                return !string.IsNullOrEmpty(recommendation);
            }
            catch (Exception ex)
            {
                errors.Add($"Öneri oluşturma hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Teknisyen önerisi fonksiyonunu test eder
        /// </summary>
        private async Task<bool> TestTechnicianSuggestionAsync(List<string> errors)
        {
            try
            {
                var suggestions = await _aiPredictionService.SuggestTechniciansAsync(ServiceCategory.SoftwareIssue, Priority.High);
                return suggestions != null; // Boş liste de geçerli
            }
            catch (Exception ex)
            {
                errors.Add($"Teknisyen önerisi hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Kapsamlı analiz fonksiyonunu test eder
        /// </summary>
        private async Task<bool> TestComprehensiveAnalysisAsync(List<string> errors)
        {
            try
            {
                var analysis = await _aiPredictionService.AnalyzeServiceRequestAsync("Test açıklaması", "Test başlığı");
                return analysis != null && !string.IsNullOrEmpty(analysis.Recommendation);
            }
            catch (Exception ex)
            {
                errors.Add($"Kapsamlı analiz hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tek bir test senaryosunu çalıştırır
        /// </summary>
        private async Task<AiTestScenarioResult> RunScenarioAsync<T>(string scenarioName, string description, Func<Task<T>> testAction)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new AiTestScenarioResult
            {
                ScenarioName = scenarioName,
                TestDescription = description
            };

            try
            {
                var testResult = await testAction();
                result.Success = true;
                result.Result = testResult;
                
                _logger.LogDebug("Senaryo '{Scenario}' başarılı. Sonuç: {Result}", scenarioName, testResult);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                
                _logger.LogWarning(ex, "Senaryo '{Scenario}' başarısız", scenarioName);
            }
            finally
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
            }

            return result;
        }

        #endregion
    }
}