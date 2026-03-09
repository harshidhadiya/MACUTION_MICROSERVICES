using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using USER.Model.Constrains_on_table;

namespace USER.Model
{
    public class MACUTIONDB:DbContext
    {
        public DbSet<UserTable> USERS { get; set; }
        public MACUTIONDB(DbContextOptions<MACUTIONDB> options) : base(options)
        {
           
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserTable>().HasData(new UserTable
            {
                Id=1,
                Name="harshid",
                HashPassword="AQAAAAIAAYagAAAAEFT9RFfK4iHUi5Ju7L5g9318ZE/4RtcErhPGnBI8QT/MM0Rtp/4ZoNLdKqcjs1yI8A==",
                Email="harshid.hadiya@gmail.com",
                Phone="1234567890",
                Address="123 Main St",
                Role="SELLER",ProfilePicture=null,
                CreatedAt=DateTime.Parse("2024-06-01T00:00:00Z")
            });
            modelBuilder.ApplyConfiguration(new UserConstraints());
        }
    }
}