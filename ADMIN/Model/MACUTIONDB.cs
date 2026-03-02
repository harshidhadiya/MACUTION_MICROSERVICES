using ADMIN.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using USER.Model.Constrains_on_table;

namespace ADMIN.Model
{
    public class MACUTIONDB:DbContext
    {
        public DbSet<RequestTable> REQUESTS { get; set; }
        public MACUTIONDB(DbContextOptions<MACUTIONDB> options) : base(options)
        {        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new RequestConstraints());
        }
    }
}