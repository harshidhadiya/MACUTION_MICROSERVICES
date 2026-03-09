using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace VERIFY.Model.Constrains_on_table
{
    public class VerifyProductConstraints : IEntityTypeConfiguration<VerifyProductTable>
    {
        public void Configure(EntityTypeBuilder<VerifyProductTable> builder)
        {
            builder.HasKey(data => data.Id);

            builder.Property(data => data.ProductId)
                .IsRequired();

            builder.Property(data => data.SellerId)
                .IsRequired();

            
            builder.Property(data => data.VerifiedTime)
                .HasDefaultValueSql("GETDATE()");

            builder.Property(data => data.ProductName)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasIndex(data => data.ProductId)
                .IsUnique();
            builder.Property(data => data.Description).HasMaxLength(100);
        }
    }
}