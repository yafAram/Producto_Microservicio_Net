using DsiCode.Micro.Product.API.Models.Dto;
using DsiCode.Micro.Product.API.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DsiCode.Micro.Product.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductService productService, ILogger<ProductController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        [HttpGet]
        [Route("GetAll")]
        public async Task<ResponseDto> GetAll()
        {
            return await _productService.GetAllProductsAsync();
        }

        [HttpGet]
        public async Task<ResponseDto> Get()
        {
            return await _productService.GetAllProductsAsync();
        }

        [HttpGet]
        [Route("{id:int}")]
        public async Task<ResponseDto> Get(int id)
        {
            return await _productService.GetProductByIdAsync(id);
        }

        [HttpPost]
        [Authorize(Roles = "ADMINISTRATOR")]
        public async Task<ResponseDto> Post([FromForm] ProductDto productDto)
        {
            try
            {
                _logger.LogInformation("Creating product: {ProductName} by user: {User}", productDto.Name, User.Identity?.Name);
                _logger.LogInformation("Received ProductDto - Name: {Name}, Price: {Price}, Category: {Category}, ImageUrl: {ImageUrl}, ImageLocalPath: {ImageLocalPath}, HasImage: {HasImage}",
                    productDto.Name, productDto.Price, productDto.CategoryName, productDto.ImageUrl ?? "null", productDto.ImageLocalPath ?? "null", productDto.Image != null);

                // Log ModelState para debug
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState is invalid for product creation:");
                    foreach (var modelError in ModelState)
                    {
                        foreach (var error in modelError.Value.Errors)
                        {
                            _logger.LogWarning("ModelState Error - Key: {Key}, Error: {Error}", modelError.Key, error.ErrorMessage);
                        }
                    }

                    return new ResponseDto
                    {
                        IsSuccess = false,
                        Message = "Datos del modelo inválidos: " + string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))
                    };
                }

                // Validar datos básicos manualmente
                if (string.IsNullOrEmpty(productDto.Name))
                {
                    return new ResponseDto
                    {
                        IsSuccess = false,
                        Message = "El nombre del producto es requerido"
                    };
                }

                if (productDto.Price <= 0)
                {
                    return new ResponseDto
                    {
                        IsSuccess = false,
                        Message = "El precio debe ser mayor a 0"
                    };
                }

                if (string.IsNullOrEmpty(productDto.CategoryName))
                {
                    return new ResponseDto
                    {
                        IsSuccess = false,
                        Message = "La categoría es requerida"
                    };
                }

                // Asegurar que los campos de imagen tengan valores por defecto
                if (string.IsNullOrEmpty(productDto.ImageUrl))
                {
                    productDto.ImageUrl = "";
                    _logger.LogInformation("Set ImageUrl to empty string");
                }

                if (string.IsNullOrEmpty(productDto.ImageLocalPath))
                {
                    productDto.ImageLocalPath = "";
                    _logger.LogInformation("Set ImageLocalPath to empty string");
                }

                var result = await _productService.CreateProductAsync(productDto);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Product created successfully: {ProductName}", productDto.Name);
                }
                else
                {
                    _logger.LogWarning("Failed to create product: {ProductName}, Error: {Error}", productDto.Name, result.Message);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product: {ProductName}", productDto.Name);
                return new ResponseDto
                {
                    IsSuccess = false,
                    Message = $"Error interno del servidor: {ex.Message}"
                };
            }
        }

        [HttpPut]
        [Authorize(Roles = "ADMINISTRATOR")]
        public async Task<ResponseDto> Put([FromForm] ProductDto productDto)
        {
            try
            {
                _logger.LogInformation("Updating product: {ProductId} - {ProductName} by user: {User}",
                    productDto.ProductId, productDto.Name, User.Identity?.Name);

                // Validar datos básicos
                if (productDto.ProductId <= 0)
                {
                    return new ResponseDto
                    {
                        IsSuccess = false,
                        Message = "ID de producto inválido"
                    };
                }

                if (string.IsNullOrEmpty(productDto.Name))
                {
                    return new ResponseDto
                    {
                        IsSuccess = false,
                        Message = "El nombre del producto es requerido"
                    };
                }

                if (productDto.Price <= 0)
                {
                    return new ResponseDto
                    {
                        IsSuccess = false,
                        Message = "El precio debe ser mayor a 0"
                    };
                }

                if (string.IsNullOrEmpty(productDto.CategoryName))
                {
                    return new ResponseDto
                    {
                        IsSuccess = false,
                        Message = "La categoría es requerida"
                    };
                }

                var result = await _productService.UpdateProductAsync(productDto);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Product updated successfully: {ProductId} - {ProductName}",
                        productDto.ProductId, productDto.Name);
                }
                else
                {
                    _logger.LogWarning("Failed to update product: {ProductId} - {ProductName}, Error: {Error}",
                        productDto.ProductId, productDto.Name, result.Message);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {ProductId} - {ProductName}",
                    productDto.ProductId, productDto.Name);
                return new ResponseDto
                {
                    IsSuccess = false,
                    Message = $"Error interno del servidor: {ex.Message}"
                };
            }
        }

        [HttpDelete]
        [Route("{id:int}")]
        [Authorize(Roles = "ADMINISTRATOR")]
        public async Task<ResponseDto> Delete(int id)
        {
            try
            {
                _logger.LogInformation("Deleting product: {ProductId} by user: {User}", id, User.Identity?.Name);

                var result = await _productService.DeleteProductAsync(id);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Product deleted successfully: {ProductId}", id);
                }
                else
                {
                    _logger.LogWarning("Failed to delete product: {ProductId}, Error: {Error}", id, result.Message);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {ProductId}", id);
                return new ResponseDto
                {
                    IsSuccess = false,
                    Message = $"Error interno del servidor: {ex.Message}"
                };
            }
        }
    }
}