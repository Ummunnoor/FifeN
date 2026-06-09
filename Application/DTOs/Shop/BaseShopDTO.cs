namespace Application.DTOs.Shop
{
    /// <summary>
    /// Base DTO for shop creation and updates
    /// </summary>
    public abstract class BaseShopDTO
    {
        /// <summary>Shop display name/business name</summary>
        public required string Name { get; set; }

        /// <summary>Shop description</summary>
        public string? Description { get; set; }

        /// <summary>Shop phone number (can be WhatsApp)</summary>
        public string? PhoneNumber { get; set; }

        /// <summary>Shop address</summary>
        public string? Address { get; set; }

        /// <summary>Latitude for location services</summary>
        public decimal? Latitude { get; set; }

        /// <summary>Longitude for location services</summary>
        public decimal? Longitude { get; set; }

        /// <summary>Shop profile image URL</summary>
        public string? ImageUrl { get; set; }
    }
}
