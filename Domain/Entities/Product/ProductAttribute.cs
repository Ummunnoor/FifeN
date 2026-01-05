using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Entities.Product
{
    public class ProductAttribute
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public required string Key { get; set; }      // e.g. "ModelNo", "WaterResistance", "Color", "Size"
        public required string Value { get; set; }    // "RX009", "30m", "Black", "XL"

        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}