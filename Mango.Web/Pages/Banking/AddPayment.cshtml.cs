using System.Threading.Tasks;
using Mango.Web.Models;
using Mango.Web.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mango.Web.Pages.Banking
{
    [Authorize(Roles = "ADMIN")]
    public class AddPaymentModel : PageModel
    {
        private readonly IBankingService _svc;
        public AddPaymentModel(IBankingService svc) { _svc = svc; }

        [BindProperty]
        public PaymentDto Input { get; set; } = new();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();
            await _svc.AddPaymentAsync(Input);
            TempData["success"] = "Payment added";
            return RedirectToPage("/Banking/Payments");
        }
    }
}
