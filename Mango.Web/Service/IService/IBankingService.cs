using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mango.Web.Models;

namespace Mango.Web.Service.IService
{
    public interface IBankingService
    {
        Task<List<PaymentDto>> GetPaymentsAsync(DateTime? from = null, DateTime? to = null, string? method = null);
        Task AddPaymentAsync(PaymentDto model);
        Task RecordOrderPaymentAsync(int orderHeaderId, string client, decimal amount, string method, string? reference = null);

        Task<List<TransferDto>> GetTransfersAsync();
        Task AddTransferAsync(TransferDto model);

        Task<List<PurchaseOrderDto>> GetPurchaseOrdersAsync();
        Task AddPurchaseOrderAsync(PurchaseOrderDto model);
        Task UpdatePurchaseOrderStatusAsync(int id, string status);

        Task<List<VendorDto>> GetVendorsAsync();

        Task<List<ExpenseDto>> GetExpensesAsync(string? category = null);
    }
}
