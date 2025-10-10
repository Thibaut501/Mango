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
    public class ExpensesModel : PageModel
    {
        private readonly IBankingService _svc;
        public ExpensesModel(IBankingService svc) { _svc = svc; }
        public List<ExpenseDto> Items { get; set; } = new();
        public string? SelectedCategory { get; set; }
        public List<string> DistinctCategories { get; set; } = new();
        public async Task OnGetAsync(string? category)
        {
            SelectedCategory = category;
            Items = await _svc.GetExpensesAsync(category);
            DistinctCategories = Items.Select(i=>i.Category).Distinct().OrderBy(c=>c).ToList();
        }
    }
}
