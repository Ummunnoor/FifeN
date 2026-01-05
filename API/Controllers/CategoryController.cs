using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Application.DTOs.Category;
using Application.Services.Interfaces.IProductService;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {   
        private readonly ICategoryService _categoryService;
        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }
        [HttpGet("get-all-categories")]
        public async Task<BaseResponse<IEnumerable<GetCategoryDTO>>> GetAllCategories()
        {
            return await _categoryService.GetAllCategoryAsync();
        }

        [HttpGet("get-category/{id}")]
        public async Task<BaseResponse<GetCategoryDTO>> GetCategoryById(Guid id)
        {
            return await _categoryService.GetCategoryByIdAsync(id);
        }

        [HttpPost("create-category")]
        public async Task<BaseResponse<GetCategoryDTO>> CreateCategory([FromBody] CreateCategoryDTO createCategoryDTO)
        {
            return await _categoryService.CreateCategoryAsync(createCategoryDTO);
        }

        [HttpPut("update-category")]
        public async Task<BaseResponse<GetCategoryDTO>> UpdateCategory([FromBody] UpdateCategoryDTO updateCategoryDTO)
        {
            return await _categoryService.UpdateCategoryAsync(updateCategoryDTO);
        }
        [HttpDelete("delete-category/{id}")]
        public async Task<BaseResponse<bool>> DeleteCategory([FromRoute] Guid id)
        {
            return await _categoryService.DeleteCategoryAsync(id);
        }

        
    }

}