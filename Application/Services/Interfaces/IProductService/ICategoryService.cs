using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Application.DTOs.Category;

namespace Application.Services.Interfaces.IProductService
{
    public interface ICategoryService
    {
        Task<BaseResponse<IEnumerable<GetCategoryDTO>>> GetAllCategoryAsync();
        Task<BaseResponse<GetCategoryDTO>> GetCategoryByIdAsync(Guid id);
        Task<BaseResponse<GetCategoryDTO>> CreateCategoryAsync(CreateCategoryDTO createCategoryDTO);
        Task<BaseResponse<GetCategoryDTO>> UpdateCategoryAsync(UpdateCategoryDTO updateCategoryDTO);
        Task<BaseResponse<bool>> DeleteCategoryAsync(Guid id);
    }
}