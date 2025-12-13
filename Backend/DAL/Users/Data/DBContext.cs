using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DAL.Users.Data
{
    public class DBContext(DbContextOptions<DBContext> options) : IdentityDbContext<AppUser,IdentityRole<int>,int>(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<AppUser>(entity =>
            {
                entity.ToTable(name: "Users");
            });
            builder.Entity<IdentityRole<int>>(entity =>
            {
                entity.ToTable(name: "Roles");
            });
            builder.Entity<IdentityUserRole<int>>(entity =>
            {
                entity.ToTable(name: "UserRoles");
            });
            builder.Ignore<IdentityUserClaim<int>>();
            builder.Ignore<IdentityUserLogin<int>>();
            
            builder.Ignore<IdentityRoleClaim<int>>();

            builder.Entity<EmergencyContact>()
                .HasOne(ec => ec.AppUser)
                .WithMany(au => au.EmergencyContacts)
                .HasForeignKey(ec => ec.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public DbSet<EmergencyContact> EmergencyContacts { get; set; }
    }
}
