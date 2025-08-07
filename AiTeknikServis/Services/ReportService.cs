using AiTeknikServis.Services.Contracts;
using AiTeknikServis.Repositories.Contracts;
using AiTeknikServis.Entities.Dtos.Report;
using AiTeknikServis.Entities.Models;
using AutoMapper;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Text;

namespace AiTeknikServis.Services
{
    public class ReportService : IReportService
    {
        private readonly IServiceRequestRepository _serviceRequestRepository;
        private readonly IAiPredictionService _aiPredictionService;
        private readonly IMapper _mapper;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            IServiceRequestRepository serviceRequestRepository,
            IAiPredictionService aiPredictionService,
            IMapper mapper,
            ILogger<ReportService> logger)
        {
            _serviceRequestRepository = serviceRequestRepository;
            _aiPredictionService = aiPredictionService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ServiceRequestReportDto> GenerateServiceRequestReportAsync(int serviceRequestId)
        {
            try
            {
                var serviceRequest = await _serviceRequestRepository.GetByIdAsync(serviceRequestId);
                if (serviceRequest == null)
                    throw new ArgumentException("Servis talebi bulunamadı.");

                var reportDto = new ServiceRequestReportDto
                {
                    ServiceRequestId = serviceRequest.Id,
                    Title = serviceRequest.Title,
                    Description = serviceRequest.Description,
                    CustomerName = serviceRequest.Customer?.FirstName + " " + serviceRequest.Customer?.LastName,
                    CustomerPhone = serviceRequest.Phone,
                    TechnicianName = serviceRequest.AssignedTechnician?.FirstName + " " + serviceRequest.AssignedTechnician?.LastName ?? "Atanmamış",
                    Category = serviceRequest.Category,
                    CategoryName = GetCategoryName(serviceRequest.Category),
                    Priority = serviceRequest.Priority,
                    PriorityName = GetPriorityName(serviceRequest.Priority),
                    Status = serviceRequest.Status,
                    StatusName = GetStatusName(serviceRequest.Status),
                    CreatedDate = serviceRequest.CreatedDate,
                    CompletedDate = serviceRequest.CompletedDate,
                    CompletionDays = serviceRequest.CompletedDate.HasValue 
                        ? (int)(serviceRequest.CompletedDate.Value - serviceRequest.CreatedDate).TotalDays 
                        : 0,
                    ProductInfo = serviceRequest.ProductInfo,
                    Resolution = serviceRequest.Resolution,
                    EstimatedCost = serviceRequest.EstimatedCost,
                    ActualCost = serviceRequest.ActualCost,
                    EstimatedHours = serviceRequest.EstimatedHours,
                    ActualHours = serviceRequest.ActualHours
                };

                // Süreç hareketlerini oluştur
                reportDto.ProcessMovements = GenerateProcessMovements(serviceRequest);

                // AI analizlerini ekle
                if (serviceRequest.AiPredictions?.Any() == true)
                {
                    reportDto.AiAnalyses = serviceRequest.AiPredictions.Select(ai => new AiAnalysisDto
                    {
                        Recommendation = ai.Recommendation ?? "",
                        ConfidenceScore = ai.ConfidenceScore,
                        CreatedDate = ai.CreatedDate,
                        SuggestedTechnician = ai.SuggestedTechnician
                    }).ToList();
                }

                // AI rapor özetini getir (kaydedilmiş analizi kullan)
                reportDto.AiReportSummary = GetStoredAiReportSummary(serviceRequest);

                return reportDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rapor oluşturulurken hata: {ServiceRequestId}", serviceRequestId);
                throw;
            }
        }

        public async Task<byte[]> GenerateServiceRequestPdfAsync(int serviceRequestId)
        {
            try
            {
                var reportData = await GenerateServiceRequestReportAsync(serviceRequestId);
                
                using var memoryStream = new MemoryStream();
                var document = new Document(PageSize.A4, 50, 50, 50, 50);
                var writer = PdfWriter.GetInstance(document, memoryStream);
                
                document.Open();

                // Başlık
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.DarkGray);
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.Black);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.Black);
                var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.Gray);

                // Başlık
                var title = new Paragraph($"SERVİS TALEBİ RAPORU #{reportData.ServiceRequestId}", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                document.Add(title);

                // Tarih
                var dateInfo = new Paragraph($"Rapor Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}", smallFont)
                {
                    Alignment = Element.ALIGN_RIGHT,
                    SpacingAfter = 20
                };
                document.Add(dateInfo);

                // Temel Bilgiler Tablosu
                var basicInfoTable = new PdfPTable(2) { WidthPercentage = 100 };
                basicInfoTable.SetWidths(new float[] { 1, 2 });

                AddTableRow(basicInfoTable, "Talep Başlığı:", reportData.Title, headerFont, normalFont);
                AddTableRow(basicInfoTable, "Müşteri:", reportData.CustomerName, headerFont, normalFont);
                AddTableRow(basicInfoTable, "Telefon:", reportData.CustomerPhone ?? "-", headerFont, normalFont);
                AddTableRow(basicInfoTable, "Teknisyen:", reportData.TechnicianName, headerFont, normalFont);
                AddTableRow(basicInfoTable, "Kategori:", reportData.CategoryName, headerFont, normalFont);
                AddTableRow(basicInfoTable, "Öncelik:", reportData.PriorityName, headerFont, normalFont);
                AddTableRow(basicInfoTable, "Durum:", reportData.StatusName, headerFont, normalFont);
                AddTableRow(basicInfoTable, "Oluşturulma:", reportData.CreatedDate.ToString("dd.MM.yyyy HH:mm"), headerFont, normalFont);
                
                if (reportData.CompletedDate.HasValue)
                {
                    AddTableRow(basicInfoTable, "Tamamlanma:", reportData.CompletedDate.Value.ToString("dd.MM.yyyy HH:mm"), headerFont, normalFont);
                    AddTableRow(basicInfoTable, "Tamamlanma Süresi:", $"{reportData.CompletionDays} gün", headerFont, normalFont);
                }

                document.Add(basicInfoTable);
                document.Add(new Paragraph(" ", normalFont) { SpacingAfter = 10 });

                // Açıklama
                if (!string.IsNullOrEmpty(reportData.Description))
                {
                    document.Add(new Paragraph("SORUN AÇIKLAMASI", headerFont) { SpacingAfter = 5 });
                    document.Add(new Paragraph(reportData.Description, normalFont) { SpacingAfter = 15 });
                }

                // Ürün Bilgisi
                if (!string.IsNullOrEmpty(reportData.ProductInfo))
                {
                    document.Add(new Paragraph("ÜRÜN/CİHAZ BİLGİSİ", headerFont) { SpacingAfter = 5 });
                    document.Add(new Paragraph(reportData.ProductInfo, normalFont) { SpacingAfter = 15 });
                }

                // Çözüm
                if (!string.IsNullOrEmpty(reportData.Resolution))
                {
                    document.Add(new Paragraph("ÇÖZÜM", headerFont) { SpacingAfter = 5 });
                    document.Add(new Paragraph(reportData.Resolution, normalFont) { SpacingAfter = 15 });
                }

                // Maliyet ve Süre Bilgileri
                if (reportData.EstimatedCost.HasValue || reportData.ActualCost.HasValue || 
                    reportData.EstimatedHours.HasValue || reportData.ActualHours.HasValue)
                {
                    document.Add(new Paragraph("MALİYET VE SÜRE BİLGİLERİ", headerFont) { SpacingAfter = 5 });
                    
                    var costTable = new PdfPTable(2) { WidthPercentage = 100 };
                    costTable.SetWidths(new float[] { 1, 1 });

                    if (reportData.EstimatedCost.HasValue)
                        AddTableRow(costTable, "Tahmini Maliyet:", $"₺{reportData.EstimatedCost.Value:N2}", headerFont, normalFont);
                    
                    if (reportData.ActualCost.HasValue)
                        AddTableRow(costTable, "Gerçek Maliyet:", $"₺{reportData.ActualCost.Value:N2}", headerFont, normalFont);
                    
                    if (reportData.EstimatedHours.HasValue)
                        AddTableRow(costTable, "Tahmini Süre:", $"{reportData.EstimatedHours.Value} saat", headerFont, normalFont);
                    
                    if (reportData.ActualHours.HasValue)
                        AddTableRow(costTable, "Gerçek Süre:", $"{reportData.ActualHours.Value} saat", headerFont, normalFont);

                    document.Add(costTable);
                    document.Add(new Paragraph(" ", normalFont) { SpacingAfter = 10 });
                }

                // Süreç Hareketleri
                if (reportData.ProcessMovements.Any())
                {
                    document.Add(new Paragraph("SÜREÇ HAREKETLERİ", headerFont) { SpacingAfter = 5 });
                    
                    foreach (var movement in reportData.ProcessMovements.OrderBy(m => m.Date))
                    {
                        var movementText = $"• {movement.Date:dd.MM.yyyy HH:mm} - {movement.Action}";
                        if (!string.IsNullOrEmpty(movement.PerformedBy))
                            movementText += $" ({movement.PerformedBy})";
                        
                        document.Add(new Paragraph(movementText, normalFont) { SpacingAfter = 3 });
                    }
                    
                    document.Add(new Paragraph(" ", normalFont) { SpacingAfter = 10 });
                }

                // AI Analizi
                if (reportData.AiAnalyses.Any())
                {
                    document.Add(new Paragraph("YAPAY ZEKA ANALİZİ", headerFont) { SpacingAfter = 5 });
                    
                    foreach (var analysis in reportData.AiAnalyses)
                    {
                        var analysisText = $"• Güven Skoru: %{(analysis.ConfidenceScore * 100):F0} - {analysis.Recommendation}";
                        document.Add(new Paragraph(analysisText, normalFont) { SpacingAfter = 3 });
                    }
                    
                    document.Add(new Paragraph(" ", normalFont) { SpacingAfter = 10 });
                }

                // AI Rapor Özeti
                if (!string.IsNullOrEmpty(reportData.AiReportSummary))
                {
                    document.Add(new Paragraph("YAPAY ZEKA RAPOR DEĞERLENDİRMESİ", headerFont) { SpacingAfter = 5 });
                    document.Add(new Paragraph(reportData.AiReportSummary, normalFont) { SpacingAfter = 15 });
                }

                document.Close();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF raporu oluşturulurken hata: {ServiceRequestId}", serviceRequestId);
                throw;
            }
        }

        public async Task<string> GenerateAndSaveAiReportAnalysisAsync(int serviceRequestId)
        {
            try
            {
                var serviceRequest = await _serviceRequestRepository.GetByIdAsync(serviceRequestId);
                if (serviceRequest == null)
                    throw new ArgumentException("Servis talebi bulunamadı.");

                // Eğer zaten analiz yapılmışsa, mevcut analizi döndür
                if (!string.IsNullOrEmpty(serviceRequest.AiReportAnalysis))
                {
                    return serviceRequest.AiReportAnalysis;
                }

                // Rapor verilerini hazırla
                var reportDto = new ServiceRequestReportDto
                {
                    ServiceRequestId = serviceRequest.Id,
                    Title = serviceRequest.Title,
                    Description = serviceRequest.Description,
                    CustomerName = serviceRequest.Customer?.FirstName + " " + serviceRequest.Customer?.LastName,
                    CustomerPhone = serviceRequest.Phone,
                    TechnicianName = serviceRequest.AssignedTechnician?.FirstName + " " + serviceRequest.AssignedTechnician?.LastName ?? "Atanmamış",
                    Category = serviceRequest.Category,
                    CategoryName = GetCategoryName(serviceRequest.Category),
                    Priority = serviceRequest.Priority,
                    PriorityName = GetPriorityName(serviceRequest.Priority),
                    Status = serviceRequest.Status,
                    StatusName = GetStatusName(serviceRequest.Status),
                    CreatedDate = serviceRequest.CreatedDate,
                    CompletedDate = serviceRequest.CompletedDate,
                    CompletionDays = serviceRequest.CompletedDate.HasValue 
                        ? (int)(serviceRequest.CompletedDate.Value - serviceRequest.CreatedDate).TotalDays 
                        : 0,
                    ProductInfo = serviceRequest.ProductInfo,
                    Resolution = serviceRequest.Resolution,
                    EstimatedCost = serviceRequest.EstimatedCost,
                    ActualCost = serviceRequest.ActualCost,
                    EstimatedHours = serviceRequest.EstimatedHours,
                    ActualHours = serviceRequest.ActualHours
                };

                // AI analizini yap
                var aiAnalysis = await _aiPredictionService.GenerateReportAnalysisAsync(reportDto);
                
                string finalAnalysis;
                if (!string.IsNullOrEmpty(aiAnalysis) && aiAnalysis != "Rapor analizi şu anda mevcut değil. Lütfen daha sonra tekrar deneyin.")
                {
                    finalAnalysis = aiAnalysis;
                }
                else
                {
                    // AI başarısız olursa fallback analiz kullan
                    finalAnalysis = GenerateFallbackAnalysis(reportDto);
                }

                // Analizi veritabanına kaydet
                serviceRequest.AiReportAnalysis = finalAnalysis;
                serviceRequest.AiAnalysisDate = DateTime.UtcNow;
                await _serviceRequestRepository.UpdateAsync(serviceRequest);

                return finalAnalysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI rapor analizi oluşturulurken ve kaydedilirken hata: {ServiceRequestId}", serviceRequestId);
                
                // Hata durumunda fallback analiz oluştur ve kaydet
                try
                {
                    var serviceRequest = await _serviceRequestRepository.GetByIdAsync(serviceRequestId);
                    if (serviceRequest != null)
                    {
                        var reportDto = new ServiceRequestReportDto
                        {
                            ServiceRequestId = serviceRequest.Id,
                            Title = serviceRequest.Title,
                            Description = serviceRequest.Description,
                            Category = serviceRequest.Category,
                            Priority = serviceRequest.Priority,
                            Status = serviceRequest.Status,
                            CreatedDate = serviceRequest.CreatedDate,
                            CompletedDate = serviceRequest.CompletedDate,
                            CompletionDays = serviceRequest.CompletedDate.HasValue 
                                ? (int)(serviceRequest.CompletedDate.Value - serviceRequest.CreatedDate).TotalDays 
                                : 0,
                            EstimatedCost = serviceRequest.EstimatedCost,
                            ActualCost = serviceRequest.ActualCost,
                            EstimatedHours = serviceRequest.EstimatedHours,
                            ActualHours = serviceRequest.ActualHours
                        };

                        var fallbackAnalysis = GenerateFallbackAnalysis(reportDto);
                        serviceRequest.AiReportAnalysis = fallbackAnalysis;
                        serviceRequest.AiAnalysisDate = DateTime.UtcNow;
                        await _serviceRequestRepository.UpdateAsync(serviceRequest);
                        
                        return fallbackAnalysis;
                    }
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "Fallback analiz kaydedilirken hata: {ServiceRequestId}", serviceRequestId);
                }

                return "Rapor analizi oluşturulurken bir hata oluştu.";
            }
        }

        private string GetStoredAiReportSummary(ServiceRequest serviceRequest)
        {
            if (!string.IsNullOrEmpty(serviceRequest.AiReportAnalysis))
            {
                return serviceRequest.AiReportAnalysis;
            }

            // Eğer kaydedilmiş analiz yoksa, basit bir mesaj döndür
            return "AI analizi henüz yapılmamış. Analiz, servis talebi tamamlandığında otomatik olarak oluşturulur.";
        }

        private string GenerateFallbackAnalysis(ServiceRequestReportDto reportData)
        {
            var summary = new StringBuilder();
            
            summary.AppendLine($"Bu servis talebi {reportData.CompletionDays} günde tamamlanmıştır.");
            
            if (reportData.EstimatedHours.HasValue && reportData.ActualHours.HasValue)
            {
                var efficiency = reportData.EstimatedHours.Value > 0 
                    ? (double)reportData.ActualHours.Value / (double)reportData.EstimatedHours.Value 
                    : 1.0;
                
                if (efficiency <= 0.8)
                    summary.AppendLine("Tahmini süreden daha hızlı tamamlanmış, verimli bir çalışma gerçekleştirilmiştir.");
                else if (efficiency > 1.2)
                    summary.AppendLine("Tahmini süreden daha uzun sürmüş, gelecekte daha iyi planlama yapılabilir.");
                else
                    summary.AppendLine("Tahmini süreye yakın bir zamanda tamamlanmıştır.");
            }

            if (reportData.EstimatedCost.HasValue && reportData.ActualCost.HasValue)
            {
                var costEfficiency = reportData.EstimatedCost.Value > 0 
                    ? (double)reportData.ActualCost.Value / (double)reportData.EstimatedCost.Value 
                    : 1.0;
                
                if (costEfficiency <= 0.9)
                    summary.AppendLine("Maliyet tahmini altında kalınmış, bütçe yönetimi başarılıdır.");
                else if (costEfficiency > 1.1)
                    summary.AppendLine("Maliyet tahmini aşılmış, gelecekte daha dikkatli bütçeleme yapılmalıdır.");
            }

            var categoryAnalysis = reportData.Category switch
            {
                ServiceCategory.SoftwareIssue => "Yazılım sorunları genellikle hızlı çözülebilir ancak detaylı analiz gerektirebilir.",
                ServiceCategory.HardwareIssue => "Donanım sorunları fiziksel müdahale gerektirebilir ve parça temini süreyi etkileyebilir.",
                ServiceCategory.NetworkIssue => "Ağ sorunları sistem genelinde etki yaratabilir, öncelikli çözüm gerektirir.",
                ServiceCategory.SecurityIssue => "Güvenlik sorunları kritik öneme sahiptir ve acil müdahale gerektirir.",
                ServiceCategory.Maintenance => "Bakım işlemleri düzenli yapıldığında sistem performansını artırır.",
                _ => "Genel teknik destek sağlanmıştır."
            };
            
            summary.AppendLine(categoryAnalysis);

            if (reportData.AiAnalyses.Any())
            {
                var avgConfidence = reportData.AiAnalyses.Average(a => a.ConfidenceScore) * 100;
                summary.AppendLine($"AI analiz güven skoru ortalama %{avgConfidence:F0} olarak hesaplanmıştır.");
            }

            return summary.ToString();
        }

        private List<ProcessMovementDto> GenerateProcessMovements(ServiceRequest serviceRequest)
        {
            var movements = new List<ProcessMovementDto>();

            // Talep oluşturulması
            movements.Add(new ProcessMovementDto
            {
                Date = serviceRequest.CreatedDate,
                Action = "Talep Oluşturuldu",
                Description = "Müşteri tarafından servis talebi oluşturuldu",
                PerformedBy = serviceRequest.Customer?.FirstName + " " + serviceRequest.Customer?.LastName
            });

            // Teknisyen ataması
            if (serviceRequest.AssignedTechnicianId.HasValue)
            {
                movements.Add(new ProcessMovementDto
                {
                    Date = serviceRequest.CreatedDate.AddMinutes(5), // Yaklaşık atama zamanı
                    Action = "Teknisyen Atandı",
                    Description = "Talep teknisyene atandı",
                    PerformedBy = "Sistem"
                });
            }

            // Durum değişiklikleri (basit simülasyon)
            if (serviceRequest.Status == ServiceStatus.InProgress || serviceRequest.Status == ServiceStatus.Completed)
            {
                movements.Add(new ProcessMovementDto
                {
                    Date = serviceRequest.CreatedDate.AddHours(1),
                    Action = "İşleme Alındı",
                    Description = "Teknisyen tarafından işleme alındı",
                    PerformedBy = serviceRequest.AssignedTechnician?.FirstName + " " + serviceRequest.AssignedTechnician?.LastName
                });
            }

            // Tamamlanma
            if (serviceRequest.CompletedDate.HasValue)
            {
                movements.Add(new ProcessMovementDto
                {
                    Date = serviceRequest.CompletedDate.Value,
                    Action = "Talep Tamamlandı",
                    Description = "Servis talebi başarıyla tamamlandı",
                    PerformedBy = serviceRequest.AssignedTechnician?.FirstName + " " + serviceRequest.AssignedTechnician?.LastName
                });
            }

            return movements;
        }

        private void AddTableRow(PdfPTable table, string label, string value, Font labelFont, Font valueFont)
        {
            var labelCell = new PdfPCell(new Phrase(label, labelFont))
            {
                Border = Rectangle.NO_BORDER,
                PaddingBottom = 5,
                VerticalAlignment = Element.ALIGN_TOP
            };
            
            var valueCell = new PdfPCell(new Phrase(value, valueFont))
            {
                Border = Rectangle.NO_BORDER,
                PaddingBottom = 5,
                VerticalAlignment = Element.ALIGN_TOP
            };

            table.AddCell(labelCell);
            table.AddCell(valueCell);
        }

        private string GetCategoryName(ServiceCategory category) => category switch
        {
            ServiceCategory.SoftwareIssue => "Yazılım Sorunu",
            ServiceCategory.HardwareIssue => "Donanım Sorunu",
            ServiceCategory.NetworkIssue => "Ağ Sorunu",
            ServiceCategory.SecurityIssue => "Güvenlik Sorunu",
            ServiceCategory.Maintenance => "Bakım",
            _ => "Diğer"
        };

        private string GetPriorityName(Priority priority) => priority switch
        {
            Priority.Critical => "Kritik",
            Priority.High => "Yüksek",
            Priority.Normal => "Normal",
            Priority.Low => "Düşük",
            _ => "Belirsiz"
        };

        private string GetStatusName(ServiceStatus status) => status switch
        {
            ServiceStatus.Pending => "Beklemede",
            ServiceStatus.InProgress => "İşlemde",
            ServiceStatus.OnHold => "Bekletildi",
            ServiceStatus.Completed => "Tamamlandı",
            ServiceStatus.Cancelled => "İptal Edildi",
            ServiceStatus.Rejected => "Reddedildi",
            _ => "Belirsiz"
        };
    }
}