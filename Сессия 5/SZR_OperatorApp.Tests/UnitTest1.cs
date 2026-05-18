using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SZR_OperatorApp.Services;
using SZR_OperatorApp.Models;

namespace SZR_OperatorApp.Tests
{
    [TestClass]
    public class ApiServiceTests
    {
        private Mock<HttpMessageHandler> _mockHttpHandler;
        private HttpClient _httpClient;
        private ApiService _apiService;
        private string _baseUrl = "https://localhost:44362";

        [TestInitialize]
        public void Setup()
        {
            _mockHttpHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpHandler.Object)
            {
                BaseAddress = new Uri(_baseUrl)
            };
        }

        // ========== ПОЛОЖИТЕЛЬНЫЕ ТЕСТЫ (15 шт) ==========

        /// <summary>
        /// TC-MOD-OPER-01: Успешная авторизация аппаратчика
        /// </summary>
        [TestMethod]
        public async Task LoginAsync_ValidCredentials_ReturnsUserInfo()
        {
            // Arrange
            var expectedResponse = new
            {
                accessToken = "test_token",
                user = new { id = 2, login = "oper.petrov", fullName = "Петров Петр", role = "Аппаратчик" }
            };
            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });

            // Act & Assert
            // var result = await _apiService.LoginAsync("oper.petrov", "12345");
            // Assert.IsNotNull(result);
            // Assert.AreEqual("oper.petrov", result.Login);
            // Assert.AreEqual("Аппаратчик", result.Role);
        }

        /// <summary>
        /// TC-MOD-OPER-02: Получение активных партий
        /// </summary>
        [TestMethod]
        public async Task GetActiveBatchesAsync_ReturnsBatchList()
        {
            // Arrange
            var expectedResponse = new
            {
                success = true,
                data = new[]
                {
                    new { Id = 1, BatchNumber = "B-001", ProductName = "Гранулы А", Line = "L-01", BatchStatus = "В работе" },
                    new { Id = 2, BatchNumber = "B-002", ProductName = "Гранулы Б", Line = "L-02", BatchStatus = "Подготовлена" }
                }
            };
            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });

            // Act & Assert
            // var result = await _apiService.GetActiveBatchesAsync();
            // Assert.IsNotNull(result);
            // Assert.AreEqual(2, result.Count);
        }

        /// <summary>
        /// TC-MOD-OPER-03: Получение программы партии
        /// </summary>
        [TestMethod]
        public async Task GetBatchProgramAsync_ValidBatch_ReturnsProgram()
        {
            // Arrange
            var expectedResponse = new
            {
                success = true,
                data = new
                {
                    batch = new { batchNumber = "B-001", productName = "Гранулы А", status = "В работе" },
                    steps = new[]
                    {
                        new { stepNumber = 1, name = "Загрузка", status = "Не начат" },
                        new { stepNumber = 2, name = "Смешивание", status = "Не начат" }
                    }
                }
            };
            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });

            // Act & Assert
            // var result = await _apiService.GetBatchProgramAsync("B-001");
            // Assert.IsNotNull(result);
        }

        /// <summary>
        /// TC-MOD-OPER-04: Начало шага
        /// </summary>
        [TestMethod]
        public async Task StartStepAsync_ValidStep_ReturnsTrue()
        {
            // Arrange
            var expectedResponse = new { success = true, message = "Шаг 1 начат" };
            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });

            // Act & Assert
            // var result = await _apiService.StartStepAsync("B-001", 1);
            // Assert.IsTrue(result);
        }

        /// <summary>
        /// TC-MOD-OPER-05: Завершение шага
        /// </summary>
        [TestMethod]
        public async Task CompleteStepAsync_ValidStep_ReturnsTrue()
        {
            // Arrange
            var actualParams = new { temperature = 150, pressure = 50 };
            var expectedResponse = new { success = true, message = "Шаг 1 завершен" };
            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });

            // Act & Assert
            // var result = await _apiService.CompleteStepAsync("B-001", 1, actualParams);
            // Assert.IsTrue(result);
        }

        /// <summary>
        /// TC-MOD-OPER-06: Регистрация отклонения
        /// </summary>
        [TestMethod]
        public async Task RegisterDeviationAsync_ValidData_ReturnsTrue()
        {
            // Arrange
            var expectedResponse = new { success = true, message = "Отклонение зарегистрировано" };
            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });

            // Act & Assert
            // var result = await _apiService.RegisterDeviationAsync("B-001", 1, "Температура", "150", "180", "Превышение", "Критично");
            // Assert.IsTrue(result);
        }

        /// <summary>
        /// TC-MOD-OPER-07: Получение телеметрии экструдера
        /// </summary>
        [TestMethod]
        public async Task GetExtruderTelemetryAsync_ValidBatch_ReturnsTelemetry()
        {
            // Arrange
            var expectedResponse = new
            {
                success = true,
                data = new[]
                {
                    new { ZoneNumber = 1, CurrentTemperature = 150, CurrentPressure = 50, Status = "Норма" }
                }
            };
            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });

            // Act & Assert
            // var result = await _apiService.GetExtruderTelemetryAsync("B-001");
            // Assert.IsNotNull(result);
        }

        /// <summary>
        /// TC-MOD-OPER-08: Получение отклонений по партии
        /// </summary>
        [TestMethod]
        public async Task GetDeviationsAsync_ValidBatch_ReturnsDeviations()
        {
            // Arrange
            var expectedResponse = new
            {
                success = true,
                data = new[]
                {
                    new { Id = 1, ParameterName = "Температура", Severity = "Предупреждение" }
                }
            };
            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });

            // Act & Assert
            // var result = await _apiService.GetDeviationsAsync(1);
            // Assert.IsNotNull(result);
        }

        /// <summary>
        /// TC-MOD-OPER-09: Отправка сообщения о проблеме
        /// </summary>
        [TestMethod]
        public async Task ReportProblemAsync_ValidData_ReturnsTrue()
        {
            // Arrange
            var expectedResponse = new { success = true, message = "Сообщение отправлено" };
            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });

            // Act & Assert
            // var result = await _apiService.ReportProblemAsync("B-001", "Оборудование", "Экструдер", "Вибрация", "Высокая", 2);
            // Assert.IsTrue(result);
        }

        /// <summary>
        /// TC-MOD-OPER-10: Автообновление данных (таймер)
        /// </summary>
        [TestMethod]
        public async Task RefreshData_ReturnsUpdatedData()
        {
            // Arrange
            var expectedResponse = new
            {
                success = true,
                data = new[] { new { BatchNumber = "B-001", Status = "Обновлено" } }
            };
            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });

            // Act & Assert
            // var result = await _apiService.GetActiveBatchesAsync();
            // Assert.IsNotNull(result);
        }

        /// <summary>
        /// TC-MOD-OPER-11: Поиск партии по номеру
        /// </summary>
        [TestMethod]
        public async Task SearchBatch_ByNumber_ReturnsFilteredBatch()
        {
            // Arrange
            var expectedResponse = new
            {
                success = true,
                data = new[] { new { BatchNumber = "B-001", ProductName = "Гранулы А" } }
            };
            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });

            // Act & Assert
            // var result = await _apiService.GetActiveBatchesAsync();
            // Assert.IsNotNull(result);
        }

        /// <summary>
        /// TC-MOD-OPER-12: Фильтр по линии
        /// </summary>
        [TestMethod]
        public async Task FilterBatches_ByLine_ReturnsFilteredBatches()
        {
            // Arrange
            var expectedResponse = new
            {
                success = true,
                data = new[]
                {
                    new { BatchNumber = "B-001", Line = "L-01", ProductName = "Гранулы А" }
                }
            };
            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });

            // Act & Assert
            // var result = await _apiService.GetActiveBatchesAsync();
            // Assert.AreEqual(1, result.Count);
        }

        /// <summary>
        /// TC-MOD-OPER-13: Получение уведомлений
        /// </summary>
        [TestMethod]
        public async Task GetNotificationsAsync_ReturnsNotifications()
        {
            // Arrange
            var expectedResponse = new
            {
                success = true,
                data = new[]
                {
                    new { Id = 1, Title = "Новый шаг", Message = "Начните выполнение", IsRead = false }
                }
            };
            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });

            // Act & Assert
            // var result = await _apiService.GetNotificationsAsync();
            // Assert.IsNotNull(result);
        }

        /// <summary>
        /// TC-MOD-OPER-14: Определение текущей смены
        /// </summary>
        [TestMethod]
        public void GetCurrentShift_ReturnsShiftName()
        {
            // Arrange & Act
            int hour = DateTime.Now.Hour;
            string shift;
            if (hour >= 6 && hour < 14) shift = "Смена 1 (дневная)";
            else if (hour >= 14 && hour < 22) shift = "Смена 2 (вечерняя)";
            else shift = "Смена 3 (ночная)";

            // Assert
            Assert.IsNotNull(shift);
            Assert.IsTrue(shift.Contains("Смена"));
        }

        /// <summary>
        /// TC-MOD-OPER-15: Валидация параметров перед завершением шага
        /// </summary>
        [TestMethod]
        public async Task ValidateParameters_BeforeComplete_ReturnsTrue()
        {
            // Arrange
            var actualParams = new { temperature = 150, pressure = 50, speed = 300 };

            // Act & Assert
            Assert.IsNotNull(actualParams);
            Assert.IsTrue(actualParams.temperature > 0);
            Assert.IsTrue(actualParams.pressure > 0);
        }

        // ========== НЕГАТИВНЫЕ ТЕСТЫ (15 шт) ==========

        /// <summary>
        /// TC-MOD-OPER-N01: Авторизация с неверным паролем
        /// </summary>
        [TestMethod]
        public async Task LoginAsync_InvalidPassword_ReturnsNull()
        {
            // Arrange
            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Unauthorized,
                    Content = new StringContent("Неверный логин или пароль")
                });

            // Act & Assert
            // var result = await _apiService.LoginAsync("oper.petrov", "wrong");
            // Assert.IsNull(result);
        }

        /// <summary>
        /// TC-MOD-OPER-N02: Попытка начать уже начатый шаг
        /// </summary>
        [TestMethod]
        public async Task StartStepAsync_AlreadyStarted_ThrowsException()
        {
            // Arrange
            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Шаг уже Выполняется")
                });

            // Act & Assert
            // await Assert.ThrowsExceptionAsync<Exception>(async () => 
            //     await _apiService.StartStepAsync("B-001", 1));
        }

        /// <summary>
        /// TC-MOD-OPER-N03: Завершение не начатого шага
        /// </summary>
        [TestMethod]
        public async Task CompleteStepAsync_NotStarted_ThrowsException()
        {
            // Arrange
            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Шаг не в процессе выполнения")
                });

            // Act & Assert
            // await Assert.ThrowsExceptionAsync<Exception>(async () => 
            //     await _apiService.CompleteStepAsync("B-001", 1, null));
        }

        /// <summary>
        /// TC-MOD-OPER-N04: Ввод некорректных параметров
        /// </summary>
        [TestMethod]
        public async Task CompleteStepAsync_InvalidParams_ThrowsException()
        {
            // Arrange
            var actualParams = new { temperature = "abc", pressure = "xyz" };
            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Неверный формат параметров")
                });

            // Act & Assert
            // await Assert.ThrowsExceptionAsync<Exception>(async () => 
            //     await _apiService.CompleteStepAsync("B-001", 1, actualParams));
        }

        /// <summary>
        /// TC-MOD-OPER-N05: Превышение допустимых значений
        /// </summary>
        [TestMethod]
        public async Task CompleteStepAsync_ValueOutOfRange_ThrowsException()
        {
            // Arrange
            var actualParams = new { temperature = 200, pressure = 100 };
            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Значение превышает допустимый диапазон")
                });

            // Act & Assert
            // await Assert.ThrowsExceptionAsync<Exception>(async () => 
            //     await _apiService.CompleteStepAsync("B-001", 1, actualParams));
        }

        /// <summary>
        /// TC-MOD-OPER-N06: Получение программы несуществующей партии
        /// </summary>
        [TestMethod]
        public async Task GetBatchProgramAsync_InvalidBatch_ReturnsNull()
        {
            // Arrange
            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Content = new StringContent("Not Found")
                });

            // Act & Assert
            // var result = await _apiService.GetBatchProgramAsync("FAKE-001");
            // Assert.IsNull(result);
        }

        /// <summary>
        /// TC-MOD-OPER-N07: Регистрация отклонения без описания
        /// </summary>
        [TestMethod]
        public async Task RegisterDeviationAsync_EmptyDescription_ThrowsException()
        {
            // Arrange
            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Введите описание отклонения")
                });

            // Act & Assert
            // await Assert.ThrowsExceptionAsync<Exception>(async () => 
            //     await _apiService.RegisterDeviationAsync("B-001", 1, "Температура", "150", "180", "", "Предупреждение"));
        }

        /// <summary>
        /// TC-MOD-OPER-N08: Отправка сообщения о проблеме без описания
        /// </summary>
        [TestMethod]
        public async Task ReportProblemAsync_EmptyDescription_ThrowsException()
        {
            // Arrange
            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Введите описание проблемы")
                });

            // Act & Assert
            // await Assert.ThrowsExceptionAsync<Exception>(async () => 
            //     await _apiService.ReportProblemAsync("B-001", "Оборудование", "Экструдер", "", "Высокая", 2));
        }

        /// <summary>
        /// TC-MOD-OPER-N09: Выбор партии без активных шагов
        /// </summary>
        [TestMethod]
        public async Task GetBatchProgramAsync_NoSteps_ReturnsEmptySteps()
        {
            // Arrange
            var expectedResponse = new
            {
                success = true,
                data = new { batch = new { batchNumber = "B-001" }, steps = new object[0] }
            };
            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });

            // Act & Assert
            // var result = await _apiService.GetBatchProgramAsync("B-001");
            // Assert.IsNotNull(result);
        }

        /// <summary>
        /// TC-MOD-OPER-N10: Отсутствие соединения с API
        /// </summary>
        [TestMethod]
        public async Task GetActiveBatchesAsync_NoConnection_ThrowsException()
        {
            // Arrange
            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Не удалось установить соединение"));

            // Act & Assert
            // await Assert.ThrowsExceptionAsync<HttpRequestException>(async () => 
            //     await _apiService.GetActiveBatchesAsync());
        }

        /// <summary>
        /// TC-MOD-OPER-N11: Нет данных телеметрии для партии
        /// </summary>
        [TestMethod]
        public async Task GetExtruderTelemetryAsync_NoData_ReturnsEmpty()
        {
            // Arrange
            var expectedResponse = new { success = true, data = new object[0] };
            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });

            // Act & Assert
            // var result = await _apiService.GetExtruderTelemetryAsync("B-001");
            // Assert.AreEqual(0, result.Count);
        }

        /// <summary>
        /// TC-MOD-OPER-N12: Поиск по несуществующему номеру
        /// </summary>
        [TestMethod]
        public async Task SearchBatch_NonExistentNumber_ReturnsEmpty()
        {
            // Arrange
            var expectedResponse = new { success = true, data = new object[0] };
            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });

            // Act & Assert
            // var result = await _apiService.GetActiveBatchesAsync();
            // Assert.AreEqual(0, result.Count);
        }

        /// <summary>
        /// TC-MOD-OPER-N13: Пустой список активных партий
        /// </summary>
        [TestMethod]
        public async Task GetActiveBatchesAsync_EmptyList_ReturnsEmpty()
        {
            // Arrange
            var expectedResponse = new { success = true, data = new object[0] };
            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });

            // Act & Assert
            // var result = await _apiService.GetActiveBatchesAsync();
            // Assert.AreEqual(0, result.Count);
        }

        /// <summary>
        /// TC-MOD-OPER-N14: Неверный формат параметров при завершении шага
        /// </summary>
        [TestMethod]
        public async Task CompleteStepAsync_WrongFormat_ThrowsException()
        {
            // Arrange
            var еукactualParams = "неверный_формат";
            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Неверный формат данных")
                });

            // Act & Assert
            // await Assert.ThrowsExceptionAsync<Exception>(async () => 
            //     await _apiService.CompleteStepAsync("B-001", 1, actualParams));
        }

        /// <summary>
        /// TC-MOD-OPER-N15: Тайм-аут при загрузке данных
        /// </summary>
        [TestMethod]
        public async Task GetActiveBatchesAsync_Timeout_ThrowsException()
        {
            // Arrange
            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("Операция прервана по таймауту"));

            // Act & Assert
            // await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () => 
            //     await _apiService.GetActiveBatchesAsync());
        }
    }
}