using Microsoft.Identity.Client.TelemetryCore.TelemetryClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using SZR_TechnologistApp.Models;

namespace SZR_TechnologistApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private string _token;
        private readonly string _baseUrl;
        private readonly ApiService _apiService;

        public ApiService()
        {
            _baseUrl = ConfigurationManager.AppSettings["ApiBaseUrl"] ?? "https://localhost:44362";
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void SetToken(string token)
        {
            _token = token;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // ========================= АВТОРИЗАЦИЯ =========================
        public async Task<TokenResponseDto> LoginAsync(string login, string password)
        {
            var request = new { login, password };
            var response = await PostAsync<object, ApiResponse<TokenResponseDto>>("api/auth/login", request);
            if (response != null && response.Success) return response.Data;
            throw new Exception(response?.Message ?? "Ошибка авторизации");
        }

        // ========================= ПРОДУКЦИЯ =========================
        public async Task<PagedResult<ProductDto>> GetProductsAsync(int page = 1, int pageSize = 20, string search = null, string status = null)
        {
            var url = $"api/products?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(search)) url += $"&search={Uri.EscapeDataString(search)}";
            if (!string.IsNullOrEmpty(status)) url += $"&status={Uri.EscapeDataString(status)}";
            var response = await GetAsync<ApiResponse<List<ProductDto>>>(url);
            return new PagedResult<ProductDto> { Items = response.Data, Pagination = response.Pagination };
        }

        public async Task<ProductDto> GetProductByIdAsync(int id)
        {
            var response = await GetAsync<ApiResponse<ProductDto>>($"api/products/{id}");
            return response.Data;
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto dto)
        {
            var response = await PostAsync<CreateProductDto, ApiResponse<ProductDto>>("api/products", dto);
            return response.Data;
        }

        public async Task<bool> UpdateProductAsync(int id, UpdateProductDto dto)
        {
            var response = await PutAsync<UpdateProductDto, ApiResponse<object>>($"api/products/{id}", dto);
            return response?.Success == true;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var response = await DeleteAsync<ApiResponse<object>>($"api/products/{id}");
            return response?.Success == true;
        }

        // ========================= РЕЦЕПТУРЫ =========================
        public async Task<PagedResult<RecipeDto>> GetRecipesAsync(int page = 1, int pageSize = 20, int? productId = null, string status = null)
        {
            var url = $"api/recipes?page={page}&pageSize={pageSize}";
            if (productId.HasValue) url += $"&productId={productId.Value}";
            if (!string.IsNullOrEmpty(status)) url += $"&status={Uri.EscapeDataString(status)}";
            var response = await GetAsync<ApiResponse<List<RecipeDto>>>(url);
            return new PagedResult<RecipeDto> { Items = response.Data, Pagination = response.Pagination };
        }

        public async Task<RecipeDetailDto> GetRecipeByIdAsync(int id)
        {
            var response = await GetAsync<ApiResponse<RecipeDetailDto>>($"api/recipes/{id}");
            return response.Data;
        }

        public async Task<RecipeDto> CreateRecipeAsync(CreateRecipeDto dto)
        {
            var response = await PostAsync<CreateRecipeDto, ApiResponse<RecipeDto>>("api/recipes", dto);
            return response.Data;
        }

        public async Task<bool> UpdateRecipeComponentsAsync(int recipeId, UpdateRecipeComponentsDto dto)
        {
            var response = await PutAsync<UpdateRecipeComponentsDto, ApiResponse<object>>($"api/recipes/{recipeId}/components", dto);
            return response?.Success == true;
        }
        public async Task<bool> ChangePasswordAsync(string oldPassword, string newPassword)
        {
            var request = new { oldPassword, newPassword };
            var response = await PostAsync<object, ApiResponse<object>>("api/auth/change-password", request);
            return response?.Success == true;
        }

        public async Task<bool> ApproveRecipeAsync(int recipeId)
        {
            var response = await PostAsync<object, ApiResponse<object>>($"api/recipes/{recipeId}/approve", null);
            return response?.Success == true;
        }

        public async Task<bool> DeleteRecipeAsync(int id)
        {
            var response = await DeleteAsync<ApiResponse<object>>($"api/recipes/{id}");
            return response?.Success == true;
        }

        // ========================= ТЕХКАРТЫ =========================
        public async Task<PagedResult<TechCardDto>> GetTechCardsAsync(int page = 1, int pageSize = 20, int? productId = null, string status = null)
        {
            var url = $"api/techcards?page={page}&pageSize={pageSize}";
            if (productId.HasValue) url += $"&productId={productId.Value}";
            if (!string.IsNullOrEmpty(status)) url += $"&status={Uri.EscapeDataString(status)}";
            var response = await GetAsync<ApiResponse<List<TechCardDto>>>(url);
            return new PagedResult<TechCardDto> { Items = response.Data, Pagination = response.Pagination };
        }

        public async Task<TechCardDetailDto> GetTechCardByIdAsync(int id)
        {
            var response = await GetAsync<ApiResponse<TechCardDetailDto>>($"api/techcards/{id}");
            return response.Data;
        }

        public async Task<TechCardDto> CreateTechCardAsync(CreateTechCardDto dto)
        {
            var response = await PostAsync<CreateTechCardDto, ApiResponse<TechCardDto>>("api/techcards", dto);
            return response.Data;
        }

        public async Task<TechStepDto> AddTechStepAsync(int techCardId, AddTechStepDto dto)
        {
            var response = await PostAsync<AddTechStepDto, ApiResponse<TechStepDto>>($"api/techcards/{techCardId}/steps", dto);
            return response.Data;
        }

        public async Task<bool> UpdateTechStepAsync(int techCardId, int stepId, UpdateTechStepDto dto)
        {
            var response = await PutAsync<UpdateTechStepDto, ApiResponse<object>>($"api/techcards/{techCardId}/steps/{stepId}", dto);
            return response?.Success == true;
        }

        public async Task<bool> DeleteTechStepAsync(int techCardId, int stepId)
        {
            var response = await DeleteAsync<ApiResponse<object>>($"api/techcards/{techCardId}/steps/{stepId}");
            return response?.Success == true;
        }

        public async Task<bool> ReorderTechStepsAsync(int techCardId, ReorderStepsDto dto)
        {
            var response = await PutAsync<ReorderStepsDto, ApiResponse<object>>($"api/techcards/{techCardId}/steps/reorder", dto);
            return response?.Success == true;
        }

        public async Task<bool> ApproveTechCardAsync(int id)
        {
            var response = await PostAsync<object, ApiResponse<object>>($"api/techcards/{id}/approve", null);
            return response?.Success == true;
        }

        public async Task<bool> DeleteTechCardAsync(int id)
        {
            var response = await DeleteAsync<ApiResponse<object>>($"api/techcards/{id}");
            return response?.Success == true;
        }

        // ========================= ЗАКАЗЫ =========================
        public async Task<PagedResult<ProductionOrderDto>> GetOrdersAsync(int page = 1, int pageSize = 20, string status = null, string product = null)
        {
            var url = $"api/orders?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(status)) url += $"&status={Uri.EscapeDataString(status)}";
            if (!string.IsNullOrEmpty(product)) url += $"&product={Uri.EscapeDataString(product)}";
            var response = await GetAsync<ApiResponse<List<ProductionOrderDto>>>(url);
            return new PagedResult<ProductionOrderDto> { Items = response.Data, Pagination = response.Pagination };
        }

        public async Task<ProductionOrderDto> GetOrderByIdAsync(int id)
        {
            var response = await GetAsync<ApiResponse<ProductionOrderDto>>($"api/orders/{id}");
            return response.Data;
        }

        public async Task<ProductionOrderDto> CreateOrderAsync(CreateOrderDto dto)
        {
            var response = await PostAsync<CreateOrderDto, ApiResponse<ProductionOrderDto>>("api/orders", dto);
            return response.Data;
        }

        public async Task<ProductionOrderDto> UpdateOrderAsync(int id, UpdateOrderDto dto)
        {
            var response = await PutAsync<UpdateOrderDto, ApiResponse<ProductionOrderDto>>($"api/orders/{id}", dto);
            return response.Data;
        }

        public async Task<bool> StartOrderAsync(int id)
        {
            var response = await PostAsync<object, ApiResponse<object>>($"api/orders/{id}/start", null);
            return response?.Success == true;
        }

        public async Task<ProductionBatchDto> CreateBatchFromOrderAsync(int orderId, CreateBatchFromOrderDto dto)
        {
            var response = await PostAsync<CreateBatchFromOrderDto, ApiResponse<ProductionBatchDto>>($"api/orders/{orderId}/batches", dto);
            return response.Data;
        }

        public async Task<bool> CancelOrderAsync(int id)
        {
            var response = await DeleteAsync<ApiResponse<object>>($"api/orders/{id}");
            return response?.Success == true;
        }

        // ========================= ПАРТИИ =========================
        public async Task<PagedResult<ProductionBatchDto>> GetBatchesAsync(int page = 1, int pageSize = 20)
        {
            var response = await GetAsync<ApiResponse<List<ProductionBatchDto>>>($"api/orders/batches?page={page}&pageSize={pageSize}");
            return new PagedResult<ProductionBatchDto> { Items = response.Data, Pagination = response.Pagination };
        }

        public async Task<ProductionBatchDto> GetBatchByNumberAsync(string batchNumber)
        {
            var response = await GetAsync<ApiResponse<ProductionBatchDto>>($"api/production/batches/{batchNumber}");
            return response.Data;
        }

        public async Task<BatchProgramDto> GetBatchProgramAsync(string batchNumber)
        {
            var response = await GetAsync<ApiResponse<BatchProgramDto>>($"api/operator/batch/{batchNumber}/program");
            return response.Data;
        }

        // ========================= АКТИВНЫЕ ПАРТИИ (аппаратчик, нужно технологу) =========================
        public async Task<PagedResult<ActiveBatchDto>> GetActiveBatchesAsync(int page = 1, int pageSize = 20, string line = null, string status = null, string product = null)
        {
            var url = $"api/operator/active-batches?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(line)) url += $"&line={Uri.EscapeDataString(line)}";
            if (!string.IsNullOrEmpty(status)) url += $"&status={Uri.EscapeDataString(status)}";
            if (!string.IsNullOrEmpty(product)) url += $"&product={Uri.EscapeDataString(product)}";
            var response = await GetAsync<ApiResponse<List<ActiveBatchDto>>>(url);
            return new PagedResult<ActiveBatchDto> { Items = response.Data, Pagination = response.Pagination };
        }

        // ========================= ШАГИ =========================
        public async Task<bool> StartStepAsync(string batchNumber, int stepNumber)
        {
            var response = await PostAsync<object, ApiResponse<object>>($"api/operator/batch/{batchNumber}/step/{stepNumber}/start", null);
            return response?.Success == true;
        }

        public async Task<bool> CompleteStepAsync(string batchNumber, int stepNumber, object actualParams)
        {
            var dto = new { actualParams };
            var response = await PostAsync<object, ApiResponse<object>>($"api/operator/batch/{batchNumber}/step/{stepNumber}/complete", dto);
            return response?.Success == true;
        }

        // ========================= ОТКЛОНЕНИЯ =========================
        public async Task<PagedResult<DeviationEventDto>> GetDeviationsAsync(int? batchId = null, int page = 1, int pageSize = 20)
        {
            var url = batchId.HasValue
                ? $"api/events?batchId={batchId.Value}&page={page}&pageSize={pageSize}"
                : $"api/events?page={page}&pageSize={pageSize}";
            var response = await GetAsync<ApiResponse<List<DeviationEventDto>>>(url);
            return new PagedResult<DeviationEventDto> { Items = response.Data, Pagination = response.Pagination };
        }

        public async Task<List<DeviationEventDto>> GetLatestEventsAsync(int limit = 10)
        {
            var response = await GetAsync<ApiResponse<List<DeviationEventDto>>>($"api/events/latest?limit={limit}");
            return response.Data;
        }

        public async Task<List<DeviationEventDto>> GetCriticalEventsAsync(int limit = 10)
        {
            var response = await GetAsync<ApiResponse<List<DeviationEventDto>>>($"api/events/critical?limit={limit}");
            return response.Data;
        }

        public async Task<bool> RegisterDeviationAsync(RegisterDeviationDto dto)
        {
            var response = await PostAsync<RegisterDeviationDto, ApiResponse<object>>("api/events", dto);
            return response?.Success == true;
        }

        // ========================= DASHBOARD =========================
        public async Task<DashboardDataDto> GetDashboardDataAsync()
        {
            var response = await GetAsync<ApiResponse<DashboardDataDto>>("api/dashboard");
            return response?.Data;
        }

        // ========================= ЭКСТРУДЕР =========================
        public async Task<PagedResult<ExtruderProgramDto>> GetExtruderProgramsAsync(int page = 1, int pageSize = 10, int? productId = null)
        {
            var url = $"api/extruder/programs?page={page}&pageSize={pageSize}";
            if (productId.HasValue) url += $"&productId={productId.Value}";
            var response = await GetAsync<ApiResponse<List<ExtruderProgramDto>>>(url);
            return new PagedResult<ExtruderProgramDto> { Items = response.Data, Pagination = response.Pagination };
        }

        public async Task<ExtruderProgramDetailDto> GetExtruderProgramByIdAsync(int id)
        {
            var response = await GetAsync<ApiResponse<ExtruderProgramDetailDto>>($"api/extruder/programs/{id}");
            return response.Data;
        }

        public async Task<ExtruderProgramDto> CreateExtruderProgramAsync(CreateExtruderProgramDto dto)
        {
            var response = await PostAsync<CreateExtruderProgramDto, ApiResponse<ExtruderProgramDto>>("api/extruder/programs", dto);
            return response.Data;
        }

        public async Task<bool> ActivateExtruderProgramAsync(int programId)
        {
            var response = await PutAsync<object, ApiResponse<object>>($"api/extruder/programs/{programId}/activate", null);
            return response?.Success == true;
        }

        public async Task<bool> LoadProgramToExtruderAsync(int programId, string batchNumber)
        {
            var dto = new { batchNumber };
            var response = await PostAsync<object, ApiResponse<object>>($"api/extruder/load/{programId}", dto);
            return response?.Success == true;
        }

        public async Task<List<TelemetryDto>> GetExtruderTelemetryAsync(string batchNumber, int limit = 100)
        {
            var response = await GetAsync<ApiResponse<List<TelemetryDto>>>($"api/extruder/telemetry/{batchNumber}?limit={limit}");
            return response.Data;
        }

        // ========================= СПРАВОЧНИКИ =========================
        public async Task<List<EquipmentDto>> GetEquipmentAsync(bool onlyActive = true)
        {
            var response = await GetAsync<ApiResponse<List<EquipmentDto>>>("api/equipment");
            var items = response.Data;
            if (onlyActive) items = items.FindAll(e => e.IsActive);
            return items;
        }

        public async Task<List<string>> GetLinesAsync()
        {
            var response = await GetAsync<ApiResponse<List<string>>>("api/equipment/lines");
            return response.Data;
        }

        public async Task<List<RoleDto>> GetRolesAsync()
        {
            var response = await GetAsync<ApiResponse<List<RoleDto>>>("api/roles/list");
            return response.Data;
        }

        public async Task<List<StatusItem>> GetBatchStatusesAsync()
        {
            var response = await GetAsync<ApiResponse<List<StatusItem>>>("api/statuses/batch");
            return response.Data;
        }

        public async Task<List<StatusItem>> GetStepStatusesAsync()
        {
            var response = await GetAsync<ApiResponse<List<StatusItem>>>("api/statuses/step");
            return response.Data;
        }

        public async Task<List<StatusItem>> GetLabStatusesAsync()
        {
            var response = await GetAsync<ApiResponse<List<StatusItem>>>("api/statuses/lab-batch");
            return response.Data;
        }

        public async Task<List<StatusItem>> GetOrderStatusesAsync()
        {
            var response = await GetAsync<ApiResponse<List<StatusItem>>>("api/statuses/order");
            return response.Data;
        }

        public async Task<List<StatusItem>> GetRecipeStatusesAsync()
        {
            var response = await GetAsync<ApiResponse<List<StatusItem>>>("api/statuses/recipe");
            return response.Data;
        }

        // ========================= СЫРЬЁ =========================
        public async Task<List<RawMaterialDto>> GetRawMaterialsAsync(bool onlyActive = true, string search = null, string category = null)
        {
            var url = "api/raw-materials?page=1&pageSize=100";
            if (!string.IsNullOrEmpty(search)) url += $"&search={Uri.EscapeDataString(search)}";
            if (!string.IsNullOrEmpty(category)) url += $"&category={Uri.EscapeDataString(category)}";
            var response = await GetAsync<ApiResponse<List<RawMaterialDto>>>(url);
            var items = response.Data;
            if (onlyActive) items = items.FindAll(r => r.IsActive);
            return items;
        }

        // ========================= ОТЧЁТЫ =========================
        public async Task<byte[]> GetReportBatchesAsync(DateTime from, DateTime to)
        {
            var url = $"api/reports/batches?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&format=csv";
            return await DownloadFileAsync(url);
        }

        public async Task<byte[]> GetReportDeviationsAsync(DateTime from, DateTime to)
        {
            var url = $"api/reports/deviations?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&format=csv";
            return await DownloadFileAsync(url);
        }

        public async Task<byte[]> GetReportRecipeUsageAsync(DateTime from, DateTime to)
        {
            var url = $"api/reports/recipe-usage?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&format=csv";
            return await DownloadFileAsync(url);
        }

        public async Task<byte[]> GetReportExtruderEventsAsync(DateTime from, DateTime to)
        {
            var url = $"api/reports/extruder-events?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&format=csv";
            return await DownloadFileAsync(url);
        }

        public async Task<byte[]> GetReportLabBlocksAsync(DateTime from, DateTime to)
        {
            var url = $"api/reports/lab-blocks?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&format=csv";
            return await DownloadFileAsync(url);
        }

        // ========================= АВАТАРЫ =========================
        public async Task<byte[]> GetAvatarAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"api/users/{userId}/avatar");
            if (response.IsSuccessStatusCode) return await response.Content.ReadAsByteArrayAsync();
            return null;
        }

        public async Task<bool> UploadAvatarAsync(int userId, byte[] imageData, string fileName)
        {
            using (var content = new MultipartFormDataContent())
            {
                var fileContent = new ByteArrayContent(imageData);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                content.Add(fileContent, "avatar", fileName);
                var response = await _httpClient.PostAsync($"api/users/{userId}/avatar", content);
                return response.IsSuccessStatusCode;
            }
        }
        // Отчёт по партиям (JSON)
        public async Task<List<BatchReportItem>> GetBatchesReportJsonAsync(DateTime from, DateTime to)
        {
            var url = $"api/reports/batches?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
            var response = await GetAsync<ApiResponse<List<BatchReportItem>>>(url);
            return response?.Data;
        }

        // Отчёт по отклонениям (JSON)
        public async Task<List<DeviationReportItem>> GetDeviationsReportJsonAsync(DateTime from, DateTime to)
        {
            var url = $"api/reports/deviations?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
            var response = await GetAsync<ApiResponse<List<DeviationReportItem>>>(url);
            return response?.Data;
        }
        public async Task<object> GetReferenceAllAsync()
        {
            var response = await GetAsync<ApiResponse<object>>("api/reference/all");
            return response?.Data;
        }

        // ========================= БАЗОВЫЕ HTTP МЕТОДЫ =========================
        public async Task<TResponse> GetAsync<TResponse>(string url)
        {
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new Exception($"Ошибка API ({response.StatusCode}): {content}");
            return JsonConvert.DeserializeObject<TResponse>(content);
        }

        private async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest data)
        {
            var json = JsonConvert.SerializeObject(data, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new Exception($"Ошибка API ({response.StatusCode}): {responseContent}");
            return JsonConvert.DeserializeObject<TResponse>(responseContent);
        }

        private async Task<TResponse> PutAsync<TRequest, TResponse>(string url, TRequest data)
        {
            var json = JsonConvert.SerializeObject(data, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new Exception($"Ошибка API ({response.StatusCode}): {responseContent}");
            return JsonConvert.DeserializeObject<TResponse>(responseContent);
        }

        private async Task<TResponse> DeleteAsync<TResponse>(string url)
        {
            var response = await _httpClient.DeleteAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new Exception($"Ошибка API ({response.StatusCode}): {responseContent}");
            return JsonConvert.DeserializeObject<TResponse>(responseContent);
        }

        private async Task<byte[]> DownloadFileAsync(string url)
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }
        public async Task<UserInfoDto> RegisterAsync(string login, string password, string fullName, string department = null)
        {
            var request = new { login, password, fullName, department };
            var response = await PostAsync<object, ApiResponse<UserInfoDto>>("api/auth/register", request);
            if (response != null && response.Success) return response.Data;
            throw new Exception(response?.Message ?? "Ошибка регистрации");
        }
    }

    // ========================= ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ =========================
    public class PaginationInfo
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public PaginationInfo Pagination { get; set; } = new PaginationInfo();
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string Message { get; set; }
        public PaginationInfo Pagination { get; set; }
    }
}