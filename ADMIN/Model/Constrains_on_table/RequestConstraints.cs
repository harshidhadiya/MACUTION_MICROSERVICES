using ADMIN.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace USER.Model.Constrains_on_table
{
    public class RequestConstraints : IEntityTypeConfiguration<RequestTable>
    {
        public void Configure(EntityTypeBuilder<RequestTable> builder)
        {
            builder.HasKey(data => data.Id);
            
            builder.Property(data => data.RequestUserId)
                .IsRequired();
            
            builder.Property(data => data.VerifierId)
                .IsRequired();
            
            builder.Property(data => data.VerifiedByAdmin)
                .HasDefaultValue(false)
                .IsRequired();
            
            builder.Property(data => data.RightToAdd)
                .HasDefaultValue(false)
                .IsRequired();
            
            builder.Property(data => data.CreatedAt)
                .HasDefaultValueSql("GetDate()")
                .IsRequired();
            
            builder.Property(data => data.VerifiedAt)
                .IsRequired(false);
            
            builder.Property(data => data.RightsGrantedAt)
                .IsRequired(false);

            // Indexes for better query performance
            builder.HasIndex(data => data.RequestUserId);
            builder.HasIndex(data => data.VerifierId);
            builder.HasIndex(data => data.VerifiedByAdmin);
            builder.HasIndex(data => data.RightToAdd);
            builder.Property(x=>x.Name).IsRequired();
            builder.Property(x=>x.Email).IsRequired();
        }
    }
}