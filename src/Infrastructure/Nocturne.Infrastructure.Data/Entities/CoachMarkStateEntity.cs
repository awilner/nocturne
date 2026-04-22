using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nocturne.Infrastructure.Data.Entities;

/// <summary>
/// PostgreSQL entity for per-user coach mark progression. Maps to the coach_mark_states table.
/// </summary>
[Table("coach_mark_states")]
public class CoachMarkStateEntity : ITenantScoped
{
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("subject_id")]
    public Guid SubjectId { get; set; }

    [Column("mark_key")]
    [MaxLength(255)]
    public string MarkKey { get; set; } = string.Empty;

    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "unseen";

    [Column("seen_at")]
    public DateTime? SeenAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }
}
