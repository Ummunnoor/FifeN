using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Entities.Product
{
    public class Category
    {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Name { get; set; } = null!;
            public required string Description { get; set; }
            public required string? ImageUrl { get; set; }

            public bool IsActive { get; set; } = true;
            public int SortOrder { get; set; }

            public Guid? ParentCategoryId { get; set; }
            public Category? ParentCategory { get; set; }

            public ICollection<Category> SubCategories { get; set; } = new List<Category>();
            public ICollection<Product> Products { get; set; } = new List<Product>();

            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime? UpdatedAt { get; set; }
        }

    }
