using Microsoft.EntityFrameworkCore;
using VERIFY.Model.Constrains_on_table;

namespace VERIFY.Model
{
    public class MACUTIONDB : DbContext
    {
        public DbSet<VerifyProductTable> VERIFY_PRODUCTS { get; set; } = null!;

        public MACUTIONDB(DbContextOptions<MACUTIONDB> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new VerifyProductConstraints());
        }
    }
}