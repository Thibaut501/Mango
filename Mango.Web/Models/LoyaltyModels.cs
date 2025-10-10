using System;
using System.Collections.Generic;

namespace Mango.Web.Models
{
    public class LoyaltyEntryDto
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = "Earned"; // Earned, Redeemed, Adjustment
        public int Points { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class LoyaltyDashboardDto
    {
        public ClientDto Client { get; set; } = new();
        public int Balance { get; set; }
        public string Tier { get; set; } = "Standard";
        public int PointsToNextTier { get; set; }
        public List<LoyaltyEntryDto> Recent { get; set; } = new();
    }
}
