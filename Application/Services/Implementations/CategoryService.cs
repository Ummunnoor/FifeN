using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Application.DTOs.Category;
using Application.Services.Interfaces;
using Application.Services.Interfaces.IProductService;
using AutoMapper;
using Domain.Entities.Product;

namespace Application.Services.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly IGeneric<Category> _categoryRepository;
        private readonly IMapper _mapper;
        public CategoryService(IGeneric<Category> categoryRepository, IMapper mapper)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        public async Task<BaseResponse<GetCategoryDTO>> CreateCategoryAsync(CreateCategoryDTO createCategoryDTO)
        {
            try
            {
                var entity = _mapper.Map<Category>(createCategoryDTO);
                var createdCategory = await _categoryRepository.AddAsync(entity);
                if (createdCategory == null)
                {
                    return new BaseResponse<GetCategoryDTO>(
                        Success: false,
                        Message: "Category could not be created",
                        Data: null
                    );
                }

                var categoryDTO = _mapper.Map<GetCategoryDTO>(createdCategory);
                return new BaseResponse<GetCategoryDTO>(
                    Success: true,
                    Message: "Category created successfully",
                    Data: categoryDTO
                );
            }
            catch (Exception ex)
            {
                return new BaseResponse<GetCategoryDTO>(
                    Success: false,
                    Message: $"Error occurred: {ex.Message}",
                    Data: null
                );
            }
        }

        public async Task<BaseResponse<bool>> DeleteCategoryAsync(Guid id)
        {
            try
            {
                int result = await _categoryRepository.DeleteAsync(id);
                if (result > 0)
                {
                    return new BaseResponse<bool>(
                        Success: true,
                        Message: "Category deleted successfully!",
                        Data: true
                    );
                }
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

        public async Task<BaseResponse<IEnumerable<GetCategoryDTO>>> GetAllCategoryAsync()
        {
            try
            {
                var categories = await _categoryRepository.GetAllAsync();
            var mappedCategories = _mapper.Map<IEnumerable<GetCategoryDTO>>(categories);
            return new BaseResponse<IEnumerable<GetCategoryDTO>>(
                Success: true,
                Message: "Categories retrieved successfully",
                Data: mappedCategories
            );
            }
            catch (Exception ex)
            {   
                return new BaseResponse<IEnumerable<GetCategoryDTO>>(
                    Success: false,
                    Message: $"Error: {ex.Message}",
                    Data: []
                );
            }
        }

        public async Task<BaseResponse<GetCategoryDTO>> GetCategoryByIdAsync(Guid id)
        {
            try
            {
                var category =  await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return new BaseResponse<GetCategoryDTO>(
                    Success: false,
                    Message: "Category not found",
                    Data: null
                );
            }
            var categoryDTO = _mapper.Map<GetCategoryDTO>(category);
            return new BaseResponse<GetCategoryDTO>(
                Success: true,
                Message: "Category retrieved successfully",
                Data: categoryDTO
            );
            }
            catch (Exception ex)
            {
                return new BaseResponse<GetCategoryDTO>(
                    Success: false,
                    Message: $"Error occurred: {ex.Message}",
                    Data: null
                );
            }
        }

        public async Task<BaseResponse<GetCategoryDTO>> UpdateCategoryAsync(UpdateCategoryDTO updateCategoryDTO)
        {
           try
           {
                var category = await _categoryRepository.GetByIdAsync(updateCategoryDTO.Id);
                if (category == null)
                {
                    return new BaseResponse<GetCategoryDTO>(
                        Success: false,
                        Message: "Category not found",
                        Data: null
                    );
                }
                _mapper.Map(updateCategoryDTO, category);
                var updatedCategory = await _categoryRepository.UpdateAsync(category);
                var categoryDTO = _mapper.Map<GetCategoryDTO>(updatedCategory);
                return new BaseResponse<GetCategoryDTO>(
                    Success: true,
                    Message: "Category updated successfully",
                    Data: categoryDTO
                );
           }
           catch (Exception ex)
           {
            
                return new BaseResponse<GetCategoryDTO>(
                    Success: false,
                    Message: $"Error occurred: {ex.Message}",
                    Data: null
                );
           }
        }
    }
}