using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using PRODUCT.Model;
using PRODUCT.Model.Constrains_on_table;

namespace PRODUCT.Model
{
    public class MACUTIONDB:DbContext
    {
        public DbSet<ProductTable> PRODUCTS { get; set; }
            public DbSet<ImageTable> IMAGES { get; set; }

        public MACUTIONDB(DbContextOptions<MACUTIONDB> options) : base(options)
        {
           
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
           
            modelBuilder.ApplyConfiguration(new ProductConstraints());
            modelBuilder.Entity<ImageTable>().HasOne(p => p.product).WithMany(i => i.images).HasForeignKey(p => p.ProductId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ImageTable>().HasKey(i => i.Id);
            modelBuilder.Entity<ImageTable>().Property(i => i.Image_URL).IsRequired();
             
             modelBuilder.Entity<ImageTable>().HasIndex(i => i.Image_URL).IsUnique();
             modelBuilder.Entity<ImageTable>().Property(i => i.Image_URL).HasMaxLength(200);
             modelBuilder.Entity<ImageTable>().Property(i => i.Image_URL).HasColumnType("varchar(200)");

        }
    }
}