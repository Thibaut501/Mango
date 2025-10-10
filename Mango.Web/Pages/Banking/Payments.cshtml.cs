using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mango.Web.Models;
using Mango.Web.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mango.Web.Pages.Banking
{
    [Authorize(Roles = "ADMIN")]
    public class PaymentsModel : PageModel
    {
        private readonly IBankingService _svc;
        public PaymentsModel(IBankingService svc) { _svc = svc; }
        public List<PaymentDto> Items { get; set; } = new();
        public Dictionary<string, decimal> TodayTotals { get; set; } = new();
        public async Task OnGetAsync()
        {
            Items = await _svc.GetPaymentsAsync();
            var start = DateTime.UtcNow.Date; var end = start.AddDays(1).AddTicks(-1);
            var today = await _svc.GetPaymentsAsync(start, end, null);
            TodayTotals = today
                .GroupBy(p => (p.Method ?? "Other").Trim())
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
        }
    }
}
