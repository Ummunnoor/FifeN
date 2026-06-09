using System;

namespace Domain.Entities.Catalog
{
    /// <summary>
    /// An admin-managed product category. Single category per product in the MVP (no subcategories).
    /// </summary>
    public class Category
    {
        public Guid Id { get; set; }

        /// <summary>Display name, e.g. "Fashion &amp; Clothing".</summary>
        public string Name { get; set; } = default!;

        /// <summary>URL-friendly unique slug, e.g. "fashion-clothing".</summary>
        public string Slug { get; set; } = default!;

        /// <summary>Whether the category is shown to buyers and selectable by vendors.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Ordering hint for category lists.</summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Consumable categories (e.g. food, beauty) force the listing condition to New/Sealed.
        /// </summary>
        public bool IsConsumable { get; set; }

        /// <summary>When the category was created (UTC).</summary>
        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
