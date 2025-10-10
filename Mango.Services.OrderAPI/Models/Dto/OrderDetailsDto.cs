namespace Mango.Services.OrderAPI.Models.Dto
{
    public class OrderDetailsDto
    {
        public int OrderDetailsId { get; set; }
        public int OrderHeaderId { get; set; }
        public int ProductId { get; set; }
        public int Count { get; set; }

        public string? ProductName { get; set; }
        public string Price { get; set; }
        public object Product { get; internal set; }
    }
}

