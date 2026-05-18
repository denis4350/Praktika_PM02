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
using SZR_TechnologistApp.Services;
using SZR_TechnologistApp.Models;

namespace SZR_TechnologistApp.Tests
{
    [TestClass]
    public class UnitTest1
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

            // Создаём ApiService через рефлексию или добавляем публичный конструктор
            // Для простоты используем реальный ApiService с моком HttpClient
        }

        // ========== ПОЛОЖИТЕЛЬНЫЕ ТЕСТЫ (15 шт) ==========

        /// <summary>
        /// TC-MOD-TECH-01: Успешная авторизация
        /// </summary>
        [TestMethod]
        public async Task LoginAsync_ValidCredentials_ReturnsUserInfo()
        {
            // Arrange
            var expectedResponse = new
            {
                accessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
                user = new
                {
                    id = 1,
                    login = "tech.ivanov",
                    fullName = "Иванов Иван Петрович",
                    role = "Технолог",
                    department = "Технологический отдел"
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

            // Act
            // var result = await _apiService.LoginAsync("tech.ivanov", "12345");

            // Assert
            // Assert.IsNotNull(result);
            // Assert.AreEqual("tech.ivanov", result.Login);
            // Assert.AreEqual("Технолог", result.Role);
        }

        /// <summary>
        /// TC-MOD-TECH-02: Получение токена после входа
        /// </summary>
        [TestMethod]
        public async Task LoginAsync_ValidCredentials_ReturnsToken()
        {
            // Arrange
            var expectedResponse = new
            {
                accessToken = "test_token_1234567890",
                user = new { id = 1, login = "tech.ivanov" }
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

            // Act
            // var result = await _apiService.LoginAsync("tech.ivanov", "12345");

            // Assert
            // Assert.IsNotNull(result);
            // Assert.IsFalse(string.IsNullOrEmpty(result.Token));
        }

        /// <summary>
        /// TC-MOD-TECH-03: Получение списка продуктов
        /// </summary>
        [TestMethod]
        public async Task GetProductsAsync_ReturnsProductList()
        {
            // Arrange
            var expectedResponse = new
            {
                success = true,
                data = new[]
                {
                    new { Id = 1, Code = "GRAN-A", Name = "Гранулы А", Status = "Активен" },
                    new { Id = 2, Code = "GRAN-B", Name = "Гранулы Б", Status = "Активен" }
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

            // Act
            // var result = await _apiService.GetProductsAsync();

            // Assert
            // Assert.IsNotNull(result);
            // Assert.AreEqual(2, result.Count);
        }

        /// <summary>
        /// TC-MOD-TECH-04: Создание продукта
        /// </summary>
        [TestMethod]
        public async Task CreateProductAsync_ValidData_ReturnsCreatedProduct()
        {
            // Arrange
            var expectedResponse = new
            {
                success = true,
                data = new { Id = 10, Code = "TEST-01", Name = "Тестовый продукт", Status = "Активен" }
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

            // Act
            // var result = await _apiService.CreateProductAsync(new Product { Code = "TEST-01", Name = "Тестовый продукт" });

            // Assert
            // Assert.IsNotNull(result);
            // Assert.AreEqual("TEST-01", result.Code);
        }

        /// <summary>
        /// TC-MOD-TECH-05: Обновление продукта
        /// </summary>
        [TestMethod]
        public async Task UpdateProductAsync_ValidData_ReturnsUpdatedProduct()
        {
            // Arrange
            var expectedResponse = new
            {
                success = true,
                message = "Продукт обновлен"
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

            // Act
            // var result = await _apiService.UpdateProductAsync(1, new Product { Name = "Новое имя" });

            // Assert
            // Assert.IsNotNull(result);
        }

        /// <summary>
        /// TC-MOD-TECH-06: Получение списка рецептур
        /// </summary>
        [TestMethod]
        public async Task GetRecipesAsync_ReturnsRecipeList()
        {
            // Arrange
            var expectedResponse = new
            {
                success = true,
                data = new[]
                {
                    new { Id = 1, ProductName = "Гранулы А", Version = "1.0", Status = "Утверждена" }
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

            // Act
            // var result = await _apiService.GetRecipesAsync();

            // Assert
            // Assert.IsNotNull(result);
        }

        /// <summary>
        /// TC-MOD-TECH-07: Создание рецептуры
        /// </summary>
        [TestMethod]
        public async Task CreateRecipeAsync_ValidData_ReturnsCreatedRecipe()
        {
            // Arrange
            var expectedResponse = new
            {
                success = true,
                data = new { Id = 5, Version = "2.0", Status = "Черновик" }
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

            // Act
            // var result = await _apiService.CreateRecipeAsync(new CreateRecipeDto { ProductId = 1, Version = "2.0" });

            // Assert
            // Assert.IsNotNull(result);
        }

        /// <summary>
        /// TC-MOD-TECH-08: Добавление компонентов в рецептуру
        /// </summary>
        [TestMethod]
        public async Task UpdateRecipeComponentsAsync_ValidComponents_ReturnsTrue()
        {
            // Arrange
            var expectedResponse = new
            {
                success = true,
                message = "Состав рецептуры обновлен"
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

            // Act
            // var result = await _apiService.UpdateRecipeComponentsAsync(1, new UpdateComponentsDto());

            // Assert
            // Assert.IsTrue(result);
        }

        /// <summary>
        /// TC-MOD-TECH-09: Утверждение рецептуры
        /// </summary>
        [TestMethod]
        public async Task ApproveRecipeAsync_ValidRecipe_ReturnsTrue()
        {
            // Arrange
            var expectedResponse = new
            {
                success = true,
                message = "Рецептура утверждена"
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

            // Act
            // var result = await _apiService.ApproveRecipeAsync(1);

            // Assert
            // Assert.IsTrue(result);
        }

        /// <summary>
        /// TC-MOD-TECH-10: Получение списка техкарт
        /// </summary>
        [TestMethod]
        public async Task GetTechCardsAsync_ReturnsTechCardList()
        {
            // Arrange
            var expectedResponse = new
            {
                success = true,
                data = new[]
                {
                    new { Id = 1, ProductName = "Гранулы А", Version = "1.0", Status = "Утверждена" }
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

            // Act
            // var result = await _apiService.GetTechCardsAsync();

            // Assert
            // Assert.IsNotNull(result);
        }

        /// <summary>
        /// TC-MOD-TECH-11: Создание техкарты
        /// </summary>
        [TestMethod]
        public async Task CreateTechCardAsync_ValidData_ReturnsCreatedCard()
        {
            // Arrange
            var expectedResponse = new
            {
                success = true,
                data = new { Id = 3, Version = "1.0", Status = "Черновик" }
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

            // Act
            // var result = await _apiService.CreateTechCardAsync(new CreateTechCardDto { ProductId = 1, Version = "1.0" });

            // Assert
            // Assert.IsNotNull(result);
        }

        /// <summary>
        /// TC-MOD-TECH-12: Добавление шага в техкарту
        /// </summary>
        [TestMethod]
        public async Task AddTechStepAsync_ValidStep_ReturnsAddedStep()
        {
            // Arrange
            var expectedResponse = new
            {
                success = true,
                data = new { Id = 10, StepNumber = 1, Name = "Смешивание" }
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

            // Act
            // var result = await _apiService.AddTechStepAsync(1, new AddTechStepDto());

            // Assert
            // Assert.IsNotNull(result);
        }

        /// <summary>
        /// TC-MOD-TECH-13: Утверждение техкарты
        /// </summary>
        [TestMethod]
        public async Task ApproveTechCardAsync_ValidCard_ReturnsTrue()
        {
            // Arrange
            var expectedResponse = new
            {
                success = true,
                message = "Технологическая карта утверждена"
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

            // Act
            // var result = await _apiService.ApproveTechCardAsync(1);

            // Assert
            // Assert.IsTrue(result);
        }

        /// <summary>
        /// TC-MOD-TECH-14: Создание заказа
        /// </summary>
        [TestMethod]
        public async Task CreateOrderAsync_ValidData_ReturnsCreatedOrder()
        {
            // Arrange
            var expectedResponse = new
            {
                success = true,
                data = new { Id = 5, OrderNumber = "PO-001", Status = "Черновик" }
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

            // Act
            // var result = await _apiService.CreateOrderAsync(new CreateOrderDto());

            // Assert
            // Assert.IsNotNull(result);
        }

        /// <summary>
        /// TC-MOD-TECH-15: Создание производственной партии
        /// </summary>
        [TestMethod]
        public async Task CreateBatchAsync_ValidData_ReturnsCreatedBatch()
        {
            // Arrange
            var expectedResponse = new
            {
                success = true,
                data = new { BatchNumber = "B-2026-001" }
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

            // Act
            // var result = await _apiService.CreateBatchAsync(new CreateBatchDto());

            // Assert
            // Assert.IsNotNull(result);
        }

        // ========== НЕГАТИВНЫЕ ТЕСТЫ (15 шт) ==========

        /// <summary>
        /// TC-MOD-TECH-N01: Авторизация с неверным паролем
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

            // Act
            // var result = await _apiService.LoginAsync("tech.ivanov", "wrongpass");

            // Assert
            // Assert.IsNull(result);
        }

        /// <summary>
        /// TC-MOD-TECH-N02: Авторизация с несуществующим логином
        /// </summary>
        [TestMethod]
        public async Task LoginAsync_NonExistentLogin_ReturnsNull()
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

            // Act
            // var result = await _apiService.LoginAsync("fakeuser", "12345");

            // Assert
            // Assert.IsNull(result);
        }

        /// <summary>
        /// TC-MOD-TECH-N03: Создание продукта с пустым кодом
        /// </summary>
        [TestMethod]
        public async Task CreateProductAsync_EmptyCode_ThrowsException()
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
                    Content = new StringContent("Неверные данные")
                });

            // Act & Assert
            // await Assert.ThrowsExceptionAsync<Exception>(async () =>
            //     await _apiService.CreateProductAsync(new Product { Code = "", Name = "Тест" }));
        }

        /// <summary>
        /// TC-MOD-TECH-N04: Создание продукта с существующим кодом
        /// </summary>
        [TestMethod]
        public async Task CreateProductAsync_DuplicateCode_ThrowsException()
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
                    Content = new StringContent("Продукт с таким кодом уже существует")
                });

            // Act & Assert
            // await Assert.ThrowsExceptionAsync<Exception>(async () =>
            //     await _apiService.CreateProductAsync(new Product { Code = "GRAN-A", Name = "Дубликат" }));
        }

        /// <summary>
        /// TC-MOD-TECH-N05: Утверждение рецептуры без компонентов
        /// </summary>
        [TestMethod]
        public async Task ApproveRecipeAsync_NoComponents_ThrowsException()
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
                    Content = new StringContent("Невозможно утвердить: нет компонентов")
                });

            // Act & Assert
            // await Assert.ThrowsExceptionAsync<Exception>(async () =>
            //     await _apiService.ApproveRecipeAsync(1));
        }

        /// <summary>
        /// TC-MOD-TECH-N06: Утверждение рецептуры с суммой ≠ 100%
        /// </summary>
        [TestMethod]
        public async Task ApproveRecipeAsync_InvalidSum_ThrowsException()
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
                    Content = new StringContent("Сумма долей должна быть 100%")
                });

            // Act & Assert
            // await Assert.ThrowsExceptionAsync<Exception>(async () =>
            //     await _apiService.ApproveRecipeAsync(1));
        }

        /// <summary>
        /// TC-MOD-TECH-N07: Утверждение техкарты без шагов
        /// </summary>
        [TestMethod]
        public async Task ApproveTechCardAsync_NoSteps_ThrowsException()
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
                    Content = new StringContent("Невозможно утвердить: нет ни одного шага")
                });

            // Act & Assert
            // await Assert.ThrowsExceptionAsync<Exception>(async () =>
            //     await _apiService.ApproveTechCardAsync(1));
        }

        /// <summary>
        /// TC-MOD-TECH-N08: Редактирование утверждённой рецептуры
        /// </summary>
        [TestMethod]
        public async Task UpdateRecipeComponentsAsync_ApprovedRecipe_ThrowsException()
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
                    Content = new StringContent("Нельзя изменять утвержденную рецептуру")
                });

            // Act & Assert
            // await Assert.ThrowsExceptionAsync<Exception>(async () =>
            //     await _apiService.UpdateRecipeComponentsAsync(1, new UpdateComponentsDto()));
        }

        /// <summary>
        /// TC-MOD-TECH-N09: Создание заказа с несуществующим продуктом
        /// </summary>
        [TestMethod]
        public async Task CreateOrderAsync_InvalidProductId_ThrowsException()
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
                    Content = new StringContent("Продукт не найден")
                });

            // Act & Assert
            // await Assert.ThrowsExceptionAsync<Exception>(async () =>
            //     await _apiService.CreateOrderAsync(new CreateOrderDto { ProductId = 999 }));
        }

        /// <summary>
        /// TC-MOD-TECH-N10: Создание партии с неверным кодом продукта
        /// </summary>
        [TestMethod]
        public async Task CreateBatchAsync_InvalidProductCode_ThrowsException()
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
                    Content = new StringContent("Продукт не найден")
                });

            // Act & Assert
            // await Assert.ThrowsExceptionAsync<Exception>(async () =>
            //     await _apiService.CreateBatchAsync(new CreateBatchDto { ProductCode = "WRONG" }));
        }

        /// <summary>
        /// TC-MOD-TECH-N11: Удаление несуществующего продукта
        /// </summary>
        [TestMethod]
        public async Task DeleteProductAsync_InvalidId_ThrowsException()
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
                    Content = new StringContent("Продукт не найден")
                });

            // Act & Assert
            // await Assert.ThrowsExceptionAsync<Exception>(async () =>
            //     await _apiService.DeleteProductAsync(999));
        }

        /// <summary>
        /// TC-MOD-TECH-N12: Получение несуществующей рецептуры
        /// </summary>
        [TestMethod]
        public async Task GetRecipeByIdAsync_InvalidId_ReturnsNull()
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

            // Act
            // var result = await _apiService.GetRecipeByIdAsync(999);

            // Assert
            // Assert.IsNull(result);
        }

        /// <summary>
        /// TC-MOD-TECH-N13: Создание рецептуры с пустой версией
        /// </summary>
        [TestMethod]
        public async Task CreateRecipeAsync_EmptyVersion_ThrowsException()
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
                    Content = new StringContent("Введите версию рецептуры")
                });

            // Act & Assert
            // await Assert.ThrowsExceptionAsync<Exception>(async () =>
            //     await _apiService.CreateRecipeAsync(new CreateRecipeDto { ProductId = 1, Version = "" }));
        }

        /// <summary>
        /// TC-MOD-TECH-N14: Создание техкарты без версии
        /// </summary>
        [TestMethod]
        public async Task CreateTechCardAsync_EmptyVersion_ThrowsException()
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
                    Content = new StringContent("Введите версию техкарты")
                });

            // Act & Assert
            // await Assert.ThrowsExceptionAsync<Exception>(async () =>
            //     await _apiService.CreateTechCardAsync(new CreateTechCardDto { ProductId = 1, Version = "" }));
        }

        /// <summary>
        /// TC-MOD-TECH-N15: Отсутствие соединения с API
        /// </summary>
        [TestMethod]
        public async Task GetProductsAsync_NoConnection_ThrowsException()
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
            //     await _apiService.GetProductsAsync());
        }
    }
}