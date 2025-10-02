using AutoMapper;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;

namespace Mango.Services.OrderAPI
{
    public class MappingConfig
    {
        public static MapperConfiguration RegisterMaps()
        {
            var mappingConfig = new MapperConfiguration(config =>
            {
                // Cart ↔ OrderHeader
                config.CreateMap<OrderHeaderDto, CartHeaderDto>()
                    .ForMember(dest => dest.CartTotal, opt => opt.MapFrom(src => src.OrderTotal))
                    .ReverseMap();

                config.CreateMap<CartHeaderDto, OrderHeader>()
                    .ForMember(dest => dest.OrderTotal, opt => opt.MapFrom(src => src.CartTotal));

                config.CreateMap<CartDetailsDto, OrderDetails>()
                    .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name))
                    .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Product.Price));

                config.CreateMap<CartDetailsDto, OrderDetailsDto>()
                    .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name))
                    .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Product.Price));

                config.CreateMap<OrderDetailsDto, CartDetailsDto>();

                // ✅ OrderHeader → OrderHeaderDto with nested OrderDetails
                config.CreateMap<OrderHeader, OrderHeaderDto>()
                    .ForMember(dest => dest.OrderDetails, opt => opt.MapFrom(src => src.OrderDetails));

                config.CreateMap<OrderHeaderDto, OrderHeader>();

                // ✅ OrderDetails mappings
                config.CreateMap<OrderDetails, OrderDetailsDto>();
                config.CreateMap<OrderDetailsDto, OrderDetails>();
            });

            return mappingConfig;
        }
    }
}
