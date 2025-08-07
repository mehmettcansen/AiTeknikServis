using AiTeknikServis.Entities.Dtos.Report;

namespace AiTeknikServis.Services.Contracts
{
    public interface IReportService
    {
        Task<ServiceRequestReportDto> GenerateServiceRequestReportAsync(int serviceRequestId);
        Task<byte[]> GenerateServiceRequestPdfAsync(int serviceRequestId);
        Task<string> GenerateAndSaveAiReportAnalysisAsync(int serviceRequestId);
    }
}