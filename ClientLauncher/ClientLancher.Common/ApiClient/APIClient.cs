using ClientLauncher.Common.ApiClient;
using Newtonsoft.Json;
using System.Text;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TResponse?> GetAsync<TResponse>(string url)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url);
            var content = await SafeReadContentAsync(response);

            if (!response.IsSuccessStatusCode)
            {
                LogBadResponse(response, content);
            }

            return DeserializeOrDefault<TResponse>(content);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"GET request failed: {ex.Message}");
            return default;
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonConvert.SerializeObject(data), encoding: null, "application/json")
        };

        // Log cURL
        var json = JsonConvert.SerializeObject(data);

        try
        {
            using var response = await _httpClient.SendAsync(request);
            var content = await SafeReadContentAsync(response);

            if (!response.IsSuccessStatusCode)
            {
                LogBadResponse(response, content);
            }

            return DeserializeOrDefault<TResponse>(content);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"POST request failed: {ex.Message}");
            return default;
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data, Dictionary<string, string>? headers)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json")
        };

        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        try
        {
            using var response = await _httpClient.SendAsync(request);
            var content = await SafeReadContentAsync(response);

            if (!response.IsSuccessStatusCode)
            {
                LogBadResponse(response, content);
            }

            return DeserializeOrDefault<TResponse>(content);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"POST request (with headers) failed: {ex.Message}");
            return default;
        }
    }

    public async Task<TResponse?> PostAsync<TResponse>(string url, MultipartFormDataContent dataContent)
    {
        try
        {
            using var response = await _httpClient.PostAsync(url, dataContent);
            var content = await SafeReadContentAsync(response);

            if (!response.IsSuccessStatusCode)
            {
                LogBadResponse(response, content);
            }

            return DeserializeOrDefault<TResponse>(content);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"POST multipart request failed: {ex.Message}");
            return default;
        }
    }

    private static async Task<string> SafeReadContentAsync(HttpResponseMessage response)
    {
        try
        {
            return await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return string.Empty;
        }
    }

    private static TResponse? DeserializeOrDefault<TResponse>(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return default;

        try
        {
            return JsonConvert.DeserializeObject<TResponse>(content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to deserialize response to {typeof(TResponse).Name}: {ex.Message}");
            return default;
        }
    }

    private static void LogBadResponse(HttpResponseMessage response, string content)
    {
        var url = response.RequestMessage?.RequestUri?.ToString() ?? "(unknown)";
        Console.WriteLine($"HTTP {(int)response.StatusCode} {response.ReasonPhrase} for {url}");
        if (!string.IsNullOrWhiteSpace(content))
        {
            Console.WriteLine($"Response body: {content}");
        }
    }
}