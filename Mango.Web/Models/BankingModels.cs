using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Mango.Web.Models
{
    public class PaymentDto
    {
        public int Id { get; set; }
        [Required]
        public string Client { get; set; } = string.Empty;
        [Required]
        public string Method { get; set; } = "Cash"; // Cash, Card, Bank, Juice, Cheque, Blink
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Recorded"; // Recorded, Verified, Failed
        public DateTime Date { get; set; } = DateTime.UtcNow;
        // Link to sales order to prevent duplicates when confirming Stripe
        public int? OrderHeaderId { get; set; }
        public string? Reference { get; set; }
    }

    public class TransferDto
    {
        public int Id { get; set; }
        [Required]
        public string From { get; set; } = string.Empty;
        [Required]
        public string To { get; set; } = string.Empty;
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }

    public class PurchaseOrderDto
    {
        public int Id { get; set; }
        [Required]
        public string Supplier { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Draft"; // Draft, Approved, Cancelled
        public decimal Total { get; set; }
    }

    public class VendorDto
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string? Contact { get; set; }
        public DateTime? LastOrderDate { get; set; }
        public decimal OutstandingBalance { get; set; }
    }

    public class ExpenseDto
    {
        public int Id { get; set; }
        [Required]
        public string Category { get; set; } = "General";
        [Required]
        public string Description { get; set; } = string.Empty;
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }

    public class ReconciliationSummary
    {
        public string Label { get; set; } = string.Empty; // e.g., 2025-10-10
        public int Transactions { get; set; }
        public decimal TotalIn { get; set; }
        public decimal TotalOut { get; set; }
        public string Status { get; set; } = "OK"; // OK, Mismatch, Pending
    }

    public class ReconciliationVm
    {
        public string Tab { get; set; } = "Daily"; // Daily, Weekly, Monthly
        public List<ReconciliationSummary> Summaries { get; set; } = new();
    }
}
