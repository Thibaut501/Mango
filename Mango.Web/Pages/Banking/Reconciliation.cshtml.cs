using System.Collections.Generic;
using Mango.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mango.Web.Pages.Banking
{
    [Authorize(Roles = "ADMIN")]
    public class ReconciliationModel : PageModel
    {
        public string Tab { get; set; } = "Daily";
        public List<ReconciliationSummary> Summaries { get; set; } = new();
        public void OnGet(string? tab)
        {
            Tab = string.IsNullOrWhiteSpace(tab)?"Daily":tab;
            Summaries = new List<ReconciliationSummary>
            {
                new(){ Label = System.DateTime.UtcNow.ToString("yyyy-MM-dd"), Transactions=25, TotalIn=450.00m, TotalOut=120.00m, Status="OK"},
                new(){ Label = System.DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"), Transactions=18, TotalIn=320.00m, TotalOut=80.00m, Status="Mismatch"}
            };
        }
    }
}
