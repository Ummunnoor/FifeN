using Application.DTOs;
using Application.DTOs.Product;

namespace Application.Services.Interfaces
{
    public interface IProductService
    {
        Task<BaseResponse<IEnumerable<GetProductDTO>>> GetAllProductAsync();
        Task<BaseResponse<GetProductDTO>> GetProductByIdAsync(Guid id);
        Task<BaseResponse<GetProductDTO>> CreateProductAsync(CreateProductDTO createProductDTO);
        Task<BaseResponse<GetProductDTO>> UpdateProductAsync(UpdateProductDTO updateProductDTO);
        Task<BaseResponse<bool>> DeleteProductAsync(DeleteProductDTO deleteProductDTO);
    }
}