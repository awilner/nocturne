using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nocturne.Infrastructure.Data.Entities;

/// <summary>
/// Stores avatar image data for a subject. Not tenant-scoped — subjects are platform-level.
/// </summary>
[Table("subject_avatars")]
public class SubjectAvatarEntity
{
    [Key]
    public Guid Id { get; set; }

    [Column("subject_id")]
    public Guid SubjectId { get; set; }

    [Column("data")]
    public byte[] Data { get; set; } = [];

    [Column("content_type")]
    [MaxLength(64)]
    public string ContentType { get; set; } = "image/webp";

    [Column("file_size")]
    public int FileSize { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public SubjectEntity Subject { get; set; } = null!;
}
