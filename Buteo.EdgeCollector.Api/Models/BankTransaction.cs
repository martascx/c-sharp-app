using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Buteo.EdgeCollector.Api.Models;

[Table("BankTransaction")]
public class BankTransaction
{
    [Key]
    public int TransactionID { get; set; }

    // References Client.IBAN
    [Required]
    [MaxLength(50)]
    public string SenderIBAN { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ReceiverIBAN { get; set; } = string.Empty;

    [Required]
    [MaxLength(45)]
    public string IPAddress { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
