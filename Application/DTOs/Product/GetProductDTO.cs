using System;

namespace Application.DTOs.Product
{
    public class GetProductDTO : BaseProductDTO
    {
        public required string Id { get; set; }
        public string? CategoryName { get; set; }

    }

}
