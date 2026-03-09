using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace USER.Model.Constrains_on_table
{
    public class UserConstraints : IEntityTypeConfiguration<UserTable>
    {
        public void Configure(EntityTypeBuilder<UserTable> builder)
        {
            
            builder.HasKey(data=>data.Id);
            builder.Property(data=>data.Name).IsRequired().HasMaxLength(50);
            builder.Property(data=>data.HashPassword).IsRequired().HasMaxLength(255);
            builder.Property(data=>data.Email).IsRequired().HasMaxLength(255);
            builder.Property(data=>data.Phone).IsRequired().HasMaxLength(20);
            builder.Property(data=>data.Address).IsRequired().HasMaxLength(255);
            builder.Property(data=>data.Role).HasDefaultValue("SELLER");
            builder.HasIndex(data=>data.Email).IsUnique();
            builder.Property(data=>data.ProfilePicture).HasDefaultValue(null).HasMaxLength(255);

            
        }
    }
}