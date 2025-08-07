using AiTeknikServis.Services.Contracts;
using AiTeknikServis.Services;
using AiTeknikServis.Repositories.Contracts;
using AiTeknikServis.Repositories;
using AiTeknikServis.Infrastructure.AI;
using AiTeknikServis.Infrastructure.Notifications;

namespace AiTeknikServis.Infrastructure.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Repository Services
            services.AddScoped<IServiceRequestRepository, ServiceRequestRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IWorkAssignmentRepository, WorkAssignmentRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IServiceRequestFileRepository, ServiceRequestFileRepository>();
            services.AddScoped<IEmailVerificationRepository, EmailVerificationRepository>();

            // Business Services
            services.AddScoped<IServiceRequestService, ServiceRequestService>();
            services.AddScoped<IWorkAssignmentService, WorkAssignmentService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IEmailVerificationService, EmailVerificationService>();
            services.AddScoped<IUserValidationService, UserValidationService>();
            services.AddScoped<IReportService, ReportService>();

            // AI Services
            services.AddScoped<IAiPredictionService, GeminiAiService>();
            services.AddScoped<IAiTestService, AiTestService>();
            
            // Infrastructure Services
            services.AddScoped<IEmailService, Infrastructure.Notifications.EmailService>();
            
            // HTTP Client for AI Service
            services.AddHttpClient<GeminiAiService>(client =>
            {
                client.BaseAddress = new Uri(configuration["AiSettings:GeminiApiUrl"] ?? "");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            return services;
        }
    }
}