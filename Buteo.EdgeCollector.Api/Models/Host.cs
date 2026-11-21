using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Buteo.EdgeCollector.Api.Models;

[Table("Host")]
public class Host
{
    [Key]
    public int HostID { get; set; }

    [Required]
    [MaxLength(50)]
    public string Hostname { get; set; } = string.Empty;

    [Required]
    [MaxLength(45)]
    public string IPAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(15)]
    public string OSType { get; set; } = string.Empty;

    public bool HostStatus { get; set; } = true;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(50)]
    public string Region { get; set; } = string.Empty;
}