using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Buteo.EdgeCollector.Api.Models;

[Table("SecurityEvent")]
public class SecurityEvent
{
    [Key]
    public int SecurityEventID { get; set; }

    public int HostID { get; set; }

    public int? TransactionID { get; set; }

    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    public string RawPayload { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(45)]
    public string IPAddress { get; set; } = string.Empty;
}
