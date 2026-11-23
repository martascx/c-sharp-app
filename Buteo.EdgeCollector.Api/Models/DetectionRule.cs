using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Buteo.EdgeCollector.Api.Models;

[Table("DetectionRule")]
public class DetectionRule
{
    [Key]
    public int DetectionRuleID { get; set; }

    [Required]
    [MaxLength(100)]
    public string RuleName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Threshold { get; set; }

    public int? TimeWindowMinutes { get; set; }

    [Range(1, 4)]
    public int Severity { get; set; }

    public bool IsEnabled { get; set; } = true;
}