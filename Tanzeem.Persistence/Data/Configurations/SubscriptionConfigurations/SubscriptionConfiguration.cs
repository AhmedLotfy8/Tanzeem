
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Subscriptions;

namespace Tanzeem.Persistence.Data.Configurations.SubscriptionConfigurations {
    public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription> {
        public void Configure(EntityTypeBuilder<Subscription> builder) {

            builder.Property(s => s.StripeCustomerId)
           .IsRequired()
           .HasMaxLength(255);

            builder.Property(s => s.StripeSubscriptionId)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(s => s.Plan)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(s => s.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.HasIndex(s => s.CompanyId)
                .IsUnique();

            builder.HasIndex(s => s.StripeSubscriptionId)
                .IsUnique();

            builder.HasOne(s => s.Company)
                .WithOne(c => c.Subscription)
                .HasForeignKey<Subscription>(s => s.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
