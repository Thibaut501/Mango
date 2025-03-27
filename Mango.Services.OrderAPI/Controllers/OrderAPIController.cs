using AutoMapper;
using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;
using Mango.Services.OrderAPI.Utility;
using Mango.Services.ShoppingCartAPI.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mango.Services.OrderAPI.Controllers
{
    [Route("api/order")]
    [ApiController]
    public class OrderAPIController : ControllerBase
    {
        protected ResponseDto _response;
        private readonly IMapper _mapper;
        private readonly AppDbContext _db;
        private readonly IProductService _productService;

        public OrderAPIController(AppDbContext db, IProductService productService, IMapper mapper)
        {
            _db = db;
            _productService = productService;
            _mapper = mapper;
            _response = new ResponseDto();
        }

        [Authorize]
        [HttpPost("CreateOrder")]
        public async Task<ResponseDto> CreateOrder([FromBody] CartDto cartDto)
        {
            try
            {
                OrderHeaderDto orderHeaderDto = _mapper.Map<OrderHeaderDto>(cartDto.CartHeader);
                orderHeaderDto.OrderTime = DateTime.Now;
                orderHeaderDto.Status = SD.Status_Pending;
                orderHeaderDto.OrderDetails = _mapper.Map<IEnumerable<OrderDetailsDto>>(cartDto.CartDetails);

                OrderHeader orderCreated = _db.OrderHeaders.Add(_mapper.Map<OrderHeader>(orderHeaderDto)).Entity;
                await _db.SaveChangesAsync();

                orderHeaderDto.OrderHeaderId = orderCreated.OrderHeaderId;
                _response.Result = orderHeaderDto;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [Authorize]
        [HttpPost("CreateStripeSession")]
        public async Task<ResponseDto> CreateStripeSession([FromBody] StripeRequestDto stripeRequestDto)
        {
            try
            {
                var options = new SessionCreateOptions
                {
                    SuccessUrl = stripeRequestDto.ApprovedUrl,
                    CancelUrl = stripeRequestDto.CancelUrl,
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                    PaymentMethodTypes = new List<string> { "card" } // ✅ Card only; Apple Pay / Google Pay / Cash App Pay will work automatically
                };

                List<SessionDiscountOptions> DiscountsObj = null;

                if (!string.IsNullOrEmpty(stripeRequestDto.OrderHeader.CouponCode))
                {
                    string couponCode = stripeRequestDto.OrderHeader.CouponCode;
                    long amountOff = (long)(stripeRequestDto.OrderHeader.Discount * 100);
                    string displayName = couponCode;

                    var createCouponService = new CouponService();
                    var newCouponOptions = new CouponCreateOptions
                    {
                        Currency = "usd",
                        Duration = "once",
                        AmountOff = amountOff,
                        Name = displayName,
                        Id = $"FIXED_{couponCode}_{DateTime.UtcNow.Ticks}"
                    };

                    var newCoupon = createCouponService.Create(newCouponOptions);

                    DiscountsObj = new List<SessionDiscountOptions>
                    {
                        new SessionDiscountOptions { Coupon = newCoupon.Id }
                    };
                }

                foreach (var item in stripeRequestDto.OrderHeader.OrderDetails)
                {
                    options.LineItems.Add(new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100),
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product?.Name ?? "Unnamed Product"
                            }
                        },
                        Quantity = item.Count
                    });
                }

                if (DiscountsObj != null)
                {
                    options.Discounts = DiscountsObj;
                }

                var sessionService = new SessionService();
                Session session = sessionService.Create(options);
                stripeRequestDto.StripeSessionUrl = session.Url;

                var orderHeader = _db.OrderHeaders.First(u => u.OrderHeaderId == stripeRequestDto.OrderHeader.OrderHeaderId);
                orderHeader.StripeSessionId = session.Id;
                await _db.SaveChangesAsync();

                _response.Result = stripeRequestDto;
            }
            catch (Exception ex)
            {
                _response.Message = ex.Message;
                _response.IsSuccess = false;
            }

            return _response;
        }

        [Authorize]
        [HttpPost("ValidateStripeSession")]
        public async Task<ResponseDto> ValidateStripeSession([FromBody] int orderHeaderId)
        {
            try
            {
                OrderHeader orderHeader = _db.OrderHeaders.First(u => u.OrderHeaderId == orderHeaderId);

                var sessionService = new SessionService();
                var session = sessionService.Get(orderHeader.StripeSessionId);

                var paymentIntentService = new PaymentIntentService();
                var paymentIntent = paymentIntentService.Get(session.PaymentIntentId);

                if (paymentIntent.Status == "succeeded")
                {
                    orderHeader.PaymentIntentId = paymentIntent.Id;
                    orderHeader.Status = SD.Status_Approved;
                    orderHeader.OrderTotal = session.AmountTotal.HasValue ? session.AmountTotal.Value / 100.0 : 0;

                    _db.SaveChanges();
                    _response.Result = _mapper.Map<OrderHeaderDto>(orderHeader);
                }
            }
            catch (Exception ex)
            {
                _response.Message = ex.Message;
                _response.IsSuccess = false;
            }

            return _response;
        }
    }
}
