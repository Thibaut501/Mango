using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Mango.Web.Models
{
    public class SegmentDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Filter { get; set; } // e.g. Tier:Gold AND Frequency:High
        public int Count { get; set; }
    }

    public class SegmentsVm
    {
        public List<SegmentDto> Segments { get; set; } = new();
        public string? Filter { get; set; }
        [Display(Name="Bulk Action")]
        public string? Action { get; set; } // e.g. Send Offer
    }
}
