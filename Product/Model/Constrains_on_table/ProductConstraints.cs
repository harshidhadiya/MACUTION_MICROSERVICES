using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PRODUCT.Model.Constrains_on_table
{
    public class ProductConstraints : IEntityTypeConfiguration<ProductTable>
    {
        public void Configure(EntityTypeBuilder<ProductTable> builder)
        {
            
            builder.HasKey(data=>data.Id);
            builder.Property(data=>data.product_name).IsRequired().HasMaxLength(50);
            builder.Property(data=>data.Buy_Date).IsRequired();
            builder.Property(data=>data.user_id).IsRequired();
            builder.Property(data=>data.product_description).HasMaxLength(300);
            builder.Property(data=>data.creation_date).HasDefaultValueSql("GETDATE()");
        }
    }
}