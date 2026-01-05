using Application.DTOs;
using Application.DTOs.Product;
using Application.Services.Interfaces.IProductService;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        public ProductController(IProductService productService)
        {
            _productService = productService;
        }
        [HttpGet("get-all-products")]
        public async Task<BaseResponse<IEnumerable<GetProductDTO>>> GetAllProducts()
        {
            var products = await _productService.GetAllProductAsync();
            return products;
        }

        [HttpGet("get-product/{id}")]
        public async Task<BaseResponse<GetProductDTO>> GetProductById(Guid id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            return product;
        }

        [HttpPost("create-product")]
        public async Task<BaseResponse<GetProductDTO>> CreateProduct([FromBody] CreateProductDTO createProductDTO)
        {
            var product = await _productService.CreateProductAsync(createProductDTO);
            return product;
        }
        [HttpPut("update-product")]
        public async Task<BaseResponse<GetProductDTO>> UpdateProduct([FromBody] UpdateProductDTO updateProductDTO)
        {
            var product = await _productService.UpdateProductAsync(updateProductDTO);
            return product;
        }
        [HttpDelete("delete-product/{id}")]
        public async Task<BaseResponse<bool>> DeleteProduct([FromRoute] Guid id)
        {
            var result = await _productService.DeleteProductAsync(new DeleteProductDTO { Id = id });
            return result;
        }
    }
}