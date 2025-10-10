using System.Security.Claims;
using AutoMapper;
using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;
using Mango.Services.OrderAPI.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper.QueryableExtensions;

using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Stripe.Checkout;

// Force mapping to the correct DTO namespace
using DtoOrderHeader = Mango.Services.OrderAPI.Models.Dto.OrderHeaderDto;

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

        // ADMIN: list all orders
        
[HttpGet("GetAllOrders")]
    public async Task<ResponseDto> GetAllOrders()
    {
        var resp = new ResponseDto();
        try
        {
            var dto = await _db.OrderHeaders
                .AsNoTracking()
                .ProjectTo<OrderHeaderDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            resp.Result = dto;
            resp.IsSuccess = true;
        }
        catch (Exception ex)
        {
            resp.IsSuccess = false;
            resp.Message = ex.GetBaseException().Message;
        }
        return resp;
    }
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      
    // USER: list my orders (claims-based)
    [Authorize]
        [HttpGet("GetOrders")]
        public async Task<ResponseDto> GetOrders()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    _response.IsSuccess = false;
                    _response.Message = "User not identified.";
                    return _response;
                }

                var orderHeaders = await _db.OrderHeaders
                    .AsNoTracking()
                    .Where(u => u.UserId == userId)
                    .Include(o => o.OrderDetails)
                    .OrderByDescending(o => o.OrderHeaderId)
                    .ToListAsync();

                _response.Result = _mapper.Map<List<DtoOrderHeader>>(orderHeaders);
                _response.IsSuccess = true;
            }
            catch (AutoMapperMappingException amex)
            {
                _response.IsSuccess = false;
                _response.Message = amex.InnerException?.Message ?? amex.Message;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        // USER/ADMIN: get a single order (non-admin must own it)
        [Authorize]
        [HttpGet("GetOrder/{orderId:int}")]
        public async Task<ResponseDto> GetOrder(int orderId)
        {
            try
            {
                var orderHeader = await _db.OrderHeaders
                    .AsNoTracking()
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderHeaderId == orderId);

                if (orderHeader == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Order not found.";
                    return _response;
                }

                var isAdmin = User.IsInRole(SD.RoleAdmin);
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!isAdmin && !string.Equals(orderHeader.UserId, userId, StringComparison.Ordinal))
                {
                    _response.IsSuccess = false;
                    _response.Message = "Forbidden.";
                    return _response;
                }

                _response.Result = _mapper.Map<DtoOrderHeader>(orderHeader);
                _response.IsSuccess = true;
            }
            catch (AutoMapperMappingException amex)
            {
                _response.IsSuccess = false;
                _response.Message = amex.InnerException?.Message ?? amex.Message;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        // Create order from cart
        [HttpPost("CreateOrder")]
        public async Task<ResponseDto> CreateOrder([FromBody] Models.Dto.CartDto cartDto)
        {
            var resp = new ResponseDto();
            try
            {
                if (cartDto?.CartHeader == null)
                {
                    resp.IsSuccess = false;
                    resp.Message = "Invalid cart.";
                    return resp;
                }

                var orderHeader = new OrderHeader
                {
                    UserId = cartDto.CartHeader.UserId,
                    CouponCode = cartDto.CartHeader.CouponCode,
                    Discount = cartDto.CartHeader.Discount,
                    OrderTotal = cartDto.CartHeader.CartTotal,
                    Name = cartDto.CartHeader.Name,
                    Phone = cartDto.CartHeader.Phone,
                    Email = cartDto.CartHeader.Email,
                    OrderTime = DateTime.UtcNow,
                    Status = SD.Status_Pending
                };

                _db.OrderHeaders.Add(orderHeader);
                await _db.SaveChangesAsync();

                if (cartDto.CartDetails != null)
                {
                    var details = new List<OrderDetails>();
                    foreach (var d in cartDto.CartDetails)
                    {
                        var price = d.Product?.Price ?? 0;
                        details.Add(new OrderDetails
                        {
                            OrderHeaderId = orderHeader.OrderHeaderId,
                            ProductId = d.ProductId,
                            Count = d.Count,
                            ProductName = d.Product?.Name ?? string.Empty,
                            Price = price.ToString(CultureInfo.InvariantCulture)
                        });
                    }
                    if (details.Count > 0)
                    {
                        _db.OrderDetails.AddRange(details);
                        await _db.SaveChangesAsync();
                    }
                }

                var dto = _mapper.Map<OrderHeaderDto>(orderHeader);
                resp.Result = dto;
                resp.IsSuccess = true;
            }
            catch (Exception ex)
            {
                resp.IsSuccess = false;
                resp.Message = ex.GetBaseException().Message;
            }
            return resp;
        }

        // Create Stripe Checkout Session
        [HttpPost("CreateStripeSession")]
        public async Task<ResponseDto> CreateStripeSession([FromBody] Models.Dto.StripeRequestDto request)
        {
            var resp = new ResponseDto();
            try
            {
                if (request == null)
                {
                    resp.IsSuccess = false;
                    resp.Message = "Request body was null.";
                    return resp;
                }
                if (string.IsNullOrWhiteSpace(request.ApprovedUrl) || string.IsNullOrWhiteSpace(request.CancelUrl))
                {
                    resp.IsSuccess = false;
                    resp.Message = "ApprovedUrl/CancelUrl are required.";
                    return resp;
                }
                if (request.OrderHeaderId <= 0)
                {
                    resp.IsSuccess = false;
                    resp.Message = "Invalid OrderHeaderId.";
                    return resp;
                }

                var orderHeader = await _db.OrderHeaders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderHeaderId == request.OrderHeaderId);

                if (orderHeader == null)
                {
                    resp.IsSuccess = false;
                    resp.Message = "Order not found.";
                    return resp;
                }

                var currency = string.IsNullOrWhiteSpace(request.Currency) ? "usd" : request.Currency.ToLowerInvariant();

                var options = new SessionCreateOptions
                {
                    SuccessUrl = request.ApprovedUrl,
                    CancelUrl = request.CancelUrl,
                    Mode = "payment",
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>()
                };

                foreach (var item in orderHeader.OrderDetails)
                {
                    if (!decimal.TryParse(item.Price, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                    {
                        var normalized = item.Price?.Replace(',', '.') ?? "0";
                        if (!decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out price))
                        {
                            resp.IsSuccess = false;
                            resp.Message = $"Invalid price for product '{item.ProductName}'.";
                            return resp;
                        }
                    }

                    var unitAmount = (long)Math.Round(price * 100m, MidpointRounding.AwayFromZero);
                    if (unitAmount <= 0)
                    {
                        resp.IsSuccess = false;
                        resp.Message = $"Invalid price for product '{item.ProductName}'.";
                        return resp;
                    }

                    options.LineItems.Add(new SessionLineItemOptions
                    {
                        Quantity = item.Count,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = currency,
                            UnitAmount = unitAmount,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = string.IsNullOrWhiteSpace(item.ProductName) ? $"Product {item.ProductId}" : item.ProductName
                            }
                        }
                    });
                }

                var service = new SessionService();
                Session session;
                try
                {
                    session = await service.CreateAsync(options);
                }
                catch (Stripe.StripeException sex)
                {
                    resp.IsSuccess = false;
                    resp.Message = $"Stripe error ({sex.HttpStatusCode}): {sex.StripeError?.Message ?? sex.Message}";
                    return resp;
                }

                orderHeader.StripeSessionId = session.Id;
                await _db.SaveChangesAsync();

                resp.Result = new Models.Dto.StripeRequestDto
                {
                    StripeSessionId = session.Id,
                    StripeSessionUrl = session.Url,
                    ApprovedUrl = request.ApprovedUrl,
                    CancelUrl = request.CancelUrl,
                    OrderHeaderId = orderHeader.OrderHeaderId,
                    Currency = currency
                };
                resp.IsSuccess = true;
            }
            catch (Exception ex)
            {
                resp.IsSuccess = false;
                resp.Message = ex.GetBaseException().Message;
            }
            return resp;
        }

        // Validate Stripe session on return
        [HttpPost("ValidateStripeSession")]
        public async Task<ResponseDto> ValidateStripeSession([FromBody] int orderHeaderId)
        {
            var resp = new ResponseDto();
            try
            {
                var orderHeader = await _db.OrderHeaders.FirstOrDefaultAsync(o => o.OrderHeaderId == orderHeaderId);
                if (orderHeader == null)
                {
                    resp.IsSuccess = false;
                    resp.Message = "Order not found.";
                    return resp;
                }
                if (string.IsNullOrWhiteSpace(orderHeader.StripeSessionId))
                {
                    resp.IsSuccess = false;
                    resp.Message = "Stripe session not found.";
                    return resp;
                }

                var service = new SessionService();
                var session = await service.GetAsync(orderHeader.StripeSessionId);

                if (string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
                {
                    orderHeader.Status = SD.Status_Approved;
                    orderHeader.PaymentIntentId = session.PaymentIntentId;
                    await _db.SaveChangesAsync();
                    resp.IsSuccess = true;
                    resp.Result = _mapper.Map<OrderHeaderDto>(orderHeader);
                    return resp;
                }

                resp.IsSuccess = false;
                resp.Message = $"Payment status: {session.PaymentStatus}";
            }
            catch (Exception ex)
            {
                resp.IsSuccess = false;
                resp.Message = ex.GetBaseException().Message;
            }
            return resp;
        }

        // Update order status
        [Authorize]
        [HttpPost("UpdateOrderStatus/{orderId:int}")]
        public async Task<ResponseDto> UpdateOrderStatus(int orderId, [FromBody] string newStatus)
        {
            var resp = new ResponseDto();
            try
            {
                if (string.IsNullOrWhiteSpace(newStatus))
                {
                    resp.IsSuccess = false;
                    resp.Message = "Invalid status.";
                    return resp;
                }

                var orderHeader = await _db.OrderHeaders.FirstOrDefaultAsync(o => o.OrderHeaderId == orderId);
                if (orderHeader == null)
                {
                    resp.IsSuccess = false;
                    resp.Message = "Order not found.";
                    return resp;
                }

                // Only allow a set of known statuses
                var allowed = new[]
                {
                    SD.Status_Approved,
                    SD.Status_ReadyForPickup,
                    SD.Status_Completed,
                    SD.Status_Cancelled,
                    SD.Status_Refunded,
                    SD.Status_Pending
                };
                if (!allowed.Contains(newStatus))
                {
                    resp.IsSuccess = false;
                    resp.Message = "Unsupported status value.";
                    return resp;
                }

                orderHeader.Status = newStatus;
                await _db.SaveChangesAsync();

                resp.IsSuccess = true;
                resp.Result = _mapper.Map<DtoOrderHeader>(orderHeader);
            }
            catch (Exception ex)
            {
                resp.IsSuccess = false;
                resp.Message = ex.GetBaseException().Message;
            }
            return resp;
        }
    }
}
