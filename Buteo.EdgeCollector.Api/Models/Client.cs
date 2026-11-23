using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Buteo.EdgeCollector.Api.Models;

[Table("Client")]
public class Client
{
    [Key]
    public int ClientID { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [Required]
    [MaxLength(50)]
    public string Region { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? PasswordHash { get; set; }

    [Required]
    [MaxLength(50)]
    public string IBAN { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; } = 0;
}
