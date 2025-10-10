using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Mango.Web.Models;

namespace Mango.Web.Controllers
{
    public class ClientController : Controller
    {
        // In-memory demo store (replace with API calls later)
        private static readonly List<ClientDto> _clients = new()
        {
            new ClientDto{ Id=1, Name="Alice Martin", Email="alice@example.com", Phone="+1 555-1001", Tier="Gold", PointsBalance=1240 },
            new ClientDto{ Id=2, Name="Bob Chen", Email="bob@example.com", Phone="+1 555-1002", Tier="Silver", PointsBalance=420 },
            new ClientDto{ Id=3, Name="Carla Gomez", Email="carla@example.com", Phone="+1 555-1003", Tier="Platinum", PointsBalance=4200 }
        };

        private static readonly Dictionary<int, List<LoyaltyEntryDto>> _loyalty = new()
        {
            {1, new List<LoyaltyEntryDto>{ new(){ Date=DateTime.UtcNow.AddDays(-3), Type="Earned", Points=200, Description="Order #A100" }, new(){ Date=DateTime.UtcNow.AddDays(-1), Type="Redeemed", Points=-50, Description="Free coffee" } }},
            {2, new List<LoyaltyEntryDto>{ new(){ Date=DateTime.UtcNow.AddDays(-10), Type="Earned", Points=120, Description="Order #B200" } }},
            {3, new List<LoyaltyEntryDto>{ new(){ Date=DateTime.UtcNow.AddDays(-2), Type="Adjustment", Points=+100, Description="Promo" } }}
        };

        private static readonly Dictionary<int, List<TransactionDto>> _tx = new()
        {
            {1, new List<TransactionDto>{ new(){ Date=DateTime.UtcNow.AddDays(-4), Type="Purchase", Reference="A100", Amount=38.48m }, new(){ Date=DateTime.UtcNow.AddDays(-1), Type="Refund", Reference="A101", Amount=-8.00m } }},
            {2, new List<TransactionDto>{ new(){ Date=DateTime.UtcNow.AddDays(-12), Type="Purchase", Reference="B200", Amount=12.90m } }},
            {3, new List<TransactionDto>{ new(){ Date=DateTime.UtcNow.AddDays(-2), Type="Purchase", Reference="C300", Amount=55.10m } }}
        };

        private static readonly List<FeedbackDto> _feedback = new();

        public IActionResult Index(string? q, string? tier)
        {
            // Use LINQ to Objects to avoid expression trees and null-propagation issues
            var query = _clients.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(c => (!string.IsNullOrEmpty(c.Name) && c.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                                       || (!string.IsNullOrEmpty(c.Phone) && c.Phone.Contains(q, StringComparison.OrdinalIgnoreCase)));
            }
            if (!string.IsNullOrWhiteSpace(tier))
            {
                query = query.Where(c => string.Equals(c.Tier, tier, StringComparison.OrdinalIgnoreCase));
            }
            ViewBag.Query = q;
            ViewBag.Tier = tier;
            return View(query.ToList());
        }

        public IActionResult Create()
        {
            return View(new ClientDto());
        }

        [HttpPost]
        public IActionResult Create(ClientDto model)
        {
            if (!ModelState.IsValid) return View(model);
            model.Id = _clients.Any() ? _clients.Max(c => c.Id) + 1 : 1;
            _clients.Add(model);
            TempData["success"] = "Client created";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id)
        {
            var client = _clients.FirstOrDefault(c => c.Id == id);
            if (client == null) return NotFound();
            return View(client);
        }

        [HttpPost]
        public IActionResult Edit(ClientDto model)
        {
            if (!ModelState.IsValid) return View(model);
            var existing = _clients.FirstOrDefault(c => c.Id == model.Id);
            if (existing == null) return NotFound();
            existing.Name = model.Name;
            existing.Email = model.Email;
            existing.Phone = model.Phone;
            existing.Tier = model.Tier;
            existing.Notes = model.Notes;
            TempData["success"] = "Client updated";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Rewards(int id)
        {
            var client = _clients.FirstOrDefault(c => c.Id == id);
            if (client == null) return NotFound();
            var entries = _loyalty.TryGetValue(id, out var list) ? list : new List<LoyaltyEntryDto>();
            var vm = new LoyaltyDashboardDto
            {
                Client = client,
                Balance = client.PointsBalance,
                Tier = client.Tier,
                PointsToNextTier = Math.Max(0, 1000 - client.PointsBalance % 1000),
                Recent = entries.OrderByDescending(e => e.Date).Take(10).ToList()
            };
            return View(vm);
        }

        public IActionResult Transactions(int id, string? sortBy = "date", bool desc = true)
        {
            var client = _clients.FirstOrDefault(c => c.Id == id);
            if (client == null) return NotFound();
            var list = _tx.TryGetValue(id, out var items) ? items : new List<TransactionDto>();
            IOrderedEnumerable<TransactionDto> ordered = sortBy?.ToLowerInvariant() switch
            {
                "amount" => desc ? list.OrderByDescending(i => i.Amount) : list.OrderBy(i => i.Amount),
                "type" => desc ? list.OrderByDescending(i => i.Type) : list.OrderBy(i => i.Type),
                _ => desc ? list.OrderByDescending(i => i.Date) : list.OrderBy(i => i.Date),
            };
            var vm = new ClientTransactionsVm { Client = client, Items = ordered.ToList(), SortBy = sortBy, Desc = desc };
            return View(vm);
        }

        public IActionResult ExportCsv(int id)
        {
            var client = _clients.FirstOrDefault(c => c.Id == id);
            if (client == null) return NotFound();
            var list = _tx.TryGetValue(id, out var items) ? items : new List<TransactionDto>();
            var sb = new StringBuilder();
            sb.AppendLine("Date,Type,Reference,Amount");
            foreach (var t in list)
            {
                sb.AppendLine($"{t.Date:o},{t.Type},{t.Reference},{t.Amount}");
            }
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"client-{id}-transactions.csv");
        }

        public IActionResult Feedback(int id)
        {
            var client = _clients.FirstOrDefault(c => c.Id == id);
            if (client == null) return NotFound();
            var vm = new ClientFeedbackVm
            {
                Client = client,
                Items = _feedback.Where(f => f.ClientId == id).OrderByDescending(f => f.CreatedAt).ToList(),
                NewFeedback = new FeedbackDto { ClientId = id }
            };
            return View(vm);
        }

        [HttpPost]
        public IActionResult Feedback(ClientFeedbackVm vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Client = _clients.First(c => c.Id == vm.NewFeedback.ClientId);
                vm.Items = _feedback.Where(f => f.ClientId == vm.NewFeedback.ClientId).OrderByDescending(f => f.CreatedAt).ToList();
                return View(vm);
            }
            vm.NewFeedback.Id = _feedback.Any() ? _feedback.Max(f => f.Id) + 1 : 1;
            vm.NewFeedback.CreatedAt = DateTime.UtcNow;
            _feedback.Add(vm.NewFeedback);
            TempData["success"] = "Feedback recorded";
            return RedirectToAction(nameof(Feedback), new { id = vm.NewFeedback.ClientId });
        }

        public IActionResult Segments(string? filter, string? action)
        {
            var segments = new List<SegmentDto>
            {
                new(){ Name = "Gold Tier", Description = "All Gold clients", Filter = "Tier:Gold", Count = _clients.Count(c=>c.Tier=="Gold") },
                new(){ Name = "High Value", Description = ">= 1000 points", Filter = "Points>=1000", Count = _clients.Count(c=>c.PointsBalance>=1000) }
            };
            var vm = new SegmentsVm { Segments = segments, Filter = filter, Action = action };
            if (!string.IsNullOrWhiteSpace(action))
            {
                TempData["success"] = $"Bulk action '{action}' queued for segments.";
            }
            return View(vm);
        }
    }
}
