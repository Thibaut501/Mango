namespace Mango.Web.Models
{
    public class StripeRequestDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string StripeSessionUrl { get; set; } = string.Empty;
        public string ApprovedUrl { get; set; }
        public string CancelUrl { get; set; }
        public OrderHeaderDto OrderHeader { get; set; }
        public string Currency { get; set; } = "mur"; // Default to MUR
    }

}
