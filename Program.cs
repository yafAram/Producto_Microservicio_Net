using AutoMapper;
using DsiCode.Micro.Product.API;
using DsiCode.Micro.Product.API.Data;
using DsiCode.Micro.Product.API.Extensions;
using DsiCode.Micro.Product.API.Services;
using DsiCode.Micro.Product.API.Services.IServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(options =>
{
    // Deshabilitar validaciones automáticas del modelo
    options.SuppressAsyncSuffixInActionNames = false;
}).AddJsonOptions(opts =>
    opts.JsonSerializerOptions.ReferenceHandler =
    System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
);

// Configurar validaciones del modelo
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    // Deshabilitar respuestas automáticas de validación
    options.SuppressModelStateInvalidFilter = true;
});

// HttpContextAccessor for accessing HTTP context
builder.Services.AddHttpContextAccessor();

// Database configuration with retry policy
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    options.UseSqlServer(
        config.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        )
    );
});

// AutoMapper configuration
IMapper mapper = MappingConfig.RegisterMaps().CreateMapper();
builder.Services.AddSingleton(mapper);

// Register services
builder.Services.AddScoped<IProductService, ProductService>();

// Add logging
builder.Services.AddLogging();

// API documentation
builder.Services.AddEndpointsApiExplorer();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Swagger configuration with JWT support
builder.Services.AddSwaggerGen(option =>
{
    option.AddSecurityDefinition(name: JwtBearerDefaults.AuthenticationScheme, securityScheme: new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter the Bearer Authorization string as following: `Bearer Generated-JWT-Token`",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            },
            new string[] {}
        }
    });
});

// Authentication and Authorization
builder.AddAppAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

// Aplicar migraciones automáticas al inicio con manejo de errores
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Intentando aplicar migraciones...");
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Esperar a que SQL Server esté disponible (útil en entornos Docker)
        if (!dbContext.Database.CanConnect())
        {
            logger.LogWarning("No se puede conectar a la base de datos. Reintentando en 5 segundos...");
            Thread.Sleep(5000);
        }

        // Verificar si hay migraciones pendientes
        var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();
        if (pendingMigrations.Any())
        {
            logger.LogInformation("Aplicando {count} migraciones pendientes...", pendingMigrations.Count);
            dbContext.Database.Migrate();
            logger.LogInformation("Migraciones aplicadas con éxito");
        }
        else
        {
            logger.LogInformation("No hay migraciones pendientes");
        }

        // Verificar conexión final
        if (dbContext.Database.CanConnect())
        {
            logger.LogInformation("Conexión a SQL Server establecida correctamente");
        }
        else
        {
            logger.LogError("No se pudo establecer conexión con SQL Server después de las migraciones");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error crítico al aplicar migraciones o conectar a la base de datos");
        // Para entornos de producción, considera agregar reintentos adicionales
        throw; // Relanza la excepción para detener la aplicación
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Producto API V1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Static files for serving product images
app.UseStaticFiles();

// IMPORTANTE: El orden es crucial
app.UseAuthentication();  // Debe ir ANTES de UseAuthorization
app.UseAuthorization();   // Debe ir DESPUÉS de UseAuthentication

app.MapControllers();

app.Run();