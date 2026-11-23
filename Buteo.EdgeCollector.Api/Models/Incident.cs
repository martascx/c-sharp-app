using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Buteo.EdgeCollector.Api.Models;

[Table("Incident")]
public class Incident
{
    [Key]
    public int IncidentId { get; set; }

    public int SecurityEventID { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? IncidentDescription { get; set; }

    [Range(1, 4)]
    public int Severity { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "New";

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public int DetectionRuleID { get; set; }
}