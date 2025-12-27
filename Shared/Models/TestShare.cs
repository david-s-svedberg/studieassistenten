using StudieAssistenten.Shared.Enums;

namespace StudieAssistenten.Shared.Models;

/// <summary>
/// Represents a test shared between users.
/// A single test can be shared with multiple users (one-to-many relationship).
/// </summary>
public class TestShare
{
    public int Id { get; set; }

    // Foreign key to Test being shared
    public int TestId { get; set; }
    public Test? Test { get; set; }

    // Owner who is sharing the test
    public string OwnerId { get; set; } = string.Empty;
    public ApplicationUser? Owner { get; set; }

    // User who receives the share
    public string SharedWithUserId { get; set; } = string.Empty;
    public ApplicationUser? SharedWithUser { get; set; }

    // Share metadata
    public DateTime SharedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Returns true if the share is currently active (not revoked)
    /// </summary>
    public bool IsActive => RevokedAt == null;

    // Permission level (MVP uses Read only)
    public SharePermission Permission { get; set; } = SharePermission.Read;
}
