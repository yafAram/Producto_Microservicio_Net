using AutoMapper;
using DsiCode.Micro.Product.API.Models.Dto;

namespace DsiCode.Micro.Product.API
{
    public class MappingConfig
    {
        public static MapperConfiguration RegisterMaps()
        {
            var mappingConfig = new MapperConfiguration(config =>
            {
                config.CreateMap<ProductDto, DsiCode.Micro.Product.API.Models.Product>()
                .ReverseMap();
            });
            return mappingConfig;
        }
    }
}
