using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Settings;

namespace Tanzeem.Persistence.Data.Configurations.SettingsConfiguration
{
    internal class SettingsConfiguration : IEntityTypeConfiguration<Setting>
    {
        public void Configure(EntityTypeBuilder<Setting> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(x => x.Branch).WithMany(x => x.Settings).HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
