using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VERIFY.Model.Constrains_on_table
{
    public class VerifyProductConstraints : IEntityTypeConfiguration<VerifyProductTable>
    {
        public void Configure(EntityTypeBuilder<VerifyProductTable> builder)
        {
            builder.HasKey(data=>data.Id);
            builder.Property(data=>data.userId).IsRequired().HasMaxLength(50);
            builder.Property(data=>data.verifierId).IsRequired();
            builder.Property(data=>data.verified_time).HasDefaultValueSql("GetDate()");
        }
    }
}