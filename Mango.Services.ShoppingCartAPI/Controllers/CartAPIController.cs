using AutoMapper;
using Mango.Services.ShoppingCartAPI.Data;
using Mango.Services.ShoppingCartAPI.Models;
using Mango.Services.ShoppingCartAPI.Models.Dto;
using Mango.Services.ShoppingCartAPI.Service.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.ShoppingCartAPI.Controllers
{
    [Route("api/cart")]
    [ApiController]
    public class CartAPIController : ControllerBase
    {
        private readonly ResponseDto _response;
        private readonly IMapper _mapper;
        private readonly AppDbContext _db;
        private readonly IProductService _productService;
        private readonly ICouponService _couponService;

        // ✅ Correct Constructor
        public CartAPIController(AppDbContext db, IMapper mapper, IProductService productService, ICouponService couponService)
        {
            _db = db;
            _mapper = mapper;
            _productService = productService;
            _couponService = couponService;
            _response = new ResponseDto();
        }

        [HttpGet("GetCart/{userId}")]
        public async Task<ResponseDto> GetCart(string userId)
        {
            try
            {
                // Gracefully handle users with no cart yet
                var cartHeaderEntity = await _db.CartHeaders.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
                if (cartHeaderEntity == null)
                {
                    _response.Result = new CartDto
                    {
                        CartHeader = new CartHeaderDto { UserId = userId, CartTotal = 0, Discount = 0 },
                        CartDetails = new List<CartDetailsDto>()
                    };
                    return _response;
                }

                CartDto cart = new()
                {
                    CartHeader = _mapper.Map<CartHeaderDto>(cartHeaderEntity)
                };

                var detailsEntities = await _db.CartDetails.AsNoTracking()
                    .Where(u => u.CartHeaderId == cart.CartHeader.CartHeaderId)
                    .ToListAsync();
                cart.CartDetails = _mapper.Map<IEnumerable<CartDetailsDto>>(detailsEntities);

                IEnumerable<ProductDto> productDtos = await _productService.GetProducts();

                foreach (var item in cart.CartDetails)
                {
                    item.Product = productDtos.FirstOrDefault(u => u.ProductId == item.ProductId) ?? new ProductDto();
                    cart.CartHeader.CartTotal += (item.Count * (item.Product?.Price ?? 0));
                }

                if (!string.IsNullOrEmpty(cart.CartHeader.CouponCode))
                {
                    var coupon = await _couponService.GetCoupon(cart.CartHeader.CouponCode);
                    if (coupon != null && cart.CartHeader.CartTotal >= coupon.MinAmount)
                    {
                        cart.CartHeader.Discount = coupon.DiscountAmount;
                    }
                    else
                    {
                        cart.CartHeader.Discount = 0;
                    }
                    cart.CartHeader.CartTotal -= cart.CartHeader.Discount;
                }

                _response.Result = cart;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [HttpPost("ApplyCoupon")]
        public async Task<object> ApplyCoupon([FromBody] CartDto cartDto)
        {
            try
            {
                var cartFromDb = await _db.CartHeaders.FirstOrDefaultAsync(u => u.UserId == cartDto.CartHeader.UserId);
                if (cartFromDb == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Cart not found for user.";
                    return _response;
                }
                cartFromDb.CouponCode = cartDto.CartHeader.CouponCode;
                _db.CartHeaders.Update(cartFromDb);
                await _db.SaveChangesAsync();
                _response.Result = true;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }


        [HttpPost("RemoveCoupon")]
        public async Task<object> RemoveCoupon([FromBody] CartDto cartDto)
        {
            try
            {
                var cartFromDb = await _db.CartHeaders.FirstOrDefaultAsync(u => u.UserId == cartDto.CartHeader.UserId);
                if (cartFromDb == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Cart not found for user.";
                    return _response;
                }
                cartFromDb.CouponCode = "";
                _db.CartHeaders.Update(cartFromDb);
                await _db.SaveChangesAsync();
                _response.Result = true;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [HttpPost("CartUpsert")]
        public async Task<ResponseDto> CartUpsert([FromBody] CartDto cartDto)
        {
            try
            {
                var cartHeaderFromDb = await _db.CartHeaders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == cartDto.CartHeader.UserId);

                if (cartHeaderFromDb == null)
                {
                    CartHeader cartHeader = _mapper.Map<CartHeader>(cartDto.CartHeader);
                    _db.CartHeaders.Add(cartHeader);
                    await _db.SaveChangesAsync();

                    cartDto.CartDetails.First().CartHeaderId = cartHeader.CartHeaderId;
                    _db.CartDetails.Add(_mapper.Map<CartDetails>(cartDto.CartDetails.First()));
                    await _db.SaveChangesAsync();
                }
                else
                {
                    var cartDetailsFromDb = await _db.CartDetails
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.ProductId == cartDto.CartDetails.First().ProductId
                            && u.CartHeaderId == cartHeaderFromDb.CartHeaderId);

                    if (cartDetailsFromDb == null)
                    {
                        cartDto.CartDetails.First().CartHeaderId = cartHeaderFromDb.CartHeaderId;
                        _db.CartDetails.Add(_mapper.Map<CartDetails>(cartDto.CartDetails.First()));
                        await _db.SaveChangesAsync();
                    }
                    else
                    {
                        cartDto.CartDetails.First().Count += cartDetailsFromDb.Count;
                        cartDto.CartDetails.First().CartHeaderId = cartDetailsFromDb.CartHeaderId;
                        cartDto.CartDetails.First().CartDetailsId = cartDetailsFromDb.CartDetailsId;
                        _db.CartDetails.Update(_mapper.Map<CartDetails>(cartDto.CartDetails.First()));
                        await _db.SaveChangesAsync();
                    }
                }

                _response.Result = cartDto;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [HttpPost("RemoveCart")]
        public async Task<ResponseDto> RemoveCart([FromBody] int cartDetailsId)
        {
            try
            {
                var cartDetails = await _db.CartDetails.FirstOrDefaultAsync(u => u.CartDetailsId == cartDetailsId);
                if (cartDetails == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Cart item not found.";
                    return _response;
                }

                int totalItems = await _db.CartDetails
                    .Where(u => u.CartHeaderId == cartDetails.CartHeaderId)
                    .CountAsync();

                _db.CartDetails.Remove(cartDetails);

                if (totalItems == 1)
                {
                    var cartHeader = await _db.CartHeaders
                        .FirstOrDefaultAsync(u => u.CartHeaderId == cartDetails.CartHeaderId);

                    if (cartHeader != null)
                    {
                        _db.CartHeaders.Remove(cartHeader);
                    }
                }

                await _db.SaveChangesAsync();
                _response.Result = true;
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
