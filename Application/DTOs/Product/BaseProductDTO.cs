using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.DTOs.Product
{
    public abstract class BaseProductDTO
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required decimal Price { get; set; }
        public int? Quantity { get; set; }
        public required string ImageUrl { get; set; }
        public string? CategoryId { get; set; }

        // Optional dynamic attributes
        public List<ProductAttributeDTO>? Attributes { get; set; }
    }
}