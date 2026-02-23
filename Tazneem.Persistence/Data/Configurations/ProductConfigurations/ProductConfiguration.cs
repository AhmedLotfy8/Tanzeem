using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Products;

namespace Tanzeem.Persistence.Data.Configurations.ProductConfigurations {
    public class ProductConfiguration : IEntityTypeConfiguration<Product> {
        public void Configure(EntityTypeBuilder<Product> builder) {
        
            
            builder.Property(x => x.Name)
                .HasMaxLength(256);

            builder.Property(x => x.Price)
                .HasColumnType("decimal(18,2)");



            builder.HasOne(p => p.Company)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.TransactionItems)
                .WithOne(ti => ti.Product)
                .HasForeignKey(ti => ti.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

        }

    }
}
