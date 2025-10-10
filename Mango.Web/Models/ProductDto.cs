public class ProductDto
{
    public string Name { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }

    // saved URL/path (hidden in the form)
    public string? ImageUrl { get; set; }

    // uploaded file (shown as Choose File)
    public IFormFile? Image { get; set; }
    public int Count { get; set; }
    public int ProductId { get; set; }
}
