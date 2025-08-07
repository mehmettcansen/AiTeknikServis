namespace AiTeknikServis.Entities.Dtos.Dashboard
{
    /// <summary>
    /// Teknisyen iş yükü DTO'su
    /// </summary>
    public class TechnicianWorkloadDto
    {
        public int TechnicianId { get; set; }
        public string TechnicianName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Specializations { get; set; }
        public bool IsActive { get; set; }
        
        // İş yükü bilgileri
        public int TotalAssignments { get; set; }
        public int PendingAssignments { get; set; }
        public int InProgressAssignments { get; set; }
        public int CompletedAssignments { get; set; }
        public int CancelledAssignments { get; set; }
        
        // Performans metrikleri
        public double CompletionRate { get; set; }
        public double AverageCompletionDays { get; set; }
        public int OverdueAssignments { get; set; }
        
        // Son aktivite
        public DateTime? LastAssignmentDate { get; set; }
        public DateTime? LastCompletionDate { get; set; }
        
        // İş yükü durumu
        public WorkloadStatus WorkloadStatus { get; set; }
        public string WorkloadStatusText { get; set; } = string.Empty;
        public string WorkloadStatusColor { get; set; } = string.Empty;
        
        // Son görevler
        public List<RecentAssignmentDto> RecentAssignments { get; set; } = new();
    }
    
    /// <summary>
    /// Son görev DTO'su
    /// </summary>
    public class RecentAssignmentDto
    {
        public int AssignmentId { get; set; }
        public int ServiceRequestId { get; set; }
        public string ServiceRequestTitle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public int DaysInProgress { get; set; }
        public bool IsOverdue { get; set; }
    }
    
    /// <summary>
    /// İş yükü durumu
    /// </summary>
    public enum WorkloadStatus
    {
        Available,    // Müsait
        Normal,       // Normal
        Busy,         // Yoğun
        Overloaded    // Aşırı yüklü
    }
}