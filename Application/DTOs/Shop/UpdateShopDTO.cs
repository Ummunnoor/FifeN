using System;

namespace Application.DTOs.Shop
{
    /// <summary>
    /// DTO for updating shop details
    /// </summary>
    public class UpdateShopDTO : BaseShopDTO
    {
        /// <summary>Shop identifier</summary>
        public Guid Id { get; set; }
    }
}
