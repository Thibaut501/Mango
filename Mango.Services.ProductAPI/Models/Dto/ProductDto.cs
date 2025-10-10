using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Mango.Services.ProductAPI.Models.Dto
{
    public class ProductDto

    {
        [Key]
        public int ProductId { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Range(1, 100)]
        public double Price { get; set; }
        public string? Description { get; set; }
        public string? CategoryName { get; set; }
        // Do not bind from client; server sets after upload
        [BindNever]
        public string? ImageUrl { get; set; }
        [BindNever]
        public string? ImageLocalPath { get; set; }
        public IFormFile? Image   { get; set;}
    }
}
