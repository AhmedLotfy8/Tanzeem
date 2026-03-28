using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Suppliers;

namespace Tanzeem.Persistence.Data.Configurations.SupplierConfigurations
{
    public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
    {
        public void Configure(EntityTypeBuilder<Supplier> builder)
        {
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.FullName).IsRequired();
            
            builder.Property(x => x.Email).IsRequired().HasMaxLength(150);
            builder.HasIndex(x => x.Email).IsUnique();

            builder.Property(x => x.PhoneNumberOne).IsRequired().HasMaxLength(20);

            builder.Property(x => x.Tax_Id).HasMaxLength(50).IsRequired(false);

            builder.Property(x => x.Notes).HasMaxLength(1000).IsRequired(false);

            builder.Property(x => x.WebsiteURL).HasMaxLength(255).IsRequired(false);

            builder.HasOne(x => x.Company).WithMany(x => x.Suppliers).HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
                
        }
    }
}
