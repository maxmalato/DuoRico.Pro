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

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    }
}
