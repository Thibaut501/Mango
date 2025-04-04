using AutoMapper;
using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;
using Mango.Services.OrderAPI.Utility;
using Mango.Services.ShoppingCartAPI.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        [HttpGet("GetOrders")]
        public ResponseDto? Get(string? userId = "")
        {
            try
            {
                IEnumerable<OrderHeader> objList;
                if (User.IsInRole(SD.RoleAdmin))
                {
                    objList = _db.OrderHeaders.Include(u => u.OrderDetails).OrderByDescending(u => u.OrderHeaderId).ToList();
                }
                else
                {
                    objList = _db.OrderHeaders.Include(u => u.OrderDetails)
                                              .Where(u => u.UserId == userId)
                                              .OrderByDescending(u => u.OrderHeaderId).ToList();
                }

                var orderList = objList.Select(order => new
                {
                    order.OrderHeaderId,
                    order.Email,
                    order.Name,
                    order.Phone,
                    order.Status,
                    order.OrderTotal
                });

                _response.Result = orderList;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [Authorize]
        [HttpGet("GetOrder/{id:int}")]
        public ResponseDto? Get(int id)
        {
            try
            {
                OrderHeader orderHeader = _db.OrderHeaders.Include(u => u.OrderDetails).First(u => u.OrderHeaderId == id);
                _response.Result = _mapper.Map<OrderHeaderDto>(orderHeader);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
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
                StripeConfiguration.ApiKey = "sk_test_51R79GyGrjZGIXFR0drxxOJNm4MDkxum09vErN2ZYm1yyxEzm6HQACTYfkXgau6Mea2uJNwPT67yJ01rb2XHQeoOE00TwwBDadn"; // Replace with actual key

                string currency = stripeRequestDto.Currency?.ToLower() ?? "usd";

                var paymentMethods = new List<string> { "card" }; // Enables Card, Apple Pay, Google Pay

                if (currency == "usd")
                {
                    paymentMethods.Add("cashapp");
                }

                var options = new SessionCreateOptions
                {
                    SuccessUrl = stripeRequestDto.ApprovedUrl,
                    CancelUrl = stripeRequestDto.CancelUrl,
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                    Locale = "auto",
                    PaymentMethodTypes = paymentMethods,
                    PaymentIntentData = new SessionPaymentIntentDataOptions
                    {
                        CaptureMethod = "automatic"
                    }
                };

                if (!string.IsNullOrEmpty(stripeRequestDto.OrderHeader.CouponCode))
                {
                    long amountOff = (long)(stripeRequestDto.OrderHeader.Discount * 100);
                    var coupon = new CouponService().Create(new CouponCreateOptions
                    {
                        Currency = currency,
                        Duration = "once",
                        AmountOff = amountOff,
                        Name = stripeRequestDto.OrderHeader.CouponCode,
                        Id = $"FIXED_{stripeRequestDto.OrderHeader.CouponCode}_{DateTime.UtcNow.Ticks}"
                    });

                    options.Discounts = new List<SessionDiscountOptions>
                    {
                        new SessionDiscountOptions { Coupon = coupon.Id }
                    };
                }

                foreach (var item in stripeRequestDto.OrderHeader.OrderDetails)
                {
                    options.LineItems.Add(new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100),
                            Currency = currency,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product?.Name ?? "Unnamed Product"
                            }
                        },
                        Quantity = item.Count
                    });
                }

                var session = new SessionService().Create(options);
                stripeRequestDto.StripeSessionUrl = session.Url;

                var orderHeader = _db.OrderHeaders.First(u => u.OrderHeaderId == stripeRequestDto.OrderHeader.OrderHeaderId);
                orderHeader.StripeSessionId = session.Id;
                await _db.SaveChangesAsync();

                _response.Result = stripeRequestDto;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
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
                var session = new SessionService().Get(orderHeader.StripeSessionId);
                var paymentIntent = new PaymentIntentService().Get(session.PaymentIntentId);

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

        [Authorize]
        [HttpPost("UpdateOrderStatus/{orderId:int}")]
        public async Task<ResponseDto> UpdateOrderStatus(int orderId, [FromBody] string newStatus)
        {
            try
            {
                OrderHeader orderHeader = _db.OrderHeaders.First(u => u.OrderHeaderId == orderId);
                if (orderHeader != null)
                {
                    if (newStatus == SD.Status_Cancelled)
                    {
                        new RefundService().Create(new RefundCreateOptions
                        {
                            Reason = RefundReasons.RequestedByCustomer,
                            PaymentIntent = orderHeader.PaymentIntentId
                        });
                    }
                    orderHeader.Status = newStatus;
                    _db.SaveChanges();
                }
            }
            catch
            {
                _response.IsSuccess = false;
            }
            return _response;
        }
    }
}
