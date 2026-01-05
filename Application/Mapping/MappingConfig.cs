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
            CreateMap<UpdateProductDTO, Product>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CategoryId, opt => opt.Ignore())
            .ForMember(dest => dest.Attributes, opt => opt.Ignore());


            CreateMap<ProductAttributeDTO, ProductAttribute>();
            CreateMap<ProductAttribute, ProductAttributeDTO>();


            CreateMap<CreateCategoryDTO, Category>();
            CreateMap<Category, GetCategoryDTO>();
            CreateMap<UpdateCategoryDTO, Category>();


        }
    }
}