using Mango.Services.ShoppingCartAPI.Models.Dto;
using Mango.Services.ShoppingCartAPI.Service.IService;
using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace Mango.Services.ShoppingCartAPI.Service
{
    public class CouponService : ICouponService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _accessor;


        public CouponService(IHttpClientFactory ClientFactory, IHttpContextAccessor accessor)
        {
            _httpClientFactory = ClientFactory;
            _accessor = accessor;
        }

        public async Task<CouponDto> GetCoupon(string couponCode)
        {
            var client = _httpClientFactory.CreateClient("Coupon");
            //var token = await _accessor.HttpContext.GetTokenAsync("access_token");
            var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJlbWFpbCI6ImN1c3RvbWVyQGdtYWlsLmNvbSIsInN1YiI6IjY4MjRiOWQ4LTkwNmYtNGRjNy05NTY0LTlkYTA3YTU2OGRkYSIsIm5hbWUiOiJjdXN0b21lckBnbWFpbC5jb20iLCJyb2xlIjoiQ1VTVE9NRVIiLCJuYmYiOjE3MzIwMDg3NzMsImV4cCI6MTczMjYxMzU3MywiaWF0IjoxNzMyMDA4NzczLCJpc3MiOiJtYW5nby1hdXRoLWFwaSIsImF1ZCI6Im1hbmdvLWNsaWVudCJ9.BbnxSEBfXLZY2hu2G0kLTFIooBWCKoz07A1n9NmhSZI";
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync($"/api/coupon/GetByCode/{couponCode}");
            var apiContet = await response.Content.ReadAsStringAsync();
            var resp = JsonConvert.DeserializeObject<ResponseDto>(apiContet);
            if (resp != null && resp.IsSuccess)
            {
                return JsonConvert.DeserializeObject<CouponDto>(Convert.ToString(resp.Result));
            }
            return new CouponDto();
        }
    }
}
