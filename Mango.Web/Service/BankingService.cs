using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mango.Web.Data;
using Mango.Web.Models;
using Mango.Web.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace Mango.Web.Service
{
    public class BankingService : IBankingService
    {
        private readonly BankingDbContext _db;
        public BankingService(BankingDbContext db)
        {
            _db = db;
        }

        public async Task AddPaymentAsync(PaymentDto model)
        {
            _db.Payments.Add(model);
            await _db.SaveChangesAsync();
        }

        public async Task<List<PaymentDto>> GetPaymentsAsync(DateTime? from = null, DateTime? to = null, string? method = null)
        {
            var q = _db.Payments.AsQueryable();
            if (from.HasValue) q = q.Where(p => p.Date >= from.Value);
            if (to.HasValue) q = q.Where(p => p.Date <= to.Value);
            if (!string.IsNullOrWhiteSpace(method)) q = q.Where(p => p.Method == method);
            return await q.OrderByDescending(p=>p.Date).ToListAsync();
        }

        public async Task RecordOrderPaymentAsync(int orderHeaderId, string client, decimal amount, string method, string? reference = null)
        {
            // Skip if we already have a payment linked to this order
            var exists = await _db.Payments.AnyAsync(p => p.OrderHeaderId == orderHeaderId);
            if (exists) return;

            var payment = new PaymentDto
            {
                Client = client,
                Method = method,
                Amount = amount,
                Status = "Recorded",
                Date = DateTime.UtcNow,
                OrderHeaderId = orderHeaderId,
                Reference = reference
            };
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();
        }

        public async Task AddTransferAsync(TransferDto model)
        {
            _db.Transfers.Add(model);
            await _db.SaveChangesAsync();
        }

        public async Task<List<TransferDto>> GetTransfersAsync()
        {
            return await _db.Transfers.OrderByDescending(t=>t.Date).ToListAsync();
        }

        public async Task AddPurchaseOrderAsync(PurchaseOrderDto model)
        {
            _db.PurchaseOrders.Add(model);
            await _db.SaveChangesAsync();
        }

        public async Task<List<PurchaseOrderDto>> GetPurchaseOrdersAsync()
        {
            return await _db.PurchaseOrders.OrderByDescending(p=>p.Date).ToListAsync();
        }

        public async Task UpdatePurchaseOrderStatusAsync(int id, string status)
        {
            var po = await _db.PurchaseOrders.FirstOrDefaultAsync(p=>p.Id==id);
            if(po!=null){ po.Status = status; await _db.SaveChangesAsync(); }
        }

        public async Task<List<VendorDto>> GetVendorsAsync()
        {
            return await _db.Vendors.OrderBy(v=>v.Name).ToListAsync();
        }

        public async Task<List<ExpenseDto>> GetExpensesAsync(string? category = null)
        {
            var query = _db.Expenses.AsQueryable();
            if(!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(e=>e.Category == category);
            }
            return await query.OrderByDescending(e=>e.Date).ToListAsync();
        }
    }
}
