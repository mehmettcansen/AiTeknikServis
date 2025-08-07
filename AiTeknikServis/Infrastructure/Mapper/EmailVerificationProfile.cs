using AutoMapper;
using AiTeknikServis.Entities.Dtos.EmailVerification;
using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Infrastructure.Mapper
{
    /// <summary>
    /// Email doğrulama AutoMapper profili
    /// </summary>
    public class EmailVerificationProfile : Profile
    {
        public EmailVerificationProfile()
        {
            // EmailVerification -> EmailVerificationResponseDto
            CreateMap<EmailVerification, EmailVerificationResponseDto>()
                .ForMember(dest => dest.IsValid, opt => opt.MapFrom(src => src.IsValid))
                .ForMember(dest => dest.IsExpired, opt => opt.MapFrom(src => src.IsExpired))
                .ForMember(dest => dest.RemainingMinutes, opt => opt.Ignore()) // Service'de hesaplanacak
                .ForMember(dest => dest.TypeDisplayName, opt => opt.Ignore()); // Service'de set edilecek

            // EmailVerificationCreateDto -> EmailVerification
            CreateMap<EmailVerificationCreateDto, EmailVerification>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.VerificationCode, opt => opt.Ignore()) // Service'de generate edilecek
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.ExpiryDate, opt => opt.MapFrom(src => DateTime.UtcNow.AddMinutes(src.ExpiryMinutes)))
                .ForMember(dest => dest.IsUsed, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.UsedDate, opt => opt.Ignore())
                .ForMember(dest => dest.RetryCount, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.ToLowerInvariant()));
        }
    }
}