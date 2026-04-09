using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Healthcare.Client.APIClient
{
    /// <summary>
    /// Lớp cơ sở dùng chung cho tất cả các Client gọi API.
    /// Xử lý các thao tác cơ bản: cấu hình Base URL, parse JSON, và quản lý Token.
    /// </summary>
    public abstract class BaseHttpClient
    {
        protected readonly HttpClient _httpClient;
        protected readonly JsonSerializerOptions _jsonOptions;

        protected BaseHttpClient(string baseUrl)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };

            // Cấu hình tự động map JSON không phân biệt chữ hoa/thường (CamelCase vs PascalCase)
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// (Tùy chọn) Thêm Access Token (JWT) vào Header nếu API yêu cầu đăng nhập
        /// </summary>
        public void SetBearerToken(string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        /// <summary>
        /// Gọi API dạng GET và tự động map dữ liệu trả về thành Object
        /// </summary>
        protected async Task<T> GetAsync<T>(string endpoint)
        {
            var response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
            }

            // Quản lý lỗi
            var errorMsg = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"API Error [{response.StatusCode}]: {errorMsg}");
        }

        /// <summary>
        /// Gọi API dạng POST, gửi đi một Object và nhận về một Object
        /// </summary>
        protected async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
            }

            var errorMsg = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"API Error [{response.StatusCode}]: {errorMsg}");
        }

        /// <summary>
        /// Gọi API dạng POST nhưng chỉ quan tâm gửi thành công, không cần map dữ liệu trả về
        /// </summary>
        protected async Task<bool> PostAsync<TRequest>(string endpoint, TRequest data)
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Gọi API dạng PUT để cập nhật dữ liệu
        /// </summary>
        protected async Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            var response = await _httpClient.PutAsJsonAsync(endpoint, data, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
            }

            var errorMsg = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"API Error [{response.StatusCode}]: {errorMsg}");
        }

        /// <summary>
        /// Gọi API dạng DELETE để xóa dữ liệu
        /// </summary>
        protected async Task<bool> DeleteAsync(string endpoint)
        {
            var response = await _httpClient.DeleteAsync(endpoint);
            return response.IsSuccessStatusCode;
        }
    }
}