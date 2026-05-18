using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using SZR_OperatorApp.Models;
using SZR_OperatorApp.Views;

namespace SZR_OperatorApp.Services
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



        // ========== АВТОРИЗАЦИЯ ==========
        public async Task<UserInfoDto> LoginAsync(string login, string password)
        {
            try
            {
                var request = new { login, password };
                var response = await PostAsync<object, ApiResponse<TokenResponseDto>>("api/auth/login", request);

                if (response?.Success != true || response.Data == null)
                {
                    System.Diagnostics.Debug.WriteLine("LoginAsync: ответ не содержит токена");
                    return null;
                }

                var data = response.Data;
                var user = new UserInfoDto
                {
                    Token = data.accessToken ?? "",
                    Id = data.user?.id ?? 0,
                    Login = data.user?.login ?? login,
                    FullName = data.user?.fullName ?? "",
                    Role = data.user?.role ?? "",
                    Department = data.user?.department ?? ""
                };

                System.Diagnostics.Debug.WriteLine($"Parsed - Id: {user.Id}, Login: {user.Login}, Role: {user.Role}");
                return user;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoginAsync error: {ex.Message}");
                return null;
            }
        }
        public async Task<List<Notification>> GetNotificationsAsync()
        {
            try
            {
                var response = await GetAsync<ApiResponse<List<Notification>>>("api/notifications");
                return response?.Data ?? new List<Notification>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetNotificationsAsync error: {ex.Message}");
                return new List<Notification>();
            }
        }
        // ========== ПРОДУКТЫ ==========
        public async Task<List<Product>> GetProductsAsync()
        {
            try
            {
                var response = await GetAsync<ApiResponse<List<Product>>>("api/products");
                return response?.Data ?? new List<Product>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetProductsAsync error: {ex.Message}");
                return new List<Product>();
            }
        }

        // ========== АКТИВНЫЕ ПАРТИИ ==========
        public async Task<List<ActiveBatch>> GetActiveBatchesAsync()
        {
            try
            {
                var response = await GetAsync<ApiResponse<List<ActiveBatch>>>("api/operator/active-batches");
                return response?.Data ?? new List<ActiveBatch>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetActiveBatchesAsync error: {ex.Message}");
                return new List<ActiveBatch>();
            }
        }

        // ========== ПРОГРАММА ПАРТИИ ==========
        public async Task<BatchInfo> GetBatchProgramAsync(string batchNumber)
        {
            try
            {
                var response = await GetAsync<ApiResponse<BatchInfo>>($"api/operator/batch/{batchNumber}/program");
                return response?.Data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetBatchProgramAsync error: {ex.Message}");
                return null;
            }
        }

        // ========== ШАГИ ==========
        public async Task<bool> StartStepAsync(string batchNumber, int stepNumber)
        {
            try
            {
                var token = _httpClient.DefaultRequestHeaders.Authorization?.ToString();
                System.Diagnostics.Debug.WriteLine($"StartStepAsync: Token exists = {!string.IsNullOrEmpty(token)}");
                System.Diagnostics.Debug.WriteLine($"StartStepAsync: batchNumber={batchNumber}, stepNumber={stepNumber}");

                var dto = new { batchNumber = batchNumber, stepNumber = stepNumber };
                var response = await PostAsync<object, ApiResponse<object>>($"api/operator/batch/{batchNumber}/step/{stepNumber}/start", dto);

                System.Diagnostics.Debug.WriteLine($"StartStepAsync response success: {response?.Success}");
                return response != null && response.Success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StartStepAsync error: {ex.Message}");
                return false;
            }
        }

        public string GetToken()
        {
            return _token;
        }

        public void SetToken(string token)
        {
            _token = token;
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            System.Diagnostics.Debug.WriteLine($"SetToken вызван, токен установлен: {(string.IsNullOrEmpty(token) ? "НЕТ" : "ДА")}");
        }

        public async Task<bool> CompleteStepAsync(string batchNumber, int stepNumber, object actualParams)
        {
            try
            {
                var dto = new { batchNumber = batchNumber, stepNumber = stepNumber, actualParams = actualParams };
                var response = await PostAsync<object, ApiResponse<object>>("api/operator/batch/{batchNumber}/step/{stepNumber}/complete", dto);
                return response != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CompleteStepAsync error: {ex.Message}");
                return false;
            }
        }

        // ========== ОТКЛОНЕНИЯ ==========
        public async Task<bool> RegisterDeviationAsync(string batchNumber, int stepNumber, string parameterName,
            string plannedValue, string actualValue, string description, string severity)
        {
            try
            {
                var dto = new
                {
                    batchNumber = batchNumber,
                    stepNumber = stepNumber,
                    parameterName = parameterName,
                    plannedValue = plannedValue,
                    actualValue = actualValue,
                    description = description,
                    severity = severity
                };
                var response = await PostAsync<object, ApiResponse<object>>("api/events", dto);
                return response != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RegisterDeviationAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<List<DeviationEvent>> GetDeviationsAsync(int batchId)
        {
            try
            {
                var response = await GetAsync<ApiResponse<List<DeviationEvent>>>($"api/events?batchId={batchId}");
                return response?.Data ?? new List<DeviationEvent>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetDeviationsAsync error: {ex.Message}");
                return new List<DeviationEvent>();
            }
        }

        // ========== ЭКСТРУДЕР LIVE ==========
        public async Task<List<ExtruderTelemetryItem>> GetExtruderTelemetryAsync(string batchNumber)
        {
            try
            {
                var response = await GetAsync<ApiResponse<List<ExtruderTelemetryItem>>>($"api/extruder/telemetry/{batchNumber}");
                return response?.Data ?? new List<ExtruderTelemetryItem>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetExtruderTelemetryAsync error: {ex.Message}");
                return new List<ExtruderTelemetryItem>();
            }
        }

        // ========== СООБЩИТЬ О ПРОБЛЕМЕ ==========
        public async Task<bool> ReportProblemAsync(string batchNumber, string problemType, string equipment,
            string description, string severity, int userId)
        {
            try
            {
                var dto = new
                {
                    batchNumber = batchNumber,
                    problemType = problemType,
                    equipment = equipment,
                    description = description,
                    severity = severity,
                    userId = userId
                };
                var response = await PostAsync<object, ApiResponse<object>>("api/operator/report-problem", dto);
                return response != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ReportProblemAsync error: {ex.Message}");
                return false;
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

        public async Task<List<Equipment>> GetEquipmentAsync()
        {
            try
            {
                var response = await GetAsync<ApiResponse<List<Equipment>>>("api/equipment");
                return response?.Data ?? new List<Equipment>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetEquipmentAsync error: {ex.Message}");
                return new List<Equipment>();
            }
        }
    }

    // Оставляем только ApiResponse (он не дублируется)

}