using Microsoft.EntityFrameworkCore;

namespace DsiCode.Micro.Product.API.Extensions
{
    public static class HttpContextExtensions
    {
        public static async Task InsertParamPageHeader<T>(this HttpContext httpContext, IQueryable<T> queryable)
        {
            if (httpContext is null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            // Contemos el total de la consulta que reciba el IQueryable
            // Una vez calculado el total de registros consultados se asigna a la variable total.
            double total = await queryable.CountAsync();

            // Asignamos a la cabecera el total de registros obtenidos
            httpContext.Response.Headers.Append("cantidad-total-registros", total.ToString());
        }
    }
}