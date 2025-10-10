using System.Threading.Tasks;
using Mango.Web.Models;
using Mango.Web.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mango.Web.Pages.Banking
{
    [Authorize(Roles = "ADMIN")]
    public class NewPurchaseOrderModel : PageModel
    {
        private readonly IBankingService _svc;
        public NewPurchaseOrderModel(IBankingService svc) { _svc = svc; }

        [BindProperty]
        public PurchaseOrderDto Input { get; set; } = new();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();
            await _svc.AddPurchaseOrderAsync(Input);
            TempData["success"] = "Purchase order created";
            return RedirectToPage("/Banking/PurchaseOrders");
        }
    }
}
