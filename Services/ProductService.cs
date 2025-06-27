using AutoMapper;
using DsiCode.Micro.Product.API.Data;
using DsiCode.Micro.Product.API.Models.Dto;
using DsiCode.Micro.Product.API.Services.IServices;

namespace DsiCode.Micro.Product.API.Services
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ProductService> _logger;

        public ProductService(AppDbContext db, IMapper mapper, IWebHostEnvironment webHostEnvironment,
            IHttpContextAccessor httpContextAccessor, ILogger<ProductService> logger)
        {
            _db = db;
            _mapper = mapper;
            _webHostEnvironment = webHostEnvironment;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<ResponseDto> CreateProductAsync(ProductDto productDto)
        {
            try
            {
                _logger.LogInformation("Creating product: {ProductName}", productDto.Name);

                DsiCode.Micro.Product.API.Models.Product product = _mapper.Map<DsiCode.Micro.Product.API.Models.Product>(productDto);

                if (productDto.Image != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(productDto.Image.FileName);
                    string filePath = @"wwwroot\ProductImages\" + fileName;

                    var directoryLocation = Path.Combine(Directory.GetCurrentDirectory(), filePath);

                    // Crear directorio si no existe
                    var directory = Path.GetDirectoryName(directoryLocation);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    FileInfo file = new FileInfo(directoryLocation);
                    if (file.Exists)
                    {
                        file.Delete();
                    }

                    using (var fileStream = new FileStream(directoryLocation, FileMode.Create))
                    {
                        await productDto.Image.CopyToAsync(fileStream);
                    }

                    var request = _httpContextAccessor.HttpContext?.Request;
                    var baseUrl = $"{request?.Scheme}://{request?.Host.Value}{request?.PathBase.Value}";
                    product.ImageUrl = baseUrl + "/ProductImages/" + fileName;
                    product.ImageLocalPath = filePath;

                    _logger.LogInformation("Image saved: {FileName} at {FilePath}", fileName, filePath);
                }
                else
                {
                    // Imagen por defecto si no se proporciona una
                    product.ImageUrl = "https://placehold.co/600x400";
                    product.ImageLocalPath = "";
                    _logger.LogInformation("No image provided, using default placeholder");
                }

                _db.Productos.Add(product);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Product created successfully: {ProductId} - {ProductName}", product.ProductId, product.Name);

                return new ResponseDto()
                {
                    Result = _mapper.Map<ProductDto>(product)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product: {ProductName}", productDto.Name);
                return new ResponseDto()
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ResponseDto> DeleteProductAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting product: {ProductId}", id);

                DsiCode.Micro.Product.API.Models.Product product = _db.Productos.FirstOrDefault(u => u.ProductId == id);
                if (product == null)
                {
                    _logger.LogWarning("Product not found for deletion: {ProductId}", id);
                    return new ResponseDto()
                    {
                        IsSuccess = false,
                        Message = "Producto no encontrado"
                    };
                }

                if (!string.IsNullOrEmpty(product.ImageLocalPath))
                {
                    var oldFilePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), product.ImageLocalPath);
                    FileInfo file = new FileInfo(oldFilePathDirectory);
                    if (file.Exists)
                    {
                        file.Delete();
                        _logger.LogInformation("Deleted image file: {FilePath}", oldFilePathDirectory);
                    }
                }

                _db.Productos.Remove(product);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Product deleted successfully: {ProductId}", id);

                return new ResponseDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {ProductId}", id);
                return new ResponseDto()
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ResponseDto> GetAllProductsAsync()
        {
            try
            {
                _logger.LogInformation("Getting all products");

                IEnumerable<DsiCode.Micro.Product.API.Models.Product> objList = _db.Productos;
                var result = _mapper.Map<IEnumerable<ProductDto>>(objList);

                _logger.LogInformation("Retrieved {ProductCount} products", objList.Count());

                return new ResponseDto()
                {
                    Result = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all products");
                return new ResponseDto()
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ResponseDto> GetProductByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Getting product by ID: {ProductId}", id);

                DsiCode.Micro.Product.API.Models.Product product = _db.Productos.FirstOrDefault(u => u.ProductId == id);
                if (product == null)
                {
                    _logger.LogWarning("Product not found: {ProductId}", id);
                    return new ResponseDto()
                    {
                        IsSuccess = false,
                        Message = "Producto no encontrado"
                    };
                }

                var result = _mapper.Map<ProductDto>(product);
                _logger.LogInformation("Retrieved product: {ProductId} - {ProductName}", product.ProductId, product.Name);

                return new ResponseDto()
                {
                    Result = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product by ID: {ProductId}", id);
                return new ResponseDto()
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ResponseDto> UpdateProductAsync(ProductDto productDto)
        {
            try
            {
                _logger.LogInformation("Updating product: {ProductId} - {ProductName}", productDto.ProductId, productDto.Name);

                // Obtener el producto existente
                var existingProduct = _db.Productos.FirstOrDefault(p => p.ProductId == productDto.ProductId);
                if (existingProduct == null)
                {
                    _logger.LogWarning("Product not found for update: {ProductId}", productDto.ProductId);
                    return new ResponseDto()
                    {
                        IsSuccess = false,
                        Message = "Producto no encontrado"
                    };
                }

                // Actualizar los campos básicos
                existingProduct.Name = productDto.Name;
                existingProduct.Price = productDto.Price;
                existingProduct.Description = productDto.Description ?? "";
                existingProduct.CategoryName = productDto.CategoryName;

                // Solo actualizar imagen si se proporciona una nueva
                if (productDto.Image != null)
                {
                    _logger.LogInformation("Updating image for product: {ProductId}", productDto.ProductId);

                    // Eliminar imagen anterior si existe
                    if (!string.IsNullOrEmpty(existingProduct.ImageLocalPath))
                    {
                        var oldFilePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), existingProduct.ImageLocalPath);
                        FileInfo file = new FileInfo(oldFilePathDirectory);
                        if (file.Exists)
                        {
                            file.Delete();
                            _logger.LogInformation("Deleted old image file: {FilePath}", oldFilePathDirectory);
                        }
                    }

                    // Guardar nueva imagen
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(productDto.Image.FileName);
                    string filePath = @"wwwroot\ProductImages\" + fileName;
                    var filePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), filePath);

                    // Crear directorio si no existe
                    var directory = Path.GetDirectoryName(filePathDirectory);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (var fileStream = new FileStream(filePathDirectory, FileMode.Create))
                    {
                        await productDto.Image.CopyToAsync(fileStream);
                    }

                    var request = _httpContextAccessor.HttpContext?.Request;
                    var baseUrl = $"{request?.Scheme}://{request?.Host.Value}{request?.PathBase.Value}";
                    existingProduct.ImageUrl = baseUrl + "/ProductImages/" + fileName;
                    existingProduct.ImageLocalPath = filePath;

                    _logger.LogInformation("New image saved: {FileName} at {FilePath}", fileName, filePath);
                }
                else
                {
                    _logger.LogInformation("No new image provided, keeping existing image for product: {ProductId}", productDto.ProductId);
                }

                _db.Productos.Update(existingProduct);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Product updated successfully: {ProductId} - {ProductName}", existingProduct.ProductId, existingProduct.Name);

                return new ResponseDto()
                {
                    Result = _mapper.Map<ProductDto>(existingProduct)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {ProductId}", productDto.ProductId);
                return new ResponseDto()
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }
    }
}