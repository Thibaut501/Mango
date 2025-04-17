﻿using Mango.Services.ShoppingCartAPI.Models.Dto;
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
            var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJlbWFpbCI6ImFkbWluQGdtYWlsLmNvbSIsInN1YiI6ImY5ZThmMjc0LTFmODktNDcyZC05OGNlLThhZmJlNDVhMWRkMSIsIm5hbWUiOiJhZG1pbkBnbWFpbC5jb20iLCJyb2xlIjoiQURNSU4iLCJuYmYiOjE3NDQ4Nzg5NzEsImV4cCI6MTc0NTQ4Mzc3MSwiaWF0IjoxNzQ0ODc4OTcxLCJpc3MiOiJtYW5nby1hdXRoLWFwaSIsImF1ZCI6Im1hbmdvLWNsaWVudCJ9.jJCi8m0erpCPPvBXwiBmfDvJhA_GtXi4fkYuQ-CFHcI";
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
