using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Notifications;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Domain.Entities.Products;
using Tanzeem.Domain.Entities.Suppliers;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Entities.Users;
using Tanzeem.Services.Abstractions.Current;

namespace Tanzeem.Persistence.Data.DbContexts {
    public class TanzeemDbContext : DbContext {
        private readonly ICurrentService currentService;

        public TanzeemDbContext(DbContextOptions<TanzeemDbContext> options, ICurrentService _currentService) : base(options) {
            currentService = _currentService;
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionItem> TransactionItems { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<User> Users { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            ApplyAllGlobal(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private void ApplyAllGlobal(ModelBuilder modelBuilder) {

            // Company children
            modelBuilder.Entity<Product>().HasQueryFilter(
            p => p.CompanyId == currentService.CompanyId || currentService.CompanyId == null);

            //modelBuilder.Entity<Supplier>().HasQueryFilter(s => s.CompanyId == currentService.CompanyId);

            // Branch children
            /*
            modelBuilder.Entity<Transaction>().HasQueryFilter(t => t.BranchId == currentService.BranchId);
            modelBuilder.Entity<Inventory>().HasQueryFilter(i => i.BranchId == currentService.BranchId);
            modelBuilder.Entity<Order>().HasQueryFilter(o => o.BranchId == currentService.BranchId);
            modelBuilder.Entity<Notification>().HasQueryFilter(n => n.BranchId == currentService.BranchId);
            */
        }

    }
}
