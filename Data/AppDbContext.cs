using Microsoft.EntityFrameworkCore;
using DsiCode.Micro.Product.API.Models;

namespace DsiCode.Micro.Product.API.Data
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<DsiCode.Micro.Product.API.Models.Product> Productos { get; set; }
    }
}
