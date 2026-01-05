using Application.DTOs;
using Application.DTOs.Product;
using Application.Services.Interfaces;
using Application.Services.Interfaces.IProductService;
using AutoMapper;
using Domain.Entities.Product;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Implementations
{
    public class ProductService : IProductService
    {
        private readonly IGeneric<Product> _productRepository;
        private readonly IGeneric<Category> _categoryRepository;
        private readonly IMapper _mapper;

        public ProductService(IGeneric<Product> productRepository, IGeneric<Category> categoryRepository, IMapper mapper)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        public async Task<BaseResponse<GetProductDTO>> CreateProductAsync(CreateProductDTO createProductDTO)
        {
            try
            {
                if(createProductDTO.CategoryId.HasValue)
                {
                    var category = await _categoryRepository.GetByIdAsync(createProductDTO.CategoryId.Value);
                    if (category == null)
                    {
                        return new BaseResponse<GetProductDTO>(
                            Success: false,
                            Message: "Category not found",
                            Data: null
                        );
                    }
                }
                var entity = _mapper.Map<Product>(createProductDTO);

                var createdProduct = await _productRepository.AddAsync(entity);

                if (createdProduct == null)
                {
                    return new BaseResponse<GetProductDTO>(
                        Success: false,
                        Message: "Product could not be created",
                        Data: null
                    );
                }

                var mapped = _mapper.Map<GetProductDTO>(createdProduct);

                return new BaseResponse<GetProductDTO>(
                    Success: true,
                    Message: "Product created successfully!",
                    Data: mapped
                );
            }
            catch (Exception ex)
            {
                return new BaseResponse<GetProductDTO>(
                    Success: false,
                    Message: $"Error occurred: {ex.Message}",
                    Data: null
                );
            }
        }


        public async Task<BaseResponse<bool>> DeleteProductAsync(DeleteProductDTO deleteProduct)
        {
            try
            {
                int result = await _productRepository.DeleteAsync(deleteProduct.Id);
                if (result > 0)
                    return new BaseResponse<bool>(
                        Success: true,
                        Message: "Product deleted successfully",
                        Data: true
                    );
                return new BaseResponse<bool>(
                Success: false,
                Message: "Product not found",
                Data: false
                );
            }
            catch (Exception ex)
            {

                return new BaseResponse<bool>(
                    Success: false,
                    Message: $"Error occurred: {ex.Message}",
                    Data: false
                );
            }
        }

        public async Task<BaseResponse<IEnumerable<GetProductDTO>>> GetAllProductAsync()
        {
            try
            {
                var products = await _productRepository.GetAllAsync();
                var mapped = _mapper.Map<IEnumerable<GetProductDTO>>(products);
                return new BaseResponse<IEnumerable<GetProductDTO>>(
                    Success: true,
                    Message: "Products retrieved successfully",
                    Data: mapped
                );
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<GetProductDTO>>(
                    Success: false,
                    Message: $"Error: {ex.Message}",
                    Data: []
                );
            }
        }

        public async Task<BaseResponse<GetProductDTO>> GetProductByIdAsync(Guid id)
        {
            try
            {
                var product = await _productRepository.GetByIdAsync(id);

                if (product == null)
                {
                    return new BaseResponse<GetProductDTO>(
                        Success: false,
                        Message: "Product not found",
                        Data: null
                    );
                }

                var mapped = _mapper.Map<GetProductDTO>(product);

                return new BaseResponse<GetProductDTO>(
                    Success: true,
                    Message: "Product retrieved successfully",
                    Data: mapped
                );
            }
            catch (Exception ex)
            {
                return new BaseResponse<GetProductDTO>(
                    Success: false,
                    Message: $"Error occurred: {ex.Message}",
                    Data: null
                );
            }
        }


        public async Task<BaseResponse<GetProductDTO>> UpdateProductAsync(UpdateProductDTO updateProductDTO)
        {
            try
            {
                var existingProduct = await _productRepository.Query()
                    .Include(p => p.Attributes).FirstOrDefaultAsync(p => p.Id == updateProductDTO.Id);
                if (existingProduct == null)
                {
                    return new BaseResponse<GetProductDTO>(
                        Success: false,
                        Message: "Product not found",
                        Data: null
                    );
                }
                _mapper.Map(updateProductDTO, existingProduct);
                var updatedProduct = await _productRepository.UpdateAsync(existingProduct);
                var mapped = _mapper.Map<GetProductDTO>(updatedProduct);
                return new BaseResponse<GetProductDTO>(
                    Success: true,
                    Message: "Product updated successfully!",
                    Data: mapped
                );
            }
            catch (Exception ex)
            {
                return new BaseResponse<GetProductDTO>(
                    Success: false,
                    Message: $"Error occurred: {ex.Message}",
                    Data: null
                );
            }
        }
    }
}