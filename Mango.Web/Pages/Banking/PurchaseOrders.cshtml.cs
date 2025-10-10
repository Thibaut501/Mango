using System.Collections.Generic;
using System.Threading.Tasks;
using Mango.Web.Models;
using Mango.Web.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mango.Web.Pages.Banking
{
    [Authorize(Roles = "ADMIN")]
    public class PurchaseOrdersModel : PageModel
    {
        private readonly IBankingService _svc;
        public PurchaseOrdersModel(IBankingService svc) { _svc = svc; }
        public List<PurchaseOrderDto> Items { get; set; } = new();
        public async Task OnGetAsync()
        {
            Items = await _svc.GetPurchaseOrdersAsync();
        }
        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            await _svc.UpdatePurchaseOrderStatusAsync(id, "Approved");
            TempData["success"] = "Purchase order approved";
            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostCancelAsync(int id)
        {
            await _svc.UpdatePurchaseOrderStatusAsync(id, "Cancelled");
            TempData["success"] = "Purchase order cancelled";
            return RedirectToPage();
        }
    }
}
