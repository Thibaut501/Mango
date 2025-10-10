namespace Mango.Web.Models
{
    public class StripeRequestDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string StripeSessionUrl { get; set; } = string.Empty;
        public required string ApprovedUrl { get; set; }
        public required string CancelUrl { get; set; }
        public int OrderHeaderId { get; set; }
        public string Currency { get; set; } = "usd"; // Default to USD for compatibility
    }

}
