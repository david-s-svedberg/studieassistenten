using Microsoft.AspNetCore.Identity;

namespace StudieAssistenten.Shared.Models;

/// <summary>
/// Application user entity with Google OAuth information
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// User's full name from Google profile
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// URL to user's Google profile picture
    /// </summary>
    public string? ProfilePictureUrl { get; set; }

    /// <summary>
    /// When the user first logged in
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last login timestamp
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public ICollection<Test> Tests { get; set; } = new List<Test>();
}
