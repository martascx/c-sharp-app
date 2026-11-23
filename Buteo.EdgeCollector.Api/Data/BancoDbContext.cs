using Microsoft.EntityFrameworkCore;

namespace Buteo.EdgeCollector.Api.Data;

public class BancoDbContext : DbContext
{
    public BancoDbContext(DbContextOptions<BancoDbContext> options) : base(options)
    {
    }

    // Representa a tabela Host da base de dados Banco
    public DbSet<Models.Host> Hosts { get; set; }

    public DbSet<Models.Client> Clients { get; set; }

    public DbSet<Models.BankTransaction> BankTransactions { get; set; }

    public DbSet<Models.SecurityEvent> SecurityEvents { get; set; }

    public DbSet<Models.Incident> Incidents { get; set; }

    public DbSet<Models.DetectionRule> DetectionRules { get; set; }
}