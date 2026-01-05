namespace Application.DTOs.Category
{
    public abstract class BaseCategoryDTO
    {
        public required string Name { get; set; }   
        public string? Description { get; set; }      // Category name
        public string? ImageUrl { get; set; }     // Optional banner/icon image
    }
}
