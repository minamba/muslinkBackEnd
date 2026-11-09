using AutoMapper;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.ViewModels;
using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Api.Mapper
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            // Référentiels
            CreateMap<JobDomain, JobDomainViewModel>();
            CreateMap<Profession, ProfessionViewModel>();
            CreateMap<JobDomainRequest, JobDomain>();
            CreateMap<ProfessionRequest, Profession>();

            // Candidats
            CreateMap<CandidateProfile, CandidateProfileViewModel>()
                .ForMember(d => d.PhotoUrl, o => o.MapFrom(s => s.PhotoPath))
                .ForMember(d => d.HasCv, o => o.MapFrom(s => s.CvPath != null))
                .ForMember(d => d.IsFavorite, o => o.Ignore())
                .ForMember(d => d.IsPremium, o => o.MapFrom(s => s.PremiumUntil > DateTime.UtcNow))
                .ForMember(d => d.HasPremiumSubscription, o => o.MapFrom(s => s.StripeSubscriptionId != null));
            CreateMap<CandidateProfile, SharedCandidateProfileViewModel>()
                .ForMember(d => d.PhotoUrl, o => o.MapFrom(s => s.PhotoPath))
                .ForMember(d => d.Email, o => o.MapFrom(s => s.ShareContact == true ? s.Email : null))
                .ForMember(d => d.Phone, o => o.MapFrom(s => s.ShareContact == true ? s.Phone : null));
            CreateMap<CandidateExperience, ExperienceViewModel>();
            CreateMap<CandidateEducation, EducationViewModel>();
            CreateMap<CandidateLanguage, LanguageViewModel>();
            CreateMap<CandidateSummary, CandidateSummaryViewModel>()
                .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}".Trim()))
                .ForMember(d => d.PhotoUrl, o => o.MapFrom(s => s.PhotoPath))
                .ForMember(d => d.IsPremium, o => o.Ignore());  // renseigné selon le mode premium

            // Mise à jour d'un profil existant : seuls les champs du formulaire sont écrasés
            CreateMap<CandidateProfileRequest, CandidateProfile>()
                .ForMember(d => d.IsCvHidden, o => o.Ignore());  // réglé depuis l'encadré CV (me/cv/visibility)
            CreateMap<AdminCandidateProfileRequest, CandidateProfile>().IncludeBase<CandidateProfileRequest, CandidateProfile>();
            CreateMap<ExperienceRequest, CandidateExperience>();
            CreateMap<EducationRequest, CandidateEducation>();
            CreateMap<LanguageRequest, CandidateLanguage>();

            // Recruteurs
            CreateMap<RecruiterProfile, RecruiterProfileViewModel>()
                .ForMember(d => d.LogoUrl, o => o.MapFrom(s => s.LogoPath))
                .ForMember(d => d.IsPro, o => o.MapFrom(s => s.ProUntil > DateTime.UtcNow))
                .ForMember(d => d.HasProSubscription, o => o.MapFrom(s => s.StripeSubscriptionId != null))
                .ForMember(d => d.IdentityVerified, o => o.MapFrom(s => s.IdentityVerifiedAt != null));
            // SIRET et RNA : vérifiés et enregistrés à part (FounderViewModelBuilder)
            CreateMap<RecruiterProfileRequest, RecruiterProfile>()
                .ForMember(d => d.Siret, o => o.Ignore())
                .ForMember(d => d.Rna, o => o.Ignore());
            CreateMap<AdminRecruiterProfileRequest, RecruiterProfile>().IncludeBase<RecruiterProfileRequest, RecruiterProfile>();
            CreateMap<Company, CompanyViewModel>()
                .ForMember(d => d.LogoUrl, o => o.MapFrom(s => s.LogoPath));
            CreateMap<Company, CompanyAdminViewModel>()
                .ForMember(d => d.LogoUrl, o => o.MapFrom(s => s.LogoPath));
            CreateMap<CompanySectorCount, CompanySectorViewModel>();
            CreateMap<Company, CompanyDetailViewModel>()
                .ForMember(d => d.LogoUrl, o => o.MapFrom(s => s.LogoPath))
                .ForMember(d => d.Offers, o => o.Ignore())
                .ForMember(d => d.IsOwner, o => o.Ignore());

            // Offres
            CreateMap<JobOffer, JobOfferViewModel>()
                .ForMember(d => d.CompanyLogoUrl, o => o.MapFrom(s => s.CompanyLogoPath))
                .ForMember(d => d.IsOwner, o => o.Ignore())
                .ForMember(d => d.HasApplied, o => o.Ignore());
            CreateMap<JobOfferRequest, JobOffer>();
            CreateMap<FilledOffer, FilledOfferViewModel>()
                .ForMember(d => d.CompanyLogoUrl, o => o.MapFrom(s => s.CompanyLogoPath));
            CreateMap<FilledOffersStats, FilledOffersViewModel>();
            CreateMap(typeof(PagedResult<>), typeof(PagedViewModel<>));

            // Candidatures, favoris, messages
            CreateMap<JobApplication, JobApplicationViewModel>()
                .ForMember(d => d.CompanyLogoUrl, o => o.MapFrom(s => s.CompanyLogoPath))
                .ForMember(d => d.HasCv, o => o.MapFrom(s => s.CvPath != null));
            CreateMap<Favorite, FavoriteViewModel>();
            CreateMap<Message, MessageViewModel>()
                .ForMember(d => d.IsMine, o => o.Ignore());

            // Actualités
            CreateMap<Article, ArticleViewModel>()
                .ForMember(d => d.CoverImageUrl, o => o.MapFrom(s => s.CoverImagePath))
                .ForMember(d => d.ThumbnailUrl, o => o.MapFrom(s => s.ThumbnailPath));
        }
    }
}
