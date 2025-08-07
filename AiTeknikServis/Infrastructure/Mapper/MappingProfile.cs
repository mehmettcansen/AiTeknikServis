using AutoMapper;
using AiTeknikServis.Entities.Models;
using AiTeknikServis.Entities.Dtos.ServiceRequest;
using AiTeknikServis.Entities.Dtos.User;
using AiTeknikServis.Entities.Dtos.WorkAssignment;
using AiTeknikServis.Entities.Dtos.Notification;

namespace AiTeknikServis.Infrastructure.Mapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ServiceRequest Mappings
            CreateMap<ServiceRequest, ServiceRequestResponseDto>()
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? $"{src.Customer.FirstName} {src.Customer.LastName}" : ""))
                .ForMember(dest => dest.TechnicianName, opt => opt.MapFrom(src => src.AssignedTechnician != null ? $"{src.AssignedTechnician.FirstName} {src.AssignedTechnician.LastName}" : null))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.ToString()))
                .ForMember(dest => dest.PriorityName, opt => opt.MapFrom(src => src.Priority.ToString()))
                .ForMember(dest => dest.StatusName, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.Files, opt => opt.MapFrom(src => src.Files))
                .ForMember(dest => dest.AiPredictions, opt => opt.MapFrom(src => src.AiPredictions))
                .ForMember(dest => dest.WorkAssignments, opt => opt.MapFrom(src => src.WorkAssignments));

            CreateMap<ServiceRequestCreateDto, ServiceRequest>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore()) // AI tarafından belirlenecek
                .ForMember(dest => dest.Priority, opt => opt.Ignore()) // AI tarafından belirlenecek
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ServiceStatus.Pending))
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CompletedDate, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedTechnicianId, opt => opt.Ignore())
                .ForMember(dest => dest.Customer, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedTechnician, opt => opt.Ignore())
                .ForMember(dest => dest.Files, opt => opt.Ignore())
                .ForMember(dest => dest.AiPredictions, opt => opt.Ignore())
                .ForMember(dest => dest.WorkAssignments, opt => opt.Ignore())
                .ForMember(dest => dest.Notifications, opt => opt.Ignore())
                .ForMember(dest => dest.Resolution, opt => opt.Ignore())
                .ForMember(dest => dest.ActualCost, opt => opt.Ignore())
                .ForMember(dest => dest.ActualHours, opt => opt.Ignore());

            CreateMap<ServiceRequestUpdateDto, ServiceRequest>()
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CustomerId, opt => opt.Ignore())
                .ForMember(dest => dest.Customer, opt => opt.Ignore())
                .ForMember(dest => dest.Files, opt => opt.Ignore())
                .ForMember(dest => dest.AiPredictions, opt => opt.Ignore())
                .ForMember(dest => dest.WorkAssignments, opt => opt.Ignore())
                .ForMember(dest => dest.Notifications, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedTechnician, opt => opt.Ignore());

            // User Mappings
            CreateMap<Customer, CustomerResponseDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.ToString()))
                .ForMember(dest => dest.TotalServiceRequests, opt => opt.MapFrom(src => src.ServiceRequests.Count))
                .ForMember(dest => dest.ActiveServiceRequests, opt => opt.MapFrom(src => src.ServiceRequests.Count(sr => sr.Status != ServiceStatus.Completed && sr.Status != ServiceStatus.Cancelled)));

            CreateMap<CustomerCreateDto, Customer>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => UserRole.Customer))
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.IdentityUserId, opt => opt.MapFrom(src => src.IdentityUserId))
                .ForMember(dest => dest.ServiceRequests, opt => opt.Ignore());

            CreateMap<Technician, TechnicianResponseDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.ToString()))
                .ForMember(dest => dest.SpecializationList, opt => opt.MapFrom(src => src.GetSpecializationsList()))
                .ForMember(dest => dest.ActiveAssignments, opt => opt.MapFrom(src => src.Assignments.Count(a => a.Status == WorkAssignmentStatus.Assigned || a.Status == WorkAssignmentStatus.InProgress)))
                .ForMember(dest => dest.CompletedAssignments, opt => opt.MapFrom(src => src.Assignments.Count(a => a.Status == WorkAssignmentStatus.Completed)))
                .ForMember(dest => dest.AverageRating, opt => opt.MapFrom(src => 0.0)); // TODO: Rating sistemi eklendiğinde güncellenecek

            CreateMap<TechnicianCreateDto, Technician>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => UserRole.Technician))
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.IdentityUserId, opt => opt.Ignore())
                .ForMember(dest => dest.Assignments, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedRequests, opt => opt.Ignore());

            CreateMap<ManagerCreateDto, Manager>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => UserRole.Manager))
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.IdentityUserId, opt => opt.Ignore())
                .ForMember(dest => dest.Subordinates, opt => opt.Ignore())
                .ForMember(dest => dest.Supervisor, opt => opt.Ignore());

            // WorkAssignment Mappings
            CreateMap<WorkAssignment, WorkAssignmentResponseDto>()
                .ForMember(dest => dest.ServiceRequestTitle, opt => opt.MapFrom(src => src.ServiceRequest.Title))
                .ForMember(dest => dest.ServiceRequestPriority, opt => opt.MapFrom(src => src.ServiceRequest.Priority))
                .ForMember(dest => dest.CustomerCompanyName, opt => opt.MapFrom(src => src.ServiceRequest.Customer.CompanyName))
                .ForMember(dest => dest.TechnicianName, opt => opt.MapFrom(src => $"{src.Technician.FirstName} {src.Technician.LastName}"))
                .ForMember(dest => dest.StatusName, opt => opt.MapFrom(src => src.Status.ToString()));

            CreateMap<WorkAssignmentCreateDto, WorkAssignment>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedDate, opt => opt.Ignore())
                .ForMember(dest => dest.StartedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CompletedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CompletionNotes, opt => opt.Ignore())
                .ForMember(dest => dest.ActualHours, opt => opt.Ignore())
                .ForMember(dest => dest.ServiceRequest, opt => opt.Ignore())
                .ForMember(dest => dest.Technician, opt => opt.Ignore());

            // Notification Mappings
            CreateMap<Notification, NotificationResponseDto>()
                .ForMember(dest => dest.TypeName, opt => opt.MapFrom(src => src.Type.ToString()));

            CreateMap<NotificationCreateDto, Notification>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.IsRead, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.IsEmailSent, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.IsSmsSent, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.ServiceRequest, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore());

            // AiPrediction Mappings
            CreateMap<AiPrediction, AiPredictionResponseDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.PredictedCategory.ToString()))
                .ForMember(dest => dest.PriorityName, opt => opt.MapFrom(src => src.PredictedPriority.ToString()));

            // ServiceRequestFile Mappings
            CreateMap<ServiceRequestFile, ServiceRequestFileDto>()
                .ForMember(dest => dest.FormattedFileSize, opt => opt.Ignore())
                .ForMember(dest => dest.FileExtension, opt => opt.Ignore())
                .ForMember(dest => dest.IsImage, opt => opt.Ignore())
                .ForMember(dest => dest.DownloadUrl, opt => opt.Ignore());

            // Additional mappings for complex scenarios
            CreateMap<ServiceRequest, ServiceRequest>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.Customer, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedTechnician, opt => opt.Ignore())
                .ForMember(dest => dest.Files, opt => opt.Ignore())
                .ForMember(dest => dest.AiPredictions, opt => opt.Ignore())
                .ForMember(dest => dest.WorkAssignments, opt => opt.Ignore())
                .ForMember(dest => dest.Notifications, opt => opt.Ignore());
        }
    }
}