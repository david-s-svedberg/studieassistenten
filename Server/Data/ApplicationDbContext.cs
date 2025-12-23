using Microsoft.EntityFrameworkCore;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Data;

/// <summary>
/// Database context for the Studieassistenten application
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<StudyDocument> StudyDocuments => Set<StudyDocument>();
    public DbSet<GeneratedContent> GeneratedContents => Set<GeneratedContent>();
    public DbSet<Flashcard> Flashcards => Set<Flashcard>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure StudyDocument
        modelBuilder.Entity<StudyDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UploadedAt).IsRequired();
            
            // Relationships
            entity.HasMany(e => e.GeneratedContents)
                .WithOne(e => e.StudyDocument)
                .HasForeignKey(e => e.StudyDocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure GeneratedContent
        modelBuilder.Entity<GeneratedContent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.GeneratedAt).IsRequired();
        });
        
        // Configure Flashcard
        modelBuilder.Entity<Flashcard>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Question).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Answer).IsRequired().HasMaxLength(2000);
        });
    }
}
