using System.Collections.Generic;
using System.Threading.Tasks;
using Mango.Web.Models;
using Mango.Web.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mango.Web.Pages.Banking
{
    [Authorize(Roles = "ADMIN")]
    public class TransfersModel : PageModel
    {
        private readonly IBankingService _svc;
        public TransfersModel(IBankingService svc) { _svc = svc; }
        public List<TransferDto> Items { get; set; } = new();
        public async Task OnGetAsync()
        {
            Items = await _svc.GetTransfersAsync();
        }
    }
}
