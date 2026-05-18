using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using SZR_LaboratoryApp.Models;

namespace SZR_LaboratoryApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private string _token;
        private readonly string _baseUrl;

        public ApiService()
        {
            _baseUrl = "https://localhost:44362"; // ← ВАШ ПОРТ API
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void SetToken(string token)
        {
            _token = token;
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        // ========== АВТОРИЗАЦИЯ ==========
        public async Task<TokenResponseDto> LoginAsync(string login, string password)
        {
            var request = new { login, password };
            var response = await PostAsync<object, ApiResponse<TokenResponseDto>>("api/auth/login", request);
            return response?.Data;
        }

        // ========== ЛАБОРАТОРИЯ - ПАРТИИ СЫРЬЯ ==========
        public async Task<List<RawMaterialBatch>> GetRawMaterialBatchesAsync(string status = null, int page = 1, int pageSize = 20)
        {
            var url = "api/laboratory/raw-material-batches";
            if (!string.IsNullOrEmpty(status))
                url += $"?status={status}";

            try
            {
                var response = await GetAsync<ApiResponse<List<RawMaterialBatch>>>(url);
                return response?.Data ?? new List<RawMaterialBatch>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetRawMaterialBatchesAsync error: {ex.Message}");
                return new List<RawMaterialBatch>();
            }
        }

        // ========== ЛАБОРАТОРИЯ - ПАРТИИ ПРОДУКЦИИ ==========
        public async Task<List<ProductBatch>> GetProductBatchesAsync(string status = null, int page = 1, int pageSize = 20)
        {
            var url = "api/laboratory/product-batches";
            if (!string.IsNullOrEmpty(status))
                url += $"?status={status}";

            try
            {
                var response = await GetAsync<ApiResponse<List<ProductBatch>>>(url);
                return response?.Data ?? new List<ProductBatch>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetProductBatchesAsync error: {ex.Message}");
                return new List<ProductBatch>();
            }
        }

        // ========== ЛАБОРАТОРИЯ - ИСПЫТАНИЯ ==========
        public async Task<LabTest> CreateTestAsync(CreateTestDto dto)
        {
            try
            {
                var response = await PostAsync<CreateTestDto, ApiResponse<LabTest>>("api/laboratory/tests", dto);
                return response?.Data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateTestAsync error: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateTestResultsAsync(int testId, List<LabTestParameter> parameters)
        {
            try
            {
                var dto = new UpdateTestResultsDto
                {
                    Results = parameters.Select(p => new TestResultDto
                    {
                        ParameterId = p.Id,
                        ActualValue = p.ActualValue
                    }).ToArray()
                };

                var response = await PutAsync<UpdateTestResultsDto, ApiResponse<object>>($"api/laboratory/tests/{testId}/results", dto);
                return response?.Success == true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateTestResultsAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<string> CompleteTestAsync(int testId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"CompleteTestAsync: testId={testId}");

                var response = await PostAsync<object, ApiResponse<object>>($"api/laboratory/tests/{testId}/complete", null);

                System.Diagnostics.Debug.WriteLine($"CompleteTestAsync response success: {response != null}");

                return response?.Success == true ? (response?.Message ?? "Завершено") : null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CompleteTestAsync error: {ex.Message}");
                return null;
            }
        }
        public async Task<DecisionInfo> GetDecisionInfoAsync(int batchId, string batchType)
        {
            try
            {
                var url = $"api/laboratory/decision-info?batchId={batchId}&batchType={batchType}";
                var response = await GetAsync<ApiResponse<DecisionInfo>>(url);
                return response?.Data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetDecisionInfoAsync error: {ex.Message}");
                return null;
            }
        }
        public async Task<UserInfoDto> GetUserByIdAsync(int userId)
        {
            try
            {
                var response = await GetAsync<ApiResponse<UserInfoDto>>($"api/users/{userId}");
                return response?.Data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetUserByIdAsync error: {ex.Message}");
                return null;
            }
        }

        public class DecisionInfo
        {
            public string DecisionBy { get; set; }
            public DateTime DecisionAt { get; set; }
            public string Comment { get; set; }
        }

        public async Task<List<LabTest>> GetTestsByObjectAsync(int objectId, string objectType)
        {
            try
            {
                var url = $"api/laboratory/tests?objectId={objectId}&objectType={objectType}";
                System.Diagnostics.Debug.WriteLine($"=== GetTestsByObjectAsync ===");
                System.Diagnostics.Debug.WriteLine($"URL: {url}");

                var response = await GetAsync<ApiResponse<List<LabTest>>>(url);

                System.Diagnostics.Debug.WriteLine($"response is null: {response == null}");
                System.Diagnostics.Debug.WriteLine($"response.Success: {response?.Success}");
                System.Diagnostics.Debug.WriteLine($"response.Data count: {response?.Data?.Count ?? 0}");

                return response?.Data ?? new List<LabTest>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetTestsByObjectAsync error: {ex.Message}");
                return new List<LabTest>();
            }
        }
        public async Task<List<LabTestParameter>> GetTestParametersAsync(int testId)
        {
            try
            {
                var url = $"api/laboratory/tests/{testId}/parameters";
                var response = await GetAsync<ApiResponse<List<LabTestParameter>>>(url);
                return response?.Data ?? new List<LabTestParameter>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetTestParametersAsync error: {ex.Message}");
                return new List<LabTestParameter>();
            }
        }
        public async Task<bool> AddAuditLog(int userId, string action, string entityType, int entityId, string oldValue, string newValue, string ipAddress)
        {
            try
            {
                var dto = new
                {
                    UserId = userId,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    OldValue = oldValue,
                    NewValue = newValue,
                    IpAddress = ipAddress
                };

                var response = await PostAsync<object, ApiResponse<object>>("api/audit/add", dto);
                return response != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddAuditLog error: {ex.Message}");
                return false;
            }
        }
        // ========== ИСТОРИЯ ИЗМЕНЕНИЙ ==========
        public async Task<List<AuditLog>> GetBatchHistoryAsync(int batchId, string batchType)
        {
            try
            {
                var url = $"api/audit/batch-history?batchId={batchId}&batchType={batchType}";
                var response = await GetAsync<ApiResponse<List<AuditLog>>>(url);
                return response?.Data ?? new List<AuditLog>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetBatchHistoryAsync error: {ex.Message}");
                return new List<AuditLog>();
            }
        }

        public async Task<RawMaterialBatch> GetRawMaterialBatchByIdAsync(int id)
        {
            try
            {
                var response = await GetAsync<ApiResponse<RawMaterialBatch>>($"api/raw-material-batches/{id}");
                return response?.Data;
            }
            catch
            {
                return null;
            }
        }

        public async Task<ProductBatch> GetProductBatchByIdAsync(int id)
        {
            try
            {
                var response = await GetAsync<ApiResponse<ProductBatch>>($"api/production/batches/{id}");
                return response?.Data;
            }
            catch
            {
                return null;
            }
        }

        public async Task<ApiResponse<List<LabTest>>> GetTestArchiveAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                return await GetAsync<ApiResponse<List<LabTest>>>($"api/laboratory/tests/archive?page={page}&pageSize={pageSize}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetTestArchiveAsync error: {ex.Message}");
                return null;
            }
        }

        public async Task<LabTest> GetTestByIdAsync(int testId)
        {
            try
            {
                var response = await GetAsync<ApiResponse<LabTest>>($"api/laboratory/tests/{testId}");
                return response?.Data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetTestByIdAsync error: {ex.Message}");
                return null;
            }
        }

        // ========== ЛАБОРАТОРИЯ - РЕШЕНИЯ ==========
        public async Task<bool> DecideRawMaterialBatchAsync(int batchId, string decision, string comment)
        {
            try
            {
                var dto = new RawMaterialDecisionDto
                {
                    BatchId = batchId,
                    Decision = decision,
                    Comment = comment
                };

                var response = await PostAsync<RawMaterialDecisionDto, ApiResponse<object>>("api/laboratory/decisions/raw-material", dto);
                return response?.Success == true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DecideRawMaterialBatchAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DecideProductBatchAsync(int batchId, string decision, string comment = null)
        {
            var dto = new ProductDecisionDto
            {
                BatchId = batchId,
                Decision = decision,
                Comment = comment
            };
            var response = await PostAsync<ProductDecisionDto, ApiResponse<object>>("api/laboratory/decisions/product", dto);
            return response?.Success == true;
        }

        public async Task<bool> HasUnfinishedTestAsync(int objectId, string objectType)
        {
            try
            {
                var tests = await GetTestsByObjectAsync(objectId, objectType);
                return tests != null && tests.Any(t => t.status != "Завершено");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HasUnfinishedTestAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetRawMaterialBatchStatusAsync(int batchId)
        {
            try
            {
                var response = await GetAsync<ApiResponse<RawMaterialBatch>>($"api/raw-material-batches/{batchId}");
                return response?.Data?.labStatus;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetRawMaterialBatchStatusAsync error: {ex.Message}");
                return null;
            }
        }

        // ========== БАЗОВЫЕ HTTP МЕТОДЫ ==========
        private async Task<TResponse> GetAsync<TResponse>(string url)
        {
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Ошибка API: {content}");
            }

            return JsonConvert.DeserializeObject<TResponse>(content);
        }

        private async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest data)
        {
            var json = JsonConvert.SerializeObject(data, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Ошибка API: {responseContent}");
            }

            return JsonConvert.DeserializeObject<TResponse>(responseContent);
        }

        private async Task<TResponse> PutAsync<TRequest, TResponse>(string url, TRequest data)
        {
            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Ошибка API: {responseContent}");
            }

            return JsonConvert.DeserializeObject<TResponse>(responseContent);
        }

        private async Task<bool> DeleteAsync(string url)
        {
            var response = await _httpClient.DeleteAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new Exception($"Ошибка API: {content}");
            }
            return true;
        }
    }

    // ========== DTO КЛАССЫ ==========
    public class TokenResponseDto
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public UserInfoDto User { get; set; }
    }

    public class UserInfoDto
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string Department { get; set; }
        public string AvatarUrl { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string Message { get; set; }
        public PaginationInfo Pagination { get; set; }
    }
    public class PaginationInfo
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
    public class CreateTestDto
    {
        public string TestType { get; set; }
        public string ObjectType { get; set; }
        public int ObjectId { get; set; }
        public string Priority { get; set; }
        public string Comment { get; set; }
        public TestParameterDto[] Parameters { get; set; }
    }

    public class TestParameterDto
    {
        public string ParameterName { get; set; }
        public decimal? NormMin { get; set; }
        public decimal? NormMax { get; set; }
        public string Unit { get; set; }
    }

    public class UpdateTestResultsDto
    {
        [JsonProperty("results")]
        public TestResultDto[] Results { get; set; }
    }

    public class TestResultDto
    {
        [JsonProperty("parameterId")]
        public int ParameterId { get; set; }
        [JsonProperty("actualValue")]
        public decimal? ActualValue { get; set; }
    }

    public class RawMaterialDecisionDto
    {
        [JsonProperty("batchId")]
        public int BatchId { get; set; }
        [JsonProperty("decision")]
        public string Decision { get; set; }
        [JsonProperty("comment")]
        public string Comment { get; set; }
    }

    public class ProductDecisionDto
    {
        [JsonProperty("batchId")]
        public int BatchId { get; set; }
        [JsonProperty("decision")]
        public string Decision { get; set; }
        [JsonProperty("comment")]
        public string Comment { get; set; }
    }

    // Вспомогательный метод для LINQ

}