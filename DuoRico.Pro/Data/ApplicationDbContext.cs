using DuoRico.Pro.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DuoRico.Pro.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<ApplicationUser>(options), IDataProtectionKeyContext
    {
        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<EmergencyFund> EmergencyFunds { get; set; } = null!;

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Primeiro o Identity: ele configura as tabelas AspNet*
            base.OnModelCreating(builder);

            // Todas as consultas de transações filtram pelo casal (join com o usuário) e pelo período.
            // Sem estes índices, o banco percorre as tabelas inteiras conforme os dados crescem.
            builder.Entity<ApplicationUser>()
                .HasIndex(u => u.CoupleId);

            builder.Entity<Transaction>()
                .HasIndex(t => new { t.UserId, t.Year, t.Month });

            // Uma reserva por casal. Se quem atualizou por último excluir a conta, a reserva continua (sem autor).
            builder.Entity<EmergencyFund>(fund =>
            {
                fund.HasIndex(f => f.CoupleId).IsUnique();

                fund.HasOne(f => f.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(f => f.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                fund.Property(f => f.Version).IsRowVersion();
            });
        }
    }
}
