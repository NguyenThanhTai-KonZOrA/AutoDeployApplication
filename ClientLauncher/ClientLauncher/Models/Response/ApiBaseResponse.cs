namespace ClientLauncher.Models.Response
{
    public class ApiBaseResponse<T>
    {
        public int Status { get; set; }
        public T? Data { get; set; }
        public bool Success { get; set; }
    }
}