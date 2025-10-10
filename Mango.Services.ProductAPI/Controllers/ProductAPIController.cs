using AutoMapper;
using Mango.ProductAPI.Models.Dto;
using Mango.Services.ProductAPI.Data;
using Mango.Services.ProductAPI.Models;
using Mango.Services.ProductAPI.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mango.Services.ProductAPI.Controllers
{
    [Route("api/product")]
    [ApiController]
    public class ProductAPIController : ControllerBase
    {
        private readonly AppDbContext _db;
        private Mango.ProductAPI.Models.Dto.ResponseDto _response;
        private IMapper _mapper;

        public ProductAPIController(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
            _response = new ResponseDto();
        }

        [HttpGet]
        public ResponseDto Get()
        {
            try
            {
                IEnumerable<Product> objList = _db.Products.ToList();
                _response.Result = _mapper.Map<IEnumerable<ProductDto>>(objList);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [HttpGet]
        [Route("{id:int}")]
        public ResponseDto Get(int id)
        {
            try
            {
                Product obj = _db.Products.First(u => u.ProductId == id);
                _response.Result = _mapper.Map<ProductDto>(obj);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public ResponseDto Post([FromForm] ProductDto ProductDto)
        {
            try
            {
                Product product = _mapper.Map<Product>(ProductDto);
                _db.Products.Add(product);
                _db.SaveChanges();

                // Handle file upload if present
                if (ProductDto.Image != null)
                {
                    var fileName = product.ProductId + Path.GetExtension(ProductDto.Image.FileName);
                    var imagesDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ProductImages");
                    Directory.CreateDirectory(imagesDir);
                    var physicalPath = Path.Combine(imagesDir, fileName);
                    using (var fileStream = new FileStream(physicalPath, FileMode.Create))
                    {
                        ProductDto.Image.CopyTo(fileStream);
                    }

                    var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
                    product.ImageUrl = $"{baseUrl}/ProductImages/{fileName}";
                    product.ImageLocalPath = Path.Combine("wwwroot", "ProductImages", fileName);
                }
                else
                {
                    product.ImageUrl = product.ImageUrl ?? "https://placehold.co/600x400";
                }
                _db.Products.Update(product);
                _db.SaveChanges();
                _response.Result = _mapper.Map<ProductDto>(product);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [HttpPut]
        [Authorize(Roles = "ADMIN")]
        public ResponseDto Put([FromForm] ProductDto ProductDto)
        {
            try
            {
                // Load existing entity to preserve server-managed fields
                var existing = _db.Products.First(u => u.ProductId == ProductDto.ProductId);

                // Map simple fields
                existing.Name = ProductDto.Name;
                existing.Price = ProductDto.Price;
                existing.Description = ProductDto.Description;
                existing.CategoryName = ProductDto.CategoryName;

                // If a new image is posted, replace it; otherwise keep current
                if (ProductDto.Image != null)
                {
                    var fileName = existing.ProductId + Path.GetExtension(ProductDto.Image.FileName);
                    var imagesDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ProductImages");
                    Directory.CreateDirectory(imagesDir);
                    var physicalPath = Path.Combine(imagesDir, fileName);
                    using var fileStream = new FileStream(physicalPath, FileMode.Create);
                    ProductDto.Image.CopyTo(fileStream);

                    var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
                    existing.ImageUrl = $"{baseUrl}/ProductImages/{fileName}";
                    existing.ImageLocalPath = Path.Combine("wwwroot", "ProductImages", fileName);
                }

                _db.Products.Update(existing);
                _db.SaveChanges();

                _response.Result = _mapper.Map<ProductDto>(existing);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [HttpDelete]
        [Route("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public ResponseDto Delete(int id)
        {
            try
            {
                Product obj = _db.Products.First(u => u.ProductId == id);
                _db.Products.Remove(obj);
                _db.SaveChanges();
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
