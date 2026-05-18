using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using SZR_Production_API.Models;

namespace SZR_Production_API.Controllers
{
    [Authorize(Roles = "Лаборант,Технолог,Администратор")]
    [RoutePrefix("api/laboratory")]
    public class LaboratoryController : ApiController
    {
        private readonly SZR_ProductionEntities2 _context;

        public LaboratoryController()
        {
            _context = new SZR_ProductionEntities2();
        }

        // GET: api/laboratory/raw-material-batches?status=Ожидает&page=1&pageSize=20
        [HttpGet]
        [Route("raw-material-batches")]
        public async Task<IHttpActionResult> GetRawMaterialBatches(
            string status = null,
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var validation = ValidatePagination(page, pageSize);
                if (validation != null)
                    return validation;

                var query = _context.RawMaterialBatches.AsQueryable();

                if (!string.IsNullOrWhiteSpace(status))
                {
                    string normalizedStatus = status.Trim();
                    query = query.Where(r => r.LabStatus == normalizedStatus);
                }

                int totalCount = await query.CountAsync();

                var batches = await query
                    .OrderByDescending(r => r.ArrivalDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        id = r.Id,
                        batchNumber = r.BatchNumber,
                        supplierBatch = r.SupplierBatch,
                        materialId = r.MaterialId,
                        materialName = r.RawMaterials != null ? r.RawMaterials.Name : "",
                        materialCategory = r.RawMaterials != null ? r.RawMaterials.Category : "",
                        supplier = r.Supplier,
                        quantity = r.Quantity,
                        unit = r.Unit,
                        arrivalDate = r.ArrivalDate,
                        labStatus = r.LabStatus,
                        decisionAt = r.DecisionAt,
                        decisionBy = r.DecisionBy,
                        hasTest = _context.LabTests.Any(t =>
                            t.ObjectId == r.Id &&
                            t.ObjectType == "RawMaterial"),
                        hasOpenTest = _context.LabTests.Any(t =>
                            t.ObjectId == r.Id &&
                            t.ObjectType == "RawMaterial" &&
                            t.Status != "Завершено" &&
                            t.Status != "Отменено"),
                        lastTestDate = _context.LabTests
                            .Where(t => t.ObjectId == r.Id && t.ObjectType == "RawMaterial")
                            .OrderByDescending(t => t.AssignedAt)
                            .Select(t => (DateTime?)t.AssignedAt)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = batches,
                    pagination = BuildPagination(page, pageSize, totalCount),
                    message = "Список партий сырья получен"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения партий сырья: " + ex.Message);
            }
        }

        // GET: api/laboratory/product-batches?status=Ожидает&page=1&pageSize=20
        [HttpGet]
        [Route("product-batches")]
        public async Task<IHttpActionResult> GetProductBatches(
            string status = null,
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var validation = ValidatePagination(page, pageSize);
                if (validation != null)
                    return validation;

                var query = _context.ProductionBatches.AsQueryable();

                if (!string.IsNullOrWhiteSpace(status))
                {
                    string normalizedStatus = status.Trim();
                    query = query.Where(b => b.LabStatus == normalizedStatus);
                }

                int totalCount = await query.CountAsync();

                var batches = await query
                    .OrderByDescending(b => b.FinishedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => new
                    {
                        id = b.Id,
                        batchNumber = b.BatchNumber,
                        productId = b.ProductId,
                        productName = b.Products != null ? b.Products.Name : "",
                        line = b.Line,
                        status = b.Status,
                        labStatus = b.LabStatus,
                        startedAt = b.StartedAt,
                        finishedAt = b.FinishedAt,
                        hasTest = _context.LabTests.Any(t =>
                            t.ObjectId == b.Id &&
                            t.ObjectType == "Product"),
                        hasOpenTest = _context.LabTests.Any(t =>
                            t.ObjectId == b.Id &&
                            t.ObjectType == "Product" &&
                            t.Status != "Завершено" &&
                            t.Status != "Отменено")
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = batches,
                    pagination = BuildPagination(page, pageSize, totalCount),
                    message = "Список партий готовой продукции получен"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения партий продукции: " + ex.Message);
            }
        }

        // GET: api/laboratory/raw-material-batches/5
        [HttpGet]
        [Route("raw-material-batches/{id:int}")]
        public async Task<IHttpActionResult> GetRawMaterialBatchCard(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор партии сырья");

                var batch = await _context.RawMaterialBatches
                    .Where(r => r.Id == id)
                    .Select(r => new
                    {
                        id = r.Id,
                        batchNumber = r.BatchNumber,
                        supplierBatch = r.SupplierBatch,
                        materialId = r.MaterialId,
                        materialName = r.RawMaterials != null ? r.RawMaterials.Name : "",
                        materialCategory = r.RawMaterials != null ? r.RawMaterials.Category : "",
                        supplier = r.Supplier,
                        quantity = r.Quantity,
                        unit = r.Unit,
                        arrivalDate = r.ArrivalDate,
                        labStatus = r.LabStatus,
                        decisionAt = r.DecisionAt,
                        decisionBy = r.DecisionBy,
                        decisionComment = r.Comment
                    })
                    .FirstOrDefaultAsync();

                if (batch == null)
                    return NotFoundMessage("Партия сырья не найдена");

                var tests = await _context.LabTests
                    .Where(t => t.ObjectId == id && t.ObjectType == "RawMaterial")
                    .OrderByDescending(t => t.AssignedAt)
                    .Select(t => new
                    {
                        id = t.Id,
                        testNumber = t.TestNumber,
                        testType = t.TestType,
                        assignedAt = t.AssignedAt,
                        assignedBy = t.AssignedBy,
                        executedBy = t.ExecutedBy,
                        executedAt = t.ExecutedAt,
                        status = t.Status,
                        priority = t.Priority,
                        comment = t.Comment
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        batch,
                        tests
                    },
                    message = "Карточка партии сырья получена"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения карточки партии сырья: " + ex.Message);
            }
        }

        // POST: api/laboratory/tests
        [HttpPost]
        [Route("tests")]
        public async Task<IHttpActionResult> CreateTest([FromBody] CreateTestDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequestMessage("Тело запроса пустое");

                if (dto.ObjectId <= 0)
                    return BadRequestMessage("Некорректный идентификатор объекта контроля");

                string objectType = NormalizeObjectType(dto.ObjectType);
                if (objectType == null)
                    return BadRequestMessage("ObjectType должен быть RawMaterial или Product");

                if (string.IsNullOrWhiteSpace(dto.TestType))
                    return BadRequestMessage("Тип испытания обязателен");

                if (dto.Parameters == null || dto.Parameters.Length == 0)
                    return BadRequestMessage("Необходимо указать хотя бы один контролируемый параметр");

                foreach (var p in dto.Parameters)
                {
                    if (p == null || string.IsNullOrWhiteSpace(p.ParameterName))
                        return BadRequestMessage("У каждого параметра должно быть наименование");

                    if (p.NormMin.HasValue && p.NormMax.HasValue && p.NormMin.Value > p.NormMax.Value)
                        return BadRequestMessage("Минимальная норма параметра не может быть больше максимальной");
                }

                bool objectExists = await ObjectExists(objectType, dto.ObjectId);
                if (!objectExists)
                    return NotFoundMessage("Объект контроля не найден");

                bool hasOpenTest = await _context.LabTests.AnyAsync(t =>
                    t.ObjectId == dto.ObjectId &&
                    t.ObjectType == objectType &&
                    t.Status != "Завершено" &&
                    t.Status != "Отменено");

                if (hasOpenTest)
                    return BadRequestMessage("Для этого объекта уже существует незавершённое испытание");

                int userId = GetCurrentUserId();

                var test = new LabTests
                {
                    TestNumber = GenerateTestNumber(),
                    TestType = dto.TestType.Trim(),
                    ObjectType = objectType,
                    ObjectId = dto.ObjectId,
                    AssignedAt = DateTime.Now,
                    AssignedBy = userId,
                    Status = "Создано",
                    Priority = string.IsNullOrWhiteSpace(dto.Priority) ? "Обычный" : dto.Priority.Trim(),
                    Comment = dto.Comment
                };

                _context.LabTests.Add(test);
                await _context.SaveChangesAsync();

                foreach (var param in dto.Parameters)
                {
                    _context.LabTestParameters.Add(new LabTestParameters
                    {
                        TestId = test.Id,
                        ParameterName = param.ParameterName.Trim(),
                        NormMin = param.NormMin,
                        NormMax = param.NormMax,
                        Unit = string.IsNullOrWhiteSpace(param.Unit) ? null : param.Unit.Trim(),
                        ActualValue = null,
                        IsPassed = null
                    });
                }

                if (objectType == "RawMaterial")
                {
                    var batch = await _context.RawMaterialBatches.FindAsync(dto.ObjectId);
                    if (batch != null && batch.LabStatus == "Ожидает")
                        batch.LabStatus = "В работе";
                }
                else
                {
                    var batch = await _context.ProductionBatches.FindAsync(dto.ObjectId);
                    if (batch != null && string.IsNullOrWhiteSpace(batch.LabStatus))
                        batch.LabStatus = "В работе";
                }

                await _context.SaveChangesAsync();

                AddAudit(
                    userId,
                    "Создание лабораторного испытания",
                    objectType == "RawMaterial" ? "RawMaterialBatch" : "ProductionBatch",
                    dto.ObjectId,
                    null,
                    test.TestNumber
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = test.Id,
                        testNumber = test.TestNumber,
                        objectType = test.ObjectType,
                        objectId = test.ObjectId,
                        status = test.Status,
                        priority = test.Priority
                    },
                    message = "Испытание создано"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка создания испытания: " + ex.Message);
            }
        }

        // GET: api/laboratory/tests?objectId=1&objectType=RawMaterial
        [HttpGet]
        [Route("tests")]
        public async Task<IHttpActionResult> GetTests(
            int? objectId = null,
            string objectType = null,
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var validation = ValidatePagination(page, pageSize);
                if (validation != null)
                    return validation;

                var query = _context.LabTests.AsQueryable();

                if (objectId.HasValue)
                    query = query.Where(t => t.ObjectId == objectId.Value);

                if (!string.IsNullOrWhiteSpace(objectType))
                {
                    string normalizedType = NormalizeObjectType(objectType);
                    if (normalizedType == null)
                        return BadRequestMessage("ObjectType должен быть RawMaterial или Product");

                    query = query.Where(t => t.ObjectType == normalizedType);
                }

                int totalCount = await query.CountAsync();

                var tests = await query
                    .OrderByDescending(t => t.AssignedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new
                    {
                        id = t.Id,
                        testNumber = t.TestNumber,
                        testType = t.TestType,
                        objectType = t.ObjectType,
                        objectId = t.ObjectId,
                        assignedAt = t.AssignedAt,
                        assignedBy = t.AssignedBy,
                        executedBy = t.ExecutedBy,
                        executedAt = t.ExecutedAt,
                        status = t.Status,
                        priority = t.Priority,
                        comment = t.Comment
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = tests,
                    pagination = BuildPagination(page, pageSize, totalCount),
                    message = "Список испытаний получен"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения испытаний: " + ex.Message);
            }
        }

        // GET: api/laboratory/tests/5/parameters
        [HttpGet]
        [Route("tests/{testId:int}/parameters")]
        public async Task<IHttpActionResult> GetTestParameters(int testId)
        {
            try
            {
                if (testId <= 0)
                    return BadRequestMessage("Некорректный идентификатор испытания");

                bool testExists = await _context.LabTests.AnyAsync(t => t.Id == testId);
                if (!testExists)
                    return NotFoundMessage("Испытание не найдено");

                var parameters = await _context.LabTestParameters
                    .Where(p => p.TestId == testId)
                    .OrderBy(p => p.Id)
                    .Select(p => new
                    {
                        id = p.Id,
                        testId = p.TestId,
                        parameterName = p.ParameterName,
                        normMin = p.NormMin,
                        normMax = p.NormMax,
                        actualValue = p.ActualValue,
                        unit = p.Unit,
                        isPassed = p.IsPassed
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = parameters,
                    message = "Параметры испытания получены"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения параметров испытания: " + ex.Message);
            }
        }

        // PUT: api/laboratory/tests/5/results
        [HttpPut]
        [Route("tests/{id:int}/results")]
        public async Task<IHttpActionResult> UpdateTestResults(int id, [FromBody] UpdateTestResultsDto dto)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор испытания");

                if (dto == null || dto.Results == null || dto.Results.Length == 0)
                    return BadRequestMessage("Не указаны результаты испытания");

                var test = await _context.LabTests.FindAsync(id);
                if (test == null)
                    return NotFoundMessage("Испытание не найдено");

                if (test.Status == "Завершено")
                    return BadRequestMessage("Нельзя изменить результаты завершённого испытания");

                var parameters = await _context.LabTestParameters
                    .Where(p => p.TestId == id)
                    .ToListAsync();

                if (!parameters.Any())
                    return BadRequestMessage("У испытания отсутствуют контролируемые параметры");

                foreach (var result in dto.Results)
                {
                    if (result == null || result.ParameterId <= 0)
                        return BadRequestMessage("Некорректный параметр результата");

                    var param = parameters.FirstOrDefault(p => p.Id == result.ParameterId);

                    if (param == null)
                        return BadRequestMessage("Параметр с ID " + result.ParameterId + " не относится к этому испытанию");

                    if (!result.ActualValue.HasValue)
                        return BadRequestMessage("Фактическое значение параметра обязательно");

                    param.ActualValue = result.ActualValue;
                    param.IsPassed = CheckIfPassed(param.NormMin, param.NormMax, result.ActualValue);
                }

                if (test.Status == "Создано")
                    test.Status = "В работе";

                await _context.SaveChangesAsync();

                AddAudit(
                    GetCurrentUserId(),
                    "Сохранение результатов лабораторного испытания",
                    "LabTest",
                    test.Id,
                    null,
                    "Результаты обновлены"
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        testId = test.Id,
                        status = test.Status
                    },
                    message = "Результаты сохранены"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка сохранения результатов: " + ex.Message);
            }
        }

        // POST: api/laboratory/tests/5/complete
        [HttpPost]
        [Route("tests/{id:int}/complete")]
        public async Task<IHttpActionResult> CompleteTest(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор испытания");

                var test = await _context.LabTests
                    .Include(t => t.LabTestParameters)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (test == null)
                    return NotFoundMessage("Испытание не найдено");

                if (test.Status == "Завершено")
                    return BadRequestMessage("Испытание уже завершено");

                if (test.LabTestParameters == null || !test.LabTestParameters.Any())
                    return BadRequestMessage("Нельзя завершить испытание без контролируемых параметров");

                if (test.LabTestParameters.Any(p => !p.ActualValue.HasValue))
                    return BadRequestMessage("Нельзя завершить испытание: заполнены не все фактические значения");

                foreach (var param in test.LabTestParameters)
                {
                    param.IsPassed = CheckIfPassed(param.NormMin, param.NormMax, param.ActualValue);
                }

                bool allPassed = test.LabTestParameters.All(p => p.IsPassed == true);
                bool anyFailed = test.LabTestParameters.Any(p => p.IsPassed == false);

                string result = allPassed
                    ? "Соответствует"
                    : anyFailed ? "Не соответствует" : "Требуется проверка";

                int userId = GetCurrentUserId();

                test.Status = "Завершено";
                test.ExecutedAt = DateTime.Now;
                test.ExecutedBy = userId;

                AddAudit(
                    userId,
                    "Завершение лабораторного испытания",
                    "LabTest",
                    test.Id,
                    "В работе",
                    "Завершено"
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        testId = test.Id,
                        testNumber = test.TestNumber,
                        status = test.Status,
                        result
                    },
                    message = "Испытание завершено"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка завершения испытания: " + ex.Message);
            }
        }

        // POST: api/laboratory/decisions/raw-material
        [HttpPost]
        [Route("decisions/raw-material")]
        public async Task<IHttpActionResult> DecideRawMaterialBatch([FromBody] RawMaterialDecisionDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequestMessage("Тело запроса пустое");

                if (dto.BatchId <= 0)
                    return BadRequestMessage("Не указан корректный ID партии сырья");

                string decision = NormalizeDecision(dto.Decision);
                if (decision == null)
                    return BadRequestMessage("Решение должно быть Разрешена или Заблокирована");

                if (decision == "Заблокирована" && string.IsNullOrWhiteSpace(dto.Comment))
                    return BadRequestMessage("При блокировке партии необходимо указать причину");

                var batch = await _context.RawMaterialBatches.FindAsync(dto.BatchId);
                if (batch == null)
                    return NotFoundMessage("Партия сырья не найдена");

                bool canDecide = await HasCompletedTestWithAllResults("RawMaterial", batch.Id);
                if (!canDecide)
                {
                    return BadRequestMessage(
                        "Невозможно принять решение: нет завершённого испытания со всеми заполненными результатами"
                    );
                }

                int userId = GetCurrentUserId();
                string oldStatus = batch.LabStatus;

                batch.LabStatus = decision;
                batch.DecisionAt = DateTime.Now;
                batch.DecisionBy = userId;
                batch.Comment = dto.Comment;

                AddAudit(
                    userId,
                    "Принятие лабораторного решения по сырью",
                    "RawMaterialBatch",
                    batch.Id,
                    oldStatus,
                    decision
                );

                if (decision == "Заблокирована")
                {
                    await CreateNotification(
                        "Партия сырья " + batch.BatchNumber + " заблокирована",
                        dto.Comment ?? "Причина не указана"
                    );
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        batchId = batch.Id,
                        batchNumber = batch.BatchNumber,
                        oldStatus,
                        newStatus = batch.LabStatus,
                        decisionAt = batch.DecisionAt
                    },
                    message = "Решение по партии сырья принято"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка принятия решения по сырью: " + ex.Message);
            }
        }

        // POST: api/laboratory/decisions/product
        [HttpPost]
        [Route("decisions/product")]
        public async Task<IHttpActionResult> DecideProductBatch([FromBody] ProductDecisionDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequestMessage("Тело запроса пустое");

                if (dto.BatchId <= 0)
                    return BadRequestMessage("Не указан корректный ID партии продукции");

                string decision = NormalizeDecision(dto.Decision);
                if (decision == null)
                    return BadRequestMessage("Решение должно быть Разрешена или Заблокирована");

                if (decision == "Заблокирована" && string.IsNullOrWhiteSpace(dto.Comment))
                    return BadRequestMessage("При блокировке партии необходимо указать причину");

                var batch = await _context.ProductionBatches.FindAsync(dto.BatchId);
                if (batch == null)
                    return NotFoundMessage("Партия продукции не найдена");

                bool canDecide = await HasCompletedTestWithAllResults("Product", batch.Id);
                if (!canDecide)
                {
                    return BadRequestMessage(
                        "Невозможно принять решение: нет завершённого испытания со всеми заполненными результатами"
                    );
                }

                int userId = GetCurrentUserId();
                string oldLabStatus = batch.LabStatus;
                string oldBatchStatus = batch.Status;

                batch.LabStatus = decision;

                if (decision == "Разрешена")
                    batch.Status = "Завершена";
                else if (decision == "Заблокирована")
                    batch.Status = "Заблокирована";

                AddAudit(
                    userId,
                    "Принятие лабораторного решения по продукции",
                    "ProductionBatch",
                    batch.Id,
                    oldLabStatus + " / " + oldBatchStatus,
                    batch.LabStatus + " / " + batch.Status
                );

                if (decision == "Заблокирована")
                {
                    await CreateNotification(
                        "Партия продукции " + batch.BatchNumber + " заблокирована",
                        string.IsNullOrWhiteSpace(dto.Comment)
                            ? "Требуется вмешательство технолога"
                            : dto.Comment
                    );
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        batchId = batch.Id,
                        batchNumber = batch.BatchNumber,
                        labStatus = batch.LabStatus,
                        status = batch.Status
                    },
                    message = "Решение по партии продукции принято"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка принятия решения по продукции: " + ex.Message);
            }
        }

        // GET: api/laboratory/tests/archive?page=1&pageSize=20
        [HttpGet]
        [Route("tests/archive")]
        public async Task<IHttpActionResult> GetTestArchive(int page = 1, int pageSize = 20)
        {
            try
            {
                var validation = ValidatePagination(page, pageSize);
                if (validation != null)
                    return validation;

                var query = _context.LabTests
                    .Where(t => t.Status == "Завершено")
                    .OrderByDescending(t => t.ExecutedAt);

                int totalCount = await query.CountAsync();

                var testsRaw = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new
                    {
                        id = t.Id,
                        testNumber = t.TestNumber,
                        testType = t.TestType,
                        objectType = t.ObjectType,
                        objectId = t.ObjectId,
                        assignedAt = t.AssignedAt,
                        assignedBy = t.AssignedBy,
                        executedBy = t.ExecutedBy,
                        executedAt = t.ExecutedAt,
                        status = t.Status,
                        priority = t.Priority,
                        comment = t.Comment
                    })
                    .ToListAsync();

                var rawBatchIds = testsRaw
                    .Where(t => t.objectType == "RawMaterial")
                    .Select(t => t.objectId)
                    .Distinct()
                    .ToList();

                var productBatchIds = testsRaw
                    .Where(t => t.objectType == "Product")
                    .Select(t => t.objectId)
                    .Distinct()
                    .ToList();

                var rawBatchNames = await _context.RawMaterialBatches
                    .Where(r => rawBatchIds.Contains(r.Id))
                    .Select(r => new
                    {
                        r.Id,
                        ObjectName = r.BatchNumber + " / " + r.RawMaterials.Name
                    })
                    .ToDictionaryAsync(x => x.Id, x => x.ObjectName);

                var productBatchNames = await _context.ProductionBatches
                    .Where(b => productBatchIds.Contains(b.Id))
                    .Select(b => new
                    {
                        b.Id,
                        ObjectName = b.BatchNumber + " / " + b.Products.Name
                    })
                    .ToDictionaryAsync(x => x.Id, x => x.ObjectName);

                var tests = testsRaw.Select(t => new
                {
                    t.id,
                    t.testNumber,
                    t.testType,
                    t.objectType,
                    t.objectId,
                    objectName = t.objectType == "RawMaterial"
                        ? (rawBatchNames.ContainsKey(t.objectId) ? rawBatchNames[t.objectId] : "")
                        : (productBatchNames.ContainsKey(t.objectId) ? productBatchNames[t.objectId] : ""),
                    t.assignedAt,
                    t.assignedBy,
                    t.executedBy,
                    t.executedAt,
                    t.status,
                    t.priority,
                    t.comment
                }).ToList();

                return Ok(new
                {
                    success = true,
                    data = tests,
                    pagination = BuildPagination(page, pageSize, totalCount),
                    message = "Архив лабораторных испытаний получен"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения архива: " + ex.Message);
            }
        }

        // GET: api/laboratory/decision-info?batchId=1&batchType=RawMaterial
        [HttpGet]
        [Route("decision-info")]
        public async Task<IHttpActionResult> GetDecisionInfo(int batchId, string batchType)
        {
            try
            {
                if (batchId <= 0)
                    return BadRequestMessage("Некорректный идентификатор партии");

                string normalizedType = NormalizeObjectType(batchType);
                if (normalizedType == null)
                    return BadRequestMessage("batchType должен быть RawMaterial или Product");

                if (normalizedType == "RawMaterial")
                {
                    var batch = await _context.RawMaterialBatches
                        .FirstOrDefaultAsync(b => b.Id == batchId);

                    if (batch == null)
                        return NotFoundMessage("Партия сырья не найдена");

                    string userName = "Неизвестно";
                    if (batch.DecisionBy.HasValue)
                    {
                        var user = await _context.Users.FindAsync(batch.DecisionBy.Value);
                        userName = user != null ? user.FullName : "Неизвестно";
                    }

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            batchId = batch.Id,
                            batchNumber = batch.BatchNumber,
                            labStatus = batch.LabStatus,
                            decisionBy = userName,
                            decisionAt = batch.DecisionAt,
                            comment = batch.Comment
                        },
                        message = "Информация о решении получена"
                    });
                }
                else
                {
                    var batch = await _context.ProductionBatches
                        .FirstOrDefaultAsync(b => b.Id == batchId);

                    if (batch == null)
                        return NotFoundMessage("Партия продукции не найдена");

                    var lastAudit = await _context.AuditLogs
                        .Where(a => a.EntityType == "ProductionBatch" &&
                                    a.EntityId == batch.Id &&
                                    a.Action.Contains("лабораторного решения"))
                        .OrderByDescending(a => a.CreatedAt)
                        .FirstOrDefaultAsync();

                    string userName = null;
                    if (lastAudit != null)
                    {
                        var user = await _context.Users.FindAsync(lastAudit.UserId);
                        userName = user != null ? user.FullName : "Неизвестно";
                    }

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            batchId = batch.Id,
                            batchNumber = batch.BatchNumber,
                            labStatus = batch.LabStatus,
                            status = batch.Status,
                            decisionBy = userName,
                            decisionAt = lastAudit != null ? (DateTime?)lastAudit.CreatedAt : null,
                            comment = lastAudit != null ? lastAudit.NewValue : null
                        },
                        message = "Информация о решении получена"
                    });
                }
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения информации о решении: " + ex.Message);
            }
        }

        private bool CheckIfPassed(decimal? normMin, decimal? normMax, decimal? actualValue)
        {
            if (!actualValue.HasValue)
                return false;

            if (normMin.HasValue && actualValue.Value < normMin.Value)
                return false;

            if (normMax.HasValue && actualValue.Value > normMax.Value)
                return false;

            return true;
        }

        private string GenerateTestNumber()
        {
            return "TST-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
        }

        private int GetCurrentUserId()
        {
            var identity = User?.Identity as ClaimsIdentity;

            if (identity == null || !identity.IsAuthenticated)
                throw new UnauthorizedAccessException("Пользователь не авторизован");

            var userIdClaim = identity.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                throw new UnauthorizedAccessException("Не удалось определить ID пользователя");

            return userId;
        }

        private string GetClientIp()
        {
            if (Request != null && Request.Properties.ContainsKey("MS_HttpContext"))
            {
                var context = Request.Properties["MS_HttpContext"] as System.Web.HttpContextWrapper;
                return context?.Request?.UserHostAddress ?? "0.0.0.0";
            }

            return "0.0.0.0";
        }

        private async Task CreateNotification(string title, string message)
        {
            var technologists = await _context.Users
                .Where(u => u.Roles != null && u.Roles.Name == "Технолог" && u.IsActive)
                .ToListAsync();

            foreach (var tech in technologists)
            {
                _context.Notifications.Add(new Notifications
                {
                    UserId = tech.Id,
                    Type = "Лаборатория",
                    Title = title,
                    Message = message,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }
        }

        private void AddAudit(
            int userId,
            string action,
            string entityType,
            int entityId,
            string oldValue,
            string newValue)
        {
            _context.AuditLogs.Add(new AuditLogs
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValue = oldValue,
                NewValue = newValue,
                CreatedAt = DateTime.Now,
                IpAddress = GetClientIp()
            });
        }

        private async Task<bool> ObjectExists(string objectType, int objectId)
        {
            if (objectType == "RawMaterial")
                return await _context.RawMaterialBatches.AnyAsync(r => r.Id == objectId);

            if (objectType == "Product")
                return await _context.ProductionBatches.AnyAsync(b => b.Id == objectId);

            return false;
        }

        private async Task<bool> HasCompletedTestWithAllResults(string objectType, int objectId)
        {
            var tests = await _context.LabTests
                .Where(t => t.ObjectType == objectType &&
                            t.ObjectId == objectId &&
                            t.Status == "Завершено")
                .Select(t => t.Id)
                .ToListAsync();

            if (!tests.Any())
                return false;

            foreach (int testId in tests)
            {
                var parameters = await _context.LabTestParameters
                    .Where(p => p.TestId == testId)
                    .ToListAsync();

                if (parameters.Any() && parameters.All(p => p.ActualValue.HasValue))
                    return true;
            }

            return false;
        }

        private string NormalizeObjectType(string objectType)
        {
            if (string.IsNullOrWhiteSpace(objectType))
                return null;

            string value = objectType.Trim().ToLowerInvariant();

            if (value == "rawmaterial" || value == "raw_material" || value == "сырье" || value == "сырьё")
                return "RawMaterial";

            if (value == "product" || value == "productionbatch" || value == "продукция")
                return "Product";

            return null;
        }

        private string NormalizeDecision(string decision)
        {
            if (string.IsNullOrWhiteSpace(decision))
                return null;

            string value = decision.Trim().ToLowerInvariant();

            if (value == "разрешена" || value == "разрешить" || value == "approved" || value == "allow")
                return "Разрешена";

            if (value == "заблокирована" || value == "заблокировать" || value == "blocked" || value == "block")
                return "Заблокирована";

            return null;
        }

        private IHttpActionResult ValidatePagination(int page, int pageSize)
        {
            if (page < 1)
                return BadRequestMessage("Номер страницы должен быть больше 0");

            if (pageSize < 1 || pageSize > 100)
                return BadRequestMessage("Размер страницы должен быть от 1 до 100");

            return null;
        }

        private object BuildPagination(int page, int pageSize, int totalCount)
        {
            return new
            {
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }

        private IHttpActionResult BadRequestMessage(string message)
        {
            return Content(HttpStatusCode.BadRequest, new { success = false, message });
        }

        private IHttpActionResult NotFoundMessage(string message)
        {
            return Content(HttpStatusCode.BadRequest, ApiResponse<object>.Fail(message));
        }

        private IHttpActionResult ServerError(string message)
        {
            return Content(HttpStatusCode.BadRequest, ApiResponse<object>.Fail(message));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _context.Dispose();

            base.Dispose(disposing);
        }
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
        public TestResultDto[] Results { get; set; }
    }

    public class TestResultDto
    {
        public int ParameterId { get; set; }
        public decimal? ActualValue { get; set; }
    }

    public class RawMaterialDecisionDto
    {
        public int BatchId { get; set; }
        public string Decision { get; set; }
        public string Comment { get; set; }
    }

    public class ProductDecisionDto
    {
        public int BatchId { get; set; }
        public string Decision { get; set; }
        public string Comment { get; set; }
    }
}