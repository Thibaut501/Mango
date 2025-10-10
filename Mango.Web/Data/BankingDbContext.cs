using Mango.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Mango.Web.Data
{
    public class BankingDbContext : DbContext
    {
        public BankingDbContext(DbContextOptions<BankingDbContext> options) : base(options)
        {
        }

        public DbSet<PaymentDto> Payments => Set<PaymentDto>();
        public DbSet<TransferDto> Transfers => Set<TransferDto>();
        public DbSet<PurchaseOrderDto> PurchaseOrders => Set<PurchaseOrderDto>();
        public DbSet<VendorDto> Vendors => Set<VendorDto>();
        public DbSet<ExpenseDto> Expenses => Set<ExpenseDto>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure simple decimal precision
            modelBuilder.Entity<PaymentDto>().Property(p => p.Amount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<TransferDto>().Property(p => p.Amount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<PurchaseOrderDto>().Property(p => p.Total).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<ExpenseDto>().Property(p => p.Amount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<VendorDto>().Property(p => p.OutstandingBalance).HasColumnType("decimal(18,2)");

            // Prevent duplicate sales entries per order, but allow multiple payments without an order link
            // SQL Server filtered unique index so NULL OrderHeaderId values are not considered duplicates
            modelBuilder.Entity<PaymentDto>()
                .HasIndex(p => p.OrderHeaderId)
                .IsUnique()
                .HasFilter("[OrderHeaderId] IS NOT NULL");
        }
    }
}
