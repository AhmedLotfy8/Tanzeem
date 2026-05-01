using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Subscriptions;
using Tanzeem.Domain.Entities.Users;

namespace Tanzeem.Persistence.Data.Configurations.UsersConfigurations {
    public class UserConfiguration : IEntityTypeConfiguration<User> {
        public void Configure(EntityTypeBuilder<User> builder) {

            builder.Property(x => x.Name)
                .HasMaxLength(256);

            builder.Property(x => x.Email)
                .HasMaxLength(256);

            builder.Property(x => x.PasswordHash)
                .HasMaxLength(512);

            builder.HasIndex(u => u.StripeCustomerId)
                .IsUnique();

            builder.HasOne(x => x.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(x => x.CompanyId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.BURelations)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Subscription)
                .WithOne(s => s.User)
                .HasForeignKey<Subscription>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
