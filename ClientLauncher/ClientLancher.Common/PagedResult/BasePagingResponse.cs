namespace ClientLancher.Common.PagedResult
{
    public abstract class BasePagingResponse
    {
        // Paging
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPrevious { get; set; }
        public bool HasNext { get; set; }
    }
}