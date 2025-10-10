using System;
using System.ComponentModel.DataAnnotations;

namespace Mango.Web.Models
{
    public class ClientDto
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Phone]
        public string? Phone { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string Tier { get; set; } = "Standard"; // Standard, Silver, Gold, Platinum

        public int PointsBalance { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? Notes { get; set; }
    }
}
