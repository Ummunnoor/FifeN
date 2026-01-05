using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Entities.Product
{
    public class ProductAccessory
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Image { get; set; }
        public decimal Price { get; set; }

        public Guid ProductId { get; set; } 
        public Product Product { get; set; } = null!;
    }

}
