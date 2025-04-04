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
            var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJlbWFpbCI6ImNvbXBhbnlAZ21haWwuY29tIiwic3ViIjoiZGM5MjkxNzEtOTVlNy00OWYyLTkxYTgtNWE5YzEzOWMyNTFkIiwibmFtZSI6ImNvbXBhbnlAZ21haWwuY29tIiwicm9sZSI6IkFETUlOIiwibmJmIjoxNzQzNjY0MTgyLCJleHAiOjE3NDQyNjg5ODIsImlhdCI6MTc0MzY2NDE4MiwiaXNzIjoibWFuZ28tYXV0aC1hcGkiLCJhdWQiOiJtYW5nby1jbGllbnQifQ.ZGLSXqBWEJ5JS9EZpxMu42bdV4Hrj2CosDzC1NNXzLw";
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
