namespace DsiCode.Micro.Product.API.Models.Dto
{
    public class PagerDto(int Page = 1, int RecordsPerPage = 10)
    {
        private const int MaxRecordPerPage = 50;
        public int Page { get; set; } = Math.Max(1, Page);
        /// <summary>
        ///  clamb me permite identificar un valor valido entre 1 y el valor maximo por la pagina
        /// </summary>
        public int RecordsPerPage { get; set; } = Math.Clamp(RecordsPerPage, 1, MaxRecordPerPage);
    }
}
