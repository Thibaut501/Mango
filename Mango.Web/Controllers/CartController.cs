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
        private readonly IBankingService _bankingService;

        public CartController(ICartService cartService, IOrderService orderService, IBankingService bankingService)
        {
            _cartService = cartService;
            _orderService = orderService;
            _bankingService = bankingService;
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
                OrderHeaderId = orderHeaderDto.OrderHeaderId,
                Currency = "usd"
            };

            var stripeResponse = await _orderService.CreateStripeSession(stripeRequestDto);
            if (stripeResponse == null || !stripeResponse.IsSuccess || stripeResponse.Result == null)
            {
                TempData["error"] = "Stripe session creation failed: " + (stripeResponse?.Message ?? "Unknown error");
                return View(cart);
            }

            var stripeResponseResult = JsonConvert.DeserializeObject<StripeRequestDto>(Convert.ToString(stripeResponse.Result));

            if (stripeResponseResult == null || string.IsNullOrEmpty(stripeResponseResult.StripeSessionUrl))
            {
                TempData["error"] = "Invalid Stripe session response.";
                return View(cart);
            }

            // Use framework helper to redirect rather than manual 303 header
            return Redirect(stripeResponseResult.StripeSessionUrl);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Confirmation(int orderId)
        {
            // Auto-validate on initial load so status appears immediately
            var validation = await _orderService.ValidateStripeSession(orderId);
            bool success = validation != null && validation.IsSuccess;
            ViewBag.PaymentSuccess = success;
            ViewBag.PaymentMessage = success ? "Payment confirmed." : (validation?.Message ?? "Payment not confirmed.");

            // If paid, record a CARD payment into Banking so reports stay in sync
            if (success)
            {
                // Fetch order for total and client name
                var getOrder = await _orderService.GetOrder(orderId);
                var order = getOrder != null && getOrder.IsSuccess && getOrder.Result != null
                    ? JsonConvert.DeserializeObject<OrderHeaderDto>(Convert.ToString(getOrder.Result))
                    : null;
                if (order != null)
                {
                    var amount = (decimal)order.OrderTotal;
                    var client = string.IsNullOrWhiteSpace(order.Name) ? (order.Email ?? "Customer") : order.Name;
                    await _bankingService.RecordOrderPaymentAsync(orderId, client, amount, method: "Card", reference: order.PaymentIntentId ?? order.StripeSessionId);
                }
            }

            return View(orderId);
        }

        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ValidatePayment([FromQuery] int orderId)
        {
            var validation = await _orderService.ValidateStripeSession(orderId);
            bool success = validation != null && validation.IsSuccess;
            string message = success ? "Payment confirmed." : (validation?.Message ?? "Payment not confirmed.");

            if (success)
            {
                var getOrder = await _orderService.GetOrder(orderId);
                var order = getOrder != null && getOrder.IsSuccess && getOrder.Result != null
                    ? JsonConvert.DeserializeObject<OrderHeaderDto>(Convert.ToString(getOrder.Result))
                    : null;
                if (order != null)
                {
                    var amount = (decimal)order.OrderTotal;
                    var client = string.IsNullOrWhiteSpace(order.Name) ? (order.Email ?? "Customer") : order.Name;
                    await _bankingService.RecordOrderPaymentAsync(orderId, client, amount, method: "Card", reference: order.PaymentIntentId ?? order.StripeSessionId);
                }
            }

            return Json(new { success, message });
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
