using System;
using System.Collections.Generic;

namespace Application.DTOs.Product
{
    public class CreateProductDTO : BaseProductDTO
    {}

    // DTO for product attribute
    public class ProductAttributeDTO
    {
        public required string Key { get; set; }   // e.g., "Size", "Material", "WaterResistance"
        public required string Value { get; set; } // e.g., "XL", "Leather", "30m"
    }
}
