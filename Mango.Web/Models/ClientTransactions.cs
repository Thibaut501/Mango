using System;
using System.Collections.Generic;

namespace Mango.Web.Models
{
    public class TransactionDto
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = "Purchase"; // Purchase or Refund
        public string Reference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class ClientTransactionsVm
    {
        public ClientDto Client { get; set; } = new();
        public List<TransactionDto> Items { get; set; } = new();
        public string? SortBy { get; set; }
        public bool Desc { get; set; }
    }
}
