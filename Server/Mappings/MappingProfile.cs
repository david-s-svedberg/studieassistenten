using AutoMapper;
using StudieAssistenten.Shared.DTOs;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Mappings;

/// <summary>
/// AutoMapper profile that defines mappings between domain models and DTOs.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Document mappings
        CreateMap<StudyDocument, DocumentDto>()
            .ForMember(dest => dest.StoredFileName,
                opt => opt.MapFrom(src => Path.GetFileName(src.OriginalFilePath ?? string.Empty)));

        CreateMap<StudyDocument, DocumentSummaryDto>()
            .ForMember(dest => dest.StoredFileName,
                opt => opt.MapFrom(src => Path.GetFileName(src.OriginalFilePath ?? string.Empty)));

        CreateMap<StudyDocument, DocumentDetailDto>()
            .ForMember(dest => dest.StoredFileName,
                opt => opt.MapFrom(src => Path.GetFileName(src.OriginalFilePath ?? string.Empty)));

        // Test mappings
        CreateMap<Test, TestDto>()
            .ForMember(dest => dest.DocumentCount,
                opt => opt.MapFrom(src => src.Documents != null ? src.Documents.Count : 0))
            .ForMember(dest => dest.TotalCharacters,
                opt => opt.MapFrom(src => src.Documents != null
                    ? src.Documents.Sum(d => d.ExtractedText != null ? d.ExtractedText.Length : 0)
                    : 0))
            .ForMember(dest => dest.HasGeneratedContent,
                opt => opt.MapFrom(src => src.Documents != null &&
                    src.Documents.Any(d => d.GeneratedContents != null && d.GeneratedContents.Any())))
            .ForMember(dest => dest.Documents,
                opt => opt.MapFrom(src => src.Documents ?? new List<StudyDocument>()));

        CreateMap<Test, TestListDto>()
            .ForMember(dest => dest.DocumentCount,
                opt => opt.MapFrom(src => src.Documents != null ? src.Documents.Count : 0))
            .ForMember(dest => dest.TotalCharacters,
                opt => opt.MapFrom(src => src.Documents != null
                    ? src.Documents.Sum(d => d.ExtractedText != null ? d.ExtractedText.Length : 0)
                    : 0))
            .ForMember(dest => dest.HasGeneratedContent,
                opt => opt.MapFrom(src => src.Documents != null &&
                    src.Documents.Any(d => d.GeneratedContents != null && d.GeneratedContents.Any())))
            .ForMember(dest => dest.ShareCount,
                opt => opt.MapFrom(src => src.Shares != null
                    ? src.Shares.Count(s => s.RevokedAt == null)
                    : 0))
            .ForMember(dest => dest.Owner,
                opt => opt.MapFrom(src => src.User))
            .ForMember(dest => dest.IsOwner,
                opt => opt.Ignore()); // Set manually in controller based on current user

        CreateMap<Test, TestDetailDto>()
            .ForMember(dest => dest.DocumentCount,
                opt => opt.MapFrom(src => src.Documents != null ? src.Documents.Count : 0))
            .ForMember(dest => dest.TotalCharacters,
                opt => opt.MapFrom(src => src.Documents != null
                    ? src.Documents.Sum(d => d.ExtractedText != null ? d.ExtractedText.Length : 0)
                    : 0))
            .ForMember(dest => dest.HasGeneratedContent,
                opt => opt.MapFrom(src => src.Documents != null &&
                    src.Documents.Any(d => d.GeneratedContents != null && d.GeneratedContents.Any())))
            .ForMember(dest => dest.IsOwner,
                opt => opt.Ignore()) // Set manually in controller based on current user
            .ForMember(dest => dest.Documents,
                opt => opt.MapFrom(src => src.Documents ?? new List<StudyDocument>()));

        // Generated content mappings
        CreateMap<GeneratedContent, GeneratedContentDto>()
            .ForMember(dest => dest.Content,
                opt => opt.MapFrom(src => src.Content ?? string.Empty))
            .ForMember(dest => dest.FlashcardsCount,
                opt => opt.MapFrom(src => src.Flashcards != null ? src.Flashcards.Count : 0))
            .ForMember(dest => dest.Flashcards,
                opt => opt.MapFrom(src => src.Flashcards != null
                    ? src.Flashcards.OrderBy(f => f.Order).ToList()
                    : new List<Flashcard>()));

        // Flashcard mappings
        CreateMap<Flashcard, FlashcardDto>();

        // User mappings
        CreateMap<ApplicationUser, UserDto>()
            .ForMember(dest => dest.Email,
                opt => opt.MapFrom(src => src.Email ?? string.Empty));

        // Test share mappings
        CreateMap<TestShare, TestShareDto>()
            .ForMember(dest => dest.TestName,
                opt => opt.MapFrom(src => src.Test != null ? src.Test.Name : string.Empty))
            .ForMember(dest => dest.Owner,
                opt => opt.MapFrom(src => src.Owner))
            .ForMember(dest => dest.SharedWith,
                opt => opt.MapFrom(src => src.SharedWithUser))
            .ForMember(dest => dest.IsActive,
                opt => opt.MapFrom(src => src.RevokedAt == null));
    }
}
