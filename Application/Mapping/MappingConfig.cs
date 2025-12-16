using Application.DTOs.Category;
using Application.DTOs.Product;
using AutoMapper;
using Domain.Entities.Product;

namespace Application.Mapping
{
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            CreateMap<CreateProductDTO, Product>();
            CreateMap<Product, GetProductDTO>();

            CreateMap<CreateCategoryDTO, Category>();
            CreateMap<Category, GetCategoryDTO>();

        }
    }
}