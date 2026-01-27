using Application.DTOs;
using Application.DTOs.Product;
using Application.Services.Interfaces;
using AutoMapper;
using Domain.Entities.Product;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations
{
    public class ProductService : IProductService
    {
        private readonly IGeneric<Product> _productRepository;
        private readonly IGeneric<Category> _categoryRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IGeneric<Product> productRepository,IGeneric<Category> categoryRepository,IMapper mapper,ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<BaseResponse<GetProductDTO>> CreateProductAsync(
            CreateProductDTO createProductDTO)
        {
            _logger.LogInformation(
                "Creating product. Name: {Name}, CategoryId: {CategoryId}",
                createProductDTO.Name,
                createProductDTO.CategoryId);

            if (createProductDTO.CategoryId.HasValue)
            {
                var category = await _categoryRepository
                    .GetByIdAsync(createProductDTO.CategoryId.Value);

                if (category == null)
                {
                    _logger.LogWarning(
                        "Category not found. CategoryId: {CategoryId}",
                        createProductDTO.CategoryId);

                    return new BaseResponse<GetProductDTO>(
                        Success: false,
                        Message: "Category not found",
                        Data: null);
                }
            }

            var entity = _mapper.Map<Product>(createProductDTO);
            var createdProduct = await _productRepository.AddAsync(entity);

            _logger.LogInformation(
                "Product created successfully. ProductId: {ProductId}",
                createdProduct.Id);

            return new BaseResponse<GetProductDTO>(
                Success: true,
                Message: "Product created successfully",
                Data: _mapper.Map<GetProductDTO>(createdProduct));
        }

        public async Task<BaseResponse<bool>> DeleteProductAsync(
            DeleteProductDTO deleteProduct)
        {
            _logger.LogInformation(
                "Deleting product. ProductId: {ProductId}",
                deleteProduct.Id);

            int result = await _productRepository.DeleteAsync(deleteProduct.Id);

            if (result == 0)
            {
                _logger.LogWarning(
                    "Product not found for deletion. ProductId: {ProductId}",
                    deleteProduct.Id);

                return new BaseResponse<bool>(
                    Success: false,
                    Message: "Product not found",
                    Data: false);
            }

            _logger.LogInformation(
                "Product deleted successfully. ProductId: {ProductId}",
                deleteProduct.Id);

            return new BaseResponse<bool>(
                Success: true,
                Message: "Product deleted successfully",
                Data: true);
        }

        public async Task<BaseResponse<IEnumerable<GetProductDTO>>> GetAllProductAsync()
        {
            _logger.LogInformation("Retrieving all products");

            var products = await _productRepository.GetAllAsync();

            return new BaseResponse<IEnumerable<GetProductDTO>>(
                Success: true,
                Message: "Products retrieved successfully",
                Data: _mapper.Map<IEnumerable<GetProductDTO>>(products));
        }

        public async Task<BaseResponse<GetProductDTO>> GetProductByIdAsync(Guid id)
        {
            _logger.LogInformation(
                "Retrieving product by id. ProductId: {ProductId}",
                id);

            var product = await _productRepository.GetByIdAsync(id);

            if (product == null)
            {
                _logger.LogWarning(
                    "Product not found. ProductId: {ProductId}",
                    id);

                return new BaseResponse<GetProductDTO>(
                    Success: false,
                    Message: "Product not found",
                    Data: null);
            }

            return new BaseResponse<GetProductDTO>(
                Success: true,
                Message: "Product retrieved successfully",
                Data: _mapper.Map<GetProductDTO>(product));
        }

        public async Task<BaseResponse<GetProductDTO>> UpdateProductAsync(
            UpdateProductDTO updateProductDTO)
        {
            _logger.LogInformation(
                "Updating product. ProductId: {ProductId}",
                updateProductDTO.Id);

            var existingProduct = await _productRepository
                .Query()
                .Include(p => p.Attributes)
                .FirstOrDefaultAsync(p => p.Id == updateProductDTO.Id);

            if (existingProduct == null)
            {
                _logger.LogWarning(
                    "Product not found for update. ProductId: {ProductId}",
                    updateProductDTO.Id);

                return new BaseResponse<GetProductDTO>(
                    Success: false,
                    Message: "Product not found",
                    Data: null);
            }

            _mapper.Map(updateProductDTO, existingProduct);
            var updatedProduct = await _productRepository.UpdateAsync(existingProduct);

            _logger.LogInformation(
                "Product updated successfully. ProductId: {ProductId}",
                updatedProduct.Id);

            return new BaseResponse<GetProductDTO>(
                Success: true,
                Message: "Product updated successfully",
                Data: _mapper.Map<GetProductDTO>(updatedProduct));
        }
    }
}
