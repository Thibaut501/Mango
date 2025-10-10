using AutoMapper;
using Mango.Services.CouponAPI.Models;
using Mango.Services.CouponAPI.Models.Dto;

namespace Mango.Services.CouponAPI
{
    // Use Profile so AutoMapper can discover maps via AddAutoMapper(typeof(MappingConfig))
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            CreateMap<CouponDto, Coupon>().ReverseMap();
        }
    }
}
