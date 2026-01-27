using Application.DTOs;
using Application.DTOs.Category;
using Application.Services.Interfaces;
using AutoMapper;
using Domain.Entities.Product;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly IGeneric<Category> _categoryRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(IGeneric<Category> categoryRepository, IMapper mapper, ILogger<CategoryService> logger)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<BaseResponse<GetCategoryDTO>> CreateCategoryAsync(CreateCategoryDTO createCategoryDTO)
        {
            _logger.LogInformation( "Creating category. Name: {Name}", createCategoryDTO.Name);

            var entity = _mapper.Map<Category>(createCategoryDTO);
            var createdCategory = await _categoryRepository.AddAsync(entity);

            _logger.LogInformation(
                "Category created successfully. CategoryId: {CategoryId}",
                createdCategory.Id);

            return new BaseResponse<GetCategoryDTO>(
                Success: true,
                Message: "Category created successfully",
                Data: _mapper.Map<GetCategoryDTO>(createdCategory));
        }

        public async Task<BaseResponse<bool>> DeleteCategoryAsync(Guid id)
        {
            _logger.LogInformation(
                "Deleting category. CategoryId: {CategoryId}",
                id);

            int result = await _categoryRepository.DeleteAsync(id);

            if (result == 0)
            {
                _logger.LogWarning(
                    "Category not found for deletion. CategoryId: {CategoryId}",
                    id);

                return new BaseResponse<bool>(
                    Success: false,
                    Message: "Category not found",
                    Data: false);
            }

            _logger.LogInformation(
                "Category deleted successfully. CategoryId: {CategoryId}",
                id);

            return new BaseResponse<bool>(
                Success: true,
                Message: "Category deleted successfully",
                Data: true);
        }

        public async Task<BaseResponse<IEnumerable<GetCategoryDTO>>> GetAllCategoryAsync()
        {
            _logger.LogInformation("Retrieving all categories");

            var categories = await _categoryRepository.GetAllAsync();

            return new BaseResponse<IEnumerable<GetCategoryDTO>>(
                Success: true,
                Message: "Categories retrieved successfully",
                Data: _mapper.Map<IEnumerable<GetCategoryDTO>>(categories));
        }

        public async Task<BaseResponse<GetCategoryDTO>> GetCategoryByIdAsync(Guid id)
        {
            _logger.LogInformation(
                "Retrieving category by id. CategoryId: {CategoryId}",
                id);

            var category = await _categoryRepository.GetByIdAsync(id);

            if (category == null)
            {
                _logger.LogWarning(
                    "Category not found. CategoryId: {CategoryId}",
                    id);

                return new BaseResponse<GetCategoryDTO>(
                    Success: false,
                    Message: "Category not found",
                    Data: null);
            }

            return new BaseResponse<GetCategoryDTO>(
                Success: true,
                Message: "Category retrieved successfully",
                Data: _mapper.Map<GetCategoryDTO>(category));
        }

        public async Task<BaseResponse<GetCategoryDTO>> UpdateCategoryAsync(
            UpdateCategoryDTO updateCategoryDTO)
        {
            _logger.LogInformation(
                "Updating category. CategoryId: {CategoryId}",
                updateCategoryDTO.Id);

            var category = await _categoryRepository
                .GetByIdAsync(updateCategoryDTO.Id);

            if (category == null)
            {
                _logger.LogWarning(
                    "Category not found for update. CategoryId: {CategoryId}",
                    updateCategoryDTO.Id);

                return new BaseResponse<GetCategoryDTO>(
                    Success: false,
                    Message: "Category not found",
                    Data: null);
            }

            _mapper.Map(updateCategoryDTO, category);
            var updatedCategory = await _categoryRepository.UpdateAsync(category);

            _logger.LogInformation(
                "Category updated successfully. CategoryId: {CategoryId}",
                updatedCategory.Id);

            return new BaseResponse<GetCategoryDTO>(
                Success: true,
                Message: "Category updated successfully",
                Data: _mapper.Map<GetCategoryDTO>(updatedCategory));
        }
    }
}
