namespace ClientLauncher.Common.PagedResult
{
    public abstract class BasePagingRequest
    {
        public int? Take { get; set; }
        public int? Skip { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }
}