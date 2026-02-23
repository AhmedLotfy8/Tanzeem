using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Transactions;

namespace Tanzeem.Persistence.Data.Configurations.TransactionsConfigurations {
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction> {
        
        public void Configure(EntityTypeBuilder<Transaction> builder) {

            builder.Property(x => x.Type)
                .HasMaxLength(128);

            builder.Property(x => x.CreatedAt);

            builder.Property(x => x.Status)
                .HasMaxLength(128);



            builder.HasOne(t => t.Branch)
                .WithMany(b => b.Transactions)
                .HasForeignKey(t => t.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(t => t.TransactionItems)
                .WithOne(ti => ti.Transaction)
                .HasForeignKey(ti => ti.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

        }


    }
}
