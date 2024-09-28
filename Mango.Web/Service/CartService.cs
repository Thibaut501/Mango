using Mango.Web.Models;
using Mango.Web.Service.IService;
using Mango.Web.Utility;

namespace Mango.Web.Service
{

    
        public class CartService : ICartService
        {
            // Dependency Injection of IBaseService to handle the actual API requests
            private readonly IBaseService _baseService;

            public CartService(IBaseService baseService)
            {
                _baseService = baseService;
            }

            // Apply Coupon to Cart
            public async Task<ResponseDto?> ApplyCouponAsync(CartDto cartDto)
            {
                return await _baseService.SendAsync(new RequestDto()
                {
                    ApiType = SD.ApiType.POST,
                    Data = cartDto,
                    Url = SD.ShoppingCartAPIBase + "/api/cart/ApplyCoupon"
                });
            }

            // Get Cart By User ID
            public async Task<ResponseDto?> GetCartByUserIdAsnyc(string userId)
            {
                return await _baseService.SendAsync(new RequestDto()
                {

                    ApiType = SD.ApiType.GET,

                    Url = SD.ShoppingCartAPIBase + "/api/cart/GetCart/" + userId
                });
            }

            // Remove Item from Cart
            public async Task<ResponseDto?> RemoveFromCartAsync(int cartDetailsId)
            {
                return await _baseService.SendAsync(new RequestDto()
                {
                    ApiType = SD.ApiType.POST,
                    Data = cartDetailsId,
                    Url = SD.ShoppingCartAPIBase + "/api/cart/RemoveCart"
                });
            }

            // Add/Update Cart Items
            public async Task<ResponseDto?> UpsertCartAsync(CartDto cartDto)
            {
                return await _baseService.SendAsync(new RequestDto()
                {
                    ApiType = SD.ApiType.POST,
                    Data = cartDto,
                    Url = SD.ShoppingCartAPIBase + "/api/cart/CartUpsert"
                });
            }
        }
    }
