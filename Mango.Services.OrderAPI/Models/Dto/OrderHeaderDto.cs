using Mango.Services.OrderAPI.Models.Dto;

public class OrderHeaderDto
{
    public int OrderHeaderId { get; set; }
    public string UserId { get; set; }
    public string Name { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public DateTime OrderDate { get; set; }
    public double OrderTotal { get; set; }
    public string CouponCode { get; set; }
    public double Discount { get; set; }
    public string Status { get; set; }
    public string StripeSessionId { get; set; }
    public string PaymentIntentId { get; set; }

    public ICollection<OrderDetailsDto> OrderDetails { get; set; } = new List<OrderDetailsDto>();
}
