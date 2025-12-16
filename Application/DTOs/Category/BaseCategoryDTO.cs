namespace Application.DTOs.Category
{
    public abstract class BaseCategoryDTO
    {
        public required string Name { get; set; }         // Category name
        public required string Description { get; set; }  // Optional description
        public string? ImageUrl { get; set; }     // Optional banner/icon image
    }
}
