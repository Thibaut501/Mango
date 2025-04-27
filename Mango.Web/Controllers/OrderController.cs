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
            var orderHeaderDto = new OrderHeaderDto();
            var userId = User.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Sub)?.Value;

            var response = await _orderService.GetOrder(orderId);
            if (response != null && response.IsSuccess)
            {
                var jsonResult = JsonConvert.SerializeObject(response.Result);
                orderHeaderDto = JsonConvert.DeserializeObject<OrderHeaderDto>(jsonResult) ?? new OrderHeaderDto();
            }

            if (!User.IsInRole(SD.RoleAdmin) && userId != orderHeaderDto.UserId)
            {
                return NotFound();
            }
            return View(orderHeaderDto);
        }

        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeaderDto> list = new List<OrderHeaderDto>();
            string userId = "";

            ResponseDto? response;

            if (User.IsInRole(SD.RoleAdmin))
            {
                response = _orderService.GetAllOrders().GetAwaiter().GetResult();
            }
            else
            {
                userId = User.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Sub)?.Value ?? "";
                response = _orderService.GetAllOrder(userId).GetAwaiter().GetResult();
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
                    "approved" => list.Where(u => u.Status == SD.Status_Approved),
                    "readyforpickup" => list.Where(u => u.Status == SD.Status_ReadyForPickup),
                    "cancelled" => list.Where(u => u.Status == SD.Status_Cancelled || u.Status == SD.Status_Refunded),
                    _ => list
                };
            }

            return Json(new { data = list.OrderByDescending(u => u.OrderHeaderId) });
        }
    }
}
