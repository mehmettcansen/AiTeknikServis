using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet'ler
        public DbSet<ServiceRequest> ServiceRequests { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Technician> Technicians { get; set; }
        public DbSet<Manager> Managers { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<WorkAssignment> WorkAssignments { get; set; }
        public DbSet<AiPrediction> AiPredictions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Equipment> Equipment { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<ServiceRequestFile> ServiceRequestFiles { get; set; }
        public DbSet<EmailVerification> EmailVerifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Model konfigürasyonları
            ConfigureServiceRequest(modelBuilder);
            ConfigureUserHierarchy(modelBuilder);
            ConfigureWorkAssignment(modelBuilder);
            ConfigureAiPrediction(modelBuilder);
            ConfigureNotification(modelBuilder);
            ConfigureServiceRequestFile(modelBuilder);
            ConfigureEquipment(modelBuilder);
            ConfigureReport(modelBuilder);
            ConfigureEmailVerification(modelBuilder);
        }

        private void ConfigureServiceRequest(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ServiceRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
                
                entity.HasOne(e => e.Customer)
                      .WithMany(c => c.ServiceRequests)
                      .HasForeignKey(e => e.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.AssignedTechnician)
                      .WithMany(t => t.AssignedRequests)
                      .HasForeignKey(e => e.AssignedTechnicianId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ConfigureUserHierarchy(ModelBuilder modelBuilder)
        {
            // User base class konfigürasyonu
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                
                // Table Per Hierarchy (TPH) strategy
                entity.HasDiscriminator<string>("UserType")
                    .HasValue<Customer>("Customer")
                    .HasValue<Technician>("Technician")
                    .HasValue<Manager>("Manager")
                    .HasValue<Admin>("Admin");
            });

            // Derived class konfigürasyonları (key tanımlamadan)
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.Property(e => e.CompanyName).HasMaxLength(200);
            });

            modelBuilder.Entity<Technician>(entity =>
            {
                entity.Property(e => e.Specializations).HasMaxLength(500);
            });

            // Manager entity configuration - no additional properties to configure

            modelBuilder.Entity<Admin>(entity =>
            {
                entity.Property(e => e.AccessLevel).HasMaxLength(50);
            });
        }

        private void ConfigureWorkAssignment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WorkAssignment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AssignedDate).HasDefaultValueSql("GETDATE()");
                
                entity.HasOne(e => e.ServiceRequest)
                      .WithMany(sr => sr.WorkAssignments)
                      .HasForeignKey(e => e.ServiceRequestId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Technician)
                      .WithMany(t => t.Assignments)
                      .HasForeignKey(e => e.TechnicianId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureAiPrediction(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AiPrediction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.Recommendation).HasMaxLength(1000);
                
                entity.HasOne(e => e.ServiceRequest)
                      .WithMany(sr => sr.AiPredictions)
                      .HasForeignKey(e => e.ServiceRequestId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureNotification(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.IsRead).HasDefaultValue(false);
                
                entity.HasOne(e => e.ServiceRequest)
                      .WithMany(sr => sr.Notifications)
                      .HasForeignKey(e => e.ServiceRequestId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ConfigureServiceRequestFile(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ServiceRequestFile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UploadedDate).HasDefaultValueSql("GETDATE()");
                
                entity.HasOne(e => e.ServiceRequest)
                      .WithMany(sr => sr.Files)
                      .HasForeignKey(e => e.ServiceRequestId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureEquipment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Equipment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).HasDefaultValue("Active");
                
                entity.HasOne(e => e.Customer)
                      .WithMany()
                      .HasForeignKey(e => e.CustomerId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ConfigureReport(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.GeneratedDate).HasDefaultValueSql("GETDATE()");
                
                entity.HasOne(e => e.GeneratedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.GeneratedByUserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ConfigureEmailVerification(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EmailVerification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.VerificationCode).IsRequired().HasMaxLength(6);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.IsUsed).HasDefaultValue(false);
                entity.Property(e => e.RetryCount).HasDefaultValue(0);
                entity.Property(e => e.MaxRetries).HasDefaultValue(3);
                entity.Property(e => e.Purpose).HasMaxLength(200);
                entity.Property(e => e.AdditionalData).HasMaxLength(500);
                
                // Index'ler
                entity.HasIndex(e => new { e.Email, e.Type, e.IsUsed, e.ExpiryDate })
                      .HasDatabaseName("IX_EmailVerification_Active");
                entity.HasIndex(e => e.CreatedDate)
                      .HasDatabaseName("IX_EmailVerification_CreatedDate");
                entity.HasIndex(e => e.ExpiryDate)
                      .HasDatabaseName("IX_EmailVerification_ExpiryDate");
            });
        }
    }
}