using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mango.Web.Models;
using Mango.Web.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Mango.Web.Pages.Banking
{
    [Authorize(Roles = "ADMIN")]
    public class ReceiptsModel : PageModel
    {
        private readonly IBankingService _svc;
        public ReceiptsModel(IBankingService svc) { _svc = svc; }

        public List<PaymentDto> Items { get; set; } = new();
        [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }
        [BindProperty(SupportsGet = true)] public string? Method { get; set; } = "Card";

        public async Task OnGetAsync()
        {
            Items = await _svc.GetPaymentsAsync(From, To, Method);
        }

        public async Task<FileResult> OnGetExportCsvAsync(DateTime? from, DateTime? to, string? method)
        {
            var data = await _svc.GetPaymentsAsync(from, to, method);
            var sb = new StringBuilder();
            sb.AppendLine("Date,Client,Method,Reference,Amount");
            foreach (var p in data)
            {
                sb.AppendLine($"{p.Date:o},{Escape(p.Client)},{p.Method},{Escape(p.Reference)},{p.Amount}");
            }
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"receipts-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }

        public async Task<FileResult> OnGetExportPdfAsync(DateTime? from, DateTime? to, string? method)
        {
            var data = await _svc.GetPaymentsAsync(from, to, method);
            var total = data.Sum(x => x.Amount);

            QuestPDF.Settings.License = LicenseType.Community;

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Header().Text($"Receipts Report ({method ?? "All"})").SemiBold().FontSize(18);
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(150);
                            c.RelativeColumn();
                            c.ConstantColumn(80);
                            c.ConstantColumn(120);
                            c.ConstantColumn(100);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Text("Date");
                            h.Cell().Text("Client");
                            h.Cell().Text("Method");
                            h.Cell().Text("Reference");
                            h.Cell().AlignRight().Text("Amount");
                        });
                        foreach (var p in data)
                        {
                            table.Cell().Text(p.Date.ToLocalTime().ToString("g"));
                            table.Cell().Text(p.Client);
                            table.Cell().Text(p.Method);
                            table.Cell().Text(p.Reference ?? "-");
                            table.Cell().AlignRight().Text(p.Amount.ToString("C"));
                        }
                    });
                    page.Footer().AlignRight().Text($"Total: {total:C}");
                });
            });

            var pdfBytes = doc.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"receipts-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
        }

        private static string Escape(string? s) => string.IsNullOrEmpty(s) ? "" : s.Replace("\"", "\"\"");
    }
}
