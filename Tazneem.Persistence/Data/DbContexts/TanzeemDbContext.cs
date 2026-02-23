using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Entities.Products;
using Tanzeem.Domain.Entities.Transactions;

namespace Tanzeem.Persistence.Data.DbContexts {
    public class TanzeemDbContext : DbContext {

        public TanzeemDbContext(DbContextOptions<TanzeemDbContext> options) : base(options) {
            
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionItem> TransactionItems { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            base.OnModelCreating(modelBuilder);
        }


    
    }
}
