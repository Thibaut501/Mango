using Mango.Web.Models;
using Mango.Web.Service.IService;
using Mango.Web.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace Mango.Web.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public IActionResult OrderIndex()
        {
            return View();
        }

        public async Task<IActionResult> OrderDetail(int orderId)
        {
            var userId = User.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Sub)?.Value;
            var response = await _orderService.GetOrder(orderId);

            if (response == null || !response.IsSuccess || response.Result == null)
            {
                TempData["error"] = "Order not found or failed to retrieve.";
                return RedirectToAction(nameof(OrderIndex));
            }

            var orderHeaderDto = JsonConvert.DeserializeObject<OrderHeaderDto>(Convert.ToString(response.Result));

            if (orderHeaderDto == null)
            {
                TempData["error"] = "Failed to load order details.";
                return RedirectToAction(nameof(OrderIndex));
            }

            if (!User.IsInRole(SD.RoleAdmin) && userId != orderHeaderDto.UserId)
            {
                return NotFound();
            }

            return View(orderHeaderDto);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string status)
        {
            IEnumerable<OrderHeaderDto> list = new List<OrderHeaderDto>();
            string userId = User.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Sub)?.Value ?? "";
            ResponseDto? response;

            if (User.IsInRole(SD.RoleAdmin))
            {
                response = await _orderService.GetAllOrders();
            }
            else
            {
                response = await _orderService.GetAllOrder(userId);
            }

            if (response != null && response.IsSuccess && response.Result != null)
            {
                var jsonString = Convert.ToString(response.Result);
                if (!string.IsNullOrEmpty(jsonString))
                {
                    list = JsonConvert.DeserializeObject<List<OrderHeaderDto>>(jsonString) ?? new List<OrderHeaderDto>();
                }
            }

            if (!string.IsNullOrEmpty(status) && status.ToLower() != "all")
            {
                status = status.ToLower();
                list = status switch
                {
                    "approved" => list.Where(u => u.Status.Equals(SD.Status_Approved, StringComparison.OrdinalIgnoreCase)),
                    "readyforpickup" => list.Where(u => u.Status.Equals(SD.Status_ReadyForPickup, StringComparison.OrdinalIgnoreCase)),
                    "cancelled" => list.Where(u => u.Status.Equals(SD.Status_Cancelled, StringComparison.OrdinalIgnoreCase) || u.Status.Equals(SD.Status_Refunded, StringComparison.OrdinalIgnoreCase)),
                    _ => list
                };
            }

            return Json(new { data = list.OrderByDescending(u => u.OrderHeaderId) });
        }
    }
}
