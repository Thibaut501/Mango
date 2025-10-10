using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Mango.Web.Models
{
    public class FeedbackDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Required]
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = "Open"; // Open, InProgress, Resolved
        public string CreatedBy { get; set; } = "Agent";
    }

    public class ClientFeedbackVm
    {
        public ClientDto Client { get; set; } = new();
        public List<FeedbackDto> Items { get; set; } = new();
        public FeedbackDto NewFeedback { get; set; } = new();
    }
}
