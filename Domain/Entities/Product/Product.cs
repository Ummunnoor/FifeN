using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Entities.Product
{
    public class Product
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string Name { get; set; }
        public required string Description { get; set; }
        public decimal Price { get; set; }
        public required string CategoryId { get; set; }
        public required Category Category { get; set; }

        public ICollection<ProductAttribute> Attributes { get; set; } = [];
    }

   
}
