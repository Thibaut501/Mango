using Mango.Web.Models;
using Mango.Web.Service;
using Mango.Web.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Mango.Web.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;

        public CartController(ICartService cartService, IOrderService orderService)
        {
            _cartService = cartService;
            _orderService = orderService;
        }

        public async Task<IActionResult> CartIndex()
        {
            return View(await LoadCartDtoBasedOnLoggedInUser());
        }

        public async Task<IActionResult> Checkout()
        {
            return View(await LoadCartDtoBasedOnLoggedInUser());
        }

        [HttpPost]
        [ActionName("Checkout")]
        public async Task<IActionResult> CheckoutPost(CartDto cartDto)
        {
            CartDto cart = await LoadCartDtoBasedOnLoggedInUser();

            if (cart == null || cart.CartHeader == null)
            {
                TempData["error"] = "There was an issue retrieving the cart.";
                return RedirectToAction(nameof(CartIndex));
            }

            if (cartDto?.CartHeader == null)
            {
                TempData["error"] = "Invalid checkout details.";
                return RedirectToAction(nameof(CartIndex));
            }

            cart.CartHeader.Phone = cartDto.CartHeader.Phone;
            cart.CartHeader.Email = cartDto.CartHeader.Email;
            cart.CartHeader.Name = cartDto.CartHeader.Name;

            var response = await _orderService.CreateOrder(cart);
            if (response == null || !response.IsSuccess || response.Result == null)
            {
                TempData["error"] = "There was an issue creating the order.";
                return View(cart);
            }

            OrderHeaderDto? orderHeaderDto = JsonConvert.DeserializeObject<OrderHeaderDto>(Convert.ToString(response.Result));
            if (orderHeaderDto == null)
            {
                TempData["error"] = "Order deserialization failed.";
                return View(cart);
            }

            var domain = Request.Scheme + "://" + Request.Host.Value + "/";
            StripeRequestDto stripeRequestDto = new()
            {
                ApprovedUrl = domain + "cart/Confirmation?orderId=" + orderHeaderDto.OrderHeaderId,
                CancelUrl = domain + "cart/checkout",
                OrderHeader = orderHeaderDto
            };

            var stripeResponse = await _orderService.CreateStripeSession(stripeRequestDto);
            if (stripeResponse == null || !stripeResponse.IsSuccess || stripeResponse.Result == null)
            {
                TempData["error"] = "Stripe session creation failed: " + (stripeResponse?.Message ?? "Unknown error");
                return View(cart);
            }

            StripeRequestDto? stripeResponseResult = JsonConvert.DeserializeObject<StripeRequestDto>(Convert.ToString(stripeResponse.Result));

            if (stripeResponseResult == null || string.IsNullOrEmpty(stripeResponseResult.StripeSessionUrl))
            {
                TempData["error"] = "Invalid Stripe session response.";
                return View(cart);
            }

            Response.Headers.Add("Location", stripeResponseResult.StripeSessionUrl);
            return new StatusCodeResult(303);
        }

        public IActionResult Confirmation(int orderId)
        {
            return View(orderId);
        }

        public async Task<IActionResult> Remove(int cartDetailsId)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var response = await _cartService.RemoveFromCartAsync(cartDetailsId);

            if (response != null && response.IsSuccess)
            {
                TempData["success"] = "Item removed successfully.";
                return RedirectToAction(nameof(CartIndex));
            }

            TempData["error"] = "There was an issue removing the item from the cart.";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ApplyCoupon(CartDto cartDto)
        {
            var response = await _cartService.ApplyCouponAsync(cartDto);

            if (response != null && response.IsSuccess)
            {
                TempData["success"] = "Coupon applied successfully.";
                return RedirectToAction(nameof(CartIndex));
            }

            TempData["error"] = "Failed to apply coupon.";
            return View(cartDto);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveCoupon(CartDto cartDto)
        {
            cartDto.CartHeader.CouponCode = "";
            var response = await _cartService.ApplyCouponAsync(cartDto);

            if (response != null && response.IsSuccess)
            {
                TempData["success"] = "Coupon removed successfully.";
                return RedirectToAction(nameof(CartIndex));
            }

            TempData["error"] = "Failed to remove coupon.";
            return View(cartDto);
        }

        private async Task<CartDto> LoadCartDtoBasedOnLoggedInUser()
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var response = await _cartService.GetCartByUserIdAsnyc(userId);

            if (response != null && response.IsSuccess && response.Result != null)
            {
                var cartDto = JsonConvert.DeserializeObject<CartDto>(Convert.ToString(response.Result));
                cartDto.CartDetails ??= new List<CartDetailsDto>();
                cartDto.CartHeader ??= new CartHeaderDto();
                return cartDto;
            }

            return new CartDto
            {
                CartHeader = new CartHeaderDto(),
                CartDetails = new List<CartDetailsDto>()
            };
        }
    }
}
