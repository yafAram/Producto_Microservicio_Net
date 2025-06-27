using System.ComponentModel.DataAnnotations;

namespace DsiCode.Micro.Product.API.Models.Dto
{
    public class ProductDto
    {
        public int ProductId { get; set; }

        public string Name { get; set; } = string.Empty;

        public double Price { get; set; }

        public string Description { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        // Campos de imagen completamente opcionales - SIN validaciones
        public string ImageUrl { get; set; } = string.Empty;
        public string ImageLocalPath { get; set; } = string.Empty;

        // La imagen es opcional
        public IFormFile? Image { get; set; }
    }
}