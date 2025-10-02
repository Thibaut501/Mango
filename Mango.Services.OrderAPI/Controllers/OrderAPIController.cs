using AutoMapper;
using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;
using Mango.Services.OrderAPI.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace Mango.Services.OrderAPI.Controllers
{
    [Route("api/order")]
    [ApiController]
    public class OrderAPIController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;
        private readonly ResponseDto _response;

        public OrderAPIController(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
            _response = new ResponseDto();
        }

        [Authorize(Roles = SD.RoleAdmin)]
        [HttpGet("GetAllOrders")]
        public async Task<ResponseDto> GetAllOrders()
        {
            try
            {
                var orderHeaders = await _db.OrderHeaders
                    .Include(o => o.OrderDetails)
                    .OrderByDescending(o => o.OrderHeaderId)
                    .ToListAsync();

                _response.Result = _mapper.Map<List<OrderHeaderDto>>(orderHeaders);
                _response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [Authorize]
        [HttpGet("GetOrders/{userId}")]
        public async Task<ResponseDto> GetOrders(string userId)
        {
            try
            {
                var orderHeaders = await _db.OrderHeaders
                    .Where(u => u.UserId == userId)
                    .Include(o => o.OrderDetails)
                    .OrderByDescending(o => o.OrderHeaderId)
                    .ToListAsync();

                _response.Result = _mapper.Map<List<OrderHeaderDto>>(orderHeaders);
                _response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [Authorize]
        [HttpGet("GetOrder/{orderId}")]
        public async Task<ResponseDto> GetOrder(int orderId)
        {
            try
            {
                var orderHeader = await _db.OrderHeaders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderHeaderId == orderId);

                if (orderHeader == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Order not found";
                    return _response;
                }

                _response.Result = _mapper.Map<OrderHeaderDto>(orderHeader);
                _response.IsSuccess = true;
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
                OrderHeader orderHeader = _mapper.Map<OrderHeader>(cartDto.CartHeader);
                orderHeader.OrderTime = DateTime.Now;
                orderHeader.Status = SD.Status_Pending;
                orderHeader.OrderDetails = _mapper.Map<IEnumerable<OrderDetails>>(cartDto.CartDetails);

                await _db.OrderHeaders.AddAsync(orderHeader);
                await _db.SaveChangesAsync();

                _response.Result = _mapper.Map<OrderHeaderDto>(orderHeader);
                _response.IsSuccess = true;
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
                };

                var discountsList = new List<SessionDiscountOptions>()
                {
                    new SessionDiscountOptions
                    {
                        Coupon = stripeRequestDto.OrderHeader.CouponCode
                    }
                };

                foreach (var item in stripeRequestDto.OrderHeader.OrderDetails)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100), // Convert to cents
                            Currency = stripeRequestDto.Currency ?? "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.ProductName
                            }
                        },
                        Quantity = item.Count
                    };
                    options.LineItems.Add(sessionLineItem);
                }

                if (!string.IsNullOrEmpty(stripeRequestDto.OrderHeader.CouponCode))
                {
                    options.Discounts = discountsList;
                }

                var service = new SessionService();
                Session session = service.Create(options);
                stripeRequestDto.StripeSessionUrl = session.Url;
                stripeRequestDto.StripeSessionId = session.Id;

                OrderHeader orderHeader = await _db.OrderHeaders.FirstOrDefaultAsync(u => u.OrderHeaderId == stripeRequestDto.OrderHeader.OrderHeaderId);
                if (orderHeader != null)
                {
                    orderHeader.StripeSessionId = session.Id;
                    await _db.SaveChangesAsync();
                }

                _response.Result = stripeRequestDto;
                _response.IsSuccess = true;
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
                OrderHeader orderHeader = await _db.OrderHeaders.FirstOrDefaultAsync(u => u.OrderHeaderId == orderHeaderId);
                
                if (orderHeader == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Order not found";
                    return _response;
                }

                var service = new SessionService();
                Session session = service.Get(orderHeader.StripeSessionId);

                var paymentIntentService = new PaymentIntentService();
                PaymentIntent paymentIntent = paymentIntentService.Get(session.PaymentIntentId);

                if (paymentIntent.Status == "succeeded")
                {
                    orderHeader.PaymentIntentId = paymentIntent.Id;
                    orderHeader.Status = SD.Status_Approved;
                    await _db.SaveChangesAsync();

                    _response.Result = _mapper.Map<OrderHeaderDto>(orderHeader);
                    _response.IsSuccess = true;
                }
                else
                {
                    _response.IsSuccess = false;
                    _response.Message = "Payment not successful";
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [Authorize]
        [HttpPost("UpdateOrderStatus/{orderId}")]
        public async Task<ResponseDto> UpdateOrderStatus(int orderId, [FromBody] string newStatus)
        {
            try
            {
                OrderHeader orderHeader = await _db.OrderHeaders.FirstOrDefaultAsync(u => u.OrderHeaderId == orderId);
                if (orderHeader == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Order not found";
                    return _response;
                }

                orderHeader.Status = newStatus;
                await _db.SaveChangesAsync();

                _response.Result = _mapper.Map<OrderHeaderDto>(orderHeader);
                _response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [Authorize]
        [HttpPut("UpdateOrder")]
        public async Task<ResponseDto> UpdateOrder([FromBody] OrderHeaderDto orderHeaderDto)
        {
            try
            {
                OrderHeader orderHeader = await _db.OrderHeaders.FirstOrDefaultAsync(u => u.OrderHeaderId == orderHeaderDto.OrderHeaderId);
                if (orderHeader == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Order not found";
                    return _response;
                }

                // Update order properties
                orderHeader.Name = orderHeaderDto.Name;
                orderHeader.Phone = orderHeaderDto.Phone;
                orderHeader.Email = orderHeaderDto.Email;
                orderHeader.Status = orderHeaderDto.Status;

                await _db.SaveChangesAsync();

                _response.Result = _mapper.Map<OrderHeaderDto>(orderHeader);
                _response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }
    }
}