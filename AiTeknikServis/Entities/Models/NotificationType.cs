namespace AiTeknikServis.Entities.Models
{
    public enum NotificationType
    {
        ServiceRequestCreated = 1,
        ServiceRequestAssigned = 2,
        ServiceRequestStatusChanged = 3,
        ServiceRequestCompleted = 4,
        TechnicianAssigned = 5,
        UrgentRequest = 6,
        SystemAlert = 7
    }
}