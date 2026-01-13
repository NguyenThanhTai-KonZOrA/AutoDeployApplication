namespace ClientLauncher.Common.ApiClient
{
    public interface IApiClient
    {
        Task<TResponse?> GetAsync<TResponse>(string url);
        Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data);
        Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data, Dictionary<string, string>? headers);
        Task<TResponse?> PostAsync<TResponse>(string url, MultipartFormDataContent dataContent);
    }
}
