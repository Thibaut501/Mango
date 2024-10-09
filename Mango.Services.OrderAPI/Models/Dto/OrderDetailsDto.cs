﻿using Mango.Services.OrderAPI.Dto;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mango.Services.OrderAPI.Models.Dto
{
    public class OrderDetailsDto
    {
        public int OrderHeaderId { get; set; }  
        public OrderHeader? OrderHeaderId { get; set; }
        public int ProductId { get; set; }
        public ProductDto? Product { get; set; }
        public int Count { get; set; }
        public string ProductName { get; set; }
        public string Price { get; set; }
    }
}
