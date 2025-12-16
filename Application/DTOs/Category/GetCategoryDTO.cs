using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Product;

namespace Application.DTOs.Category
{
    public class GetCategoryDTO : BaseCategoryDTO
    {
        public required string Id { get; set; }

        // Optional: include products in this category
        public List<GetProductDTO>? Products { get; set; }
    }
}