using AutoMapper;
using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;
using Mango.Services.OrderAPI.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    }
}