using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Inventories;

namespace Tanzeem.Persistence.Data.Configurations.InventoryConfigurations {
    public class BatchConfiguration : IEntityTypeConfiguration<Batch> {
        public void Configure(EntityTypeBuilder<Batch> builder) {
            builder.Property(x => x.BatchId)
                .HasMaxLength(50);   

            builder.Property(x => x.BatchQuantity)
                .IsRequired();

            builder.Property(x => x.ExpiryDate)
                .IsRequired();

            builder.Property(x => x.RowVersion)
                .IsRowVersion();

            builder.HasOne(x => x.Inventory)
                .WithMany(i => i.Batches)
                .HasForeignKey(x => x.InventoryId)
                .OnDelete(DeleteBehavior.Cascade);  
        }

    }
}
