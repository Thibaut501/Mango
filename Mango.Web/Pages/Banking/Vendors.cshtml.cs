using System.Collections.Generic;
using System.Threading.Tasks;
using Mango.Web.Models;
using Mango.Web.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mango.Web.Pages.Banking
{
    [Authorize(Roles = "ADMIN")]
    public class VendorsModel : PageModel
    {
        private readonly IBankingService _svc;
        public VendorsModel(IBankingService svc) { _svc = svc; }
        public List<VendorDto> Items { get; set; } = new();
        public async Task OnGetAsync()
        {
            Items = await _svc.GetVendorsAsync();
        }
    }
}
