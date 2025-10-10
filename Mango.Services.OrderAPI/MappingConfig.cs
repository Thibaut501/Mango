using AutoMapper;

// Create handy aliases so we never accidentally reference DTOs/entities from another service.
using OrderModels = Mango.Services.OrderAPI.Models;
using OrderDtos   = Mango.Services.OrderAPI.Models.Dto;

namespace Mango.Services.OrderAPI
{
    // IMPORTANT: inherit from Profile so assembly scanning can find this.
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            // Core maps required by OrderAPI
            CreateMap<OrderModels.OrderHeader, OrderDtos.OrderHeaderDto>()
                .ForMember(d => d.OrderDetails, o => o.MapFrom(s => s.OrderDetails))
                .ReverseMap();

            CreateMap<OrderModels.OrderDetails, OrderDtos.OrderDetailsDto>()
                .ReverseMap();

            // If your DTOs include nested product info or different names, add ForMember() here.
            // Example:
            // .ForMember(d => d.Total, o => o.MapFrom(s => s.OrderTotal));
        }
    }
}
