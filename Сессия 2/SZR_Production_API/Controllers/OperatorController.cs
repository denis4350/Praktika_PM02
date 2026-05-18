using Newtonsoft.Json;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using SZR_Production_API.Models;

namespace SZR_Production_API.Controllers
{
    [Authorize(Roles = "Аппаратчик,Технолог,Администратор")]
    [RoutePrefix("api/operator")]
    public class OperatorController : ApiController
    {
        private readonly SZR_ProductionEntities2 _context;

        public OperatorController()
        {
            _context = new SZR_ProductionEntities2();
        }

        // GET: api/operator/active-batches?page=1&pageSize=20&line=Линия-1&status=В работе&product=
        [HttpGet]
        [Route("active-batches")]
        public async Task<IHttpActionResult> GetActiveBatches(
            int page = 1,
            int pageSize = 20,
            string line = null,
            string status = null,
            string product = null)
        {
            try
            {
                var validation = ValidatePagination(page, pageSize);
                if (validation != null)
                    return validation;

                var query = _context.ProductionBatches
                    .Where(b => b.Status == "Подготовлена" || b.Status == "В работе")
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(line))
                {
                    string normalizedLine = line.Trim();
                    query = query.Where(b => b.Line == normalizedLine);
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    string normalizedStatus = status.Trim();
                    query = query.Where(b => b.Status == normalizedStatus);
                }

                if (!string.IsNullOrWhiteSpace(product))
                {
                    string normalizedProduct = product.Trim();
                    query = query.Where(b =>
                        b.Products.Name.Contains(normalizedProduct) ||
                        b.Products.Code.Contains(normalizedProduct));
                }

                int totalCount = await query.CountAsync();

                var batches = await query
                    .OrderByDescending(b => b.StartedAt)
                    .ThenByDescending(b => b.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => new
                    {
                        id = b.Id,
                        batchNumber = b.BatchNumber,
                        productId = b.ProductId,
                        productName = b.Products.Name,
                        line = b.Line,
                        status = b.Status,
                        labStatus = b.LabStatus,
                        startedAt = b.StartedAt,
                        currentStep = _context.BatchStepExecutions
                            .Where(bs => bs.BatchId == b.Id && bs.Status != "Завершён")
                            .OrderBy(bs => bs.StepNumber)
                            .Select(bs => (int?)bs.StepNumber)
                            .FirstOrDefault(),
                        currentStepName = _context.BatchStepExecutions
                            .Where(bs => bs.BatchId == b.Id && bs.Status != "Завершён")
                            .OrderBy(bs => bs.StepNumber)
                            .Select(bs => bs.TechSteps.Name)
                            .FirstOrDefault(),
                        currentStepStatus = _context.BatchStepExecutions
                            .Where(bs => bs.BatchId == b.Id && bs.Status != "Завершён")
                            .OrderBy(bs => bs.StepNumber)
                            .Select(bs => bs.Status)
                            .FirstOrDefault(),
                        hasWarning = _context.DeviationEvents.Any(d =>
                            d.BatchId == b.Id &&
                            d.Severity == "Предупреждение" &&
                            d.ResolvedAt == null),
                        hasCriticalDeviation = _context.DeviationEvents.Any(d =>
                            d.BatchId == b.Id &&
                            d.Severity == "Критично" &&
                            d.ResolvedAt == null)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = batches,
                    pagination = BuildPagination(page, pageSize, totalCount),
                    message = "Активные партии получены"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения активных партий: " + ex.Message);
            }
        }

        // GET: api/operator/batch/B-2026-05-16-1/program
        [HttpGet]
        [Route("batch/{batchNumber}/program")]
        public async Task<IHttpActionResult> GetBatchProgram(string batchNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(batchNumber))
                    return BadRequestMessage("Номер партии обязателен");

                string normalizedBatchNumber = batchNumber.Trim();

                var batch = await _context.ProductionBatches
                    .Include(b => b.Products)
                    .Include(b => b.TechCards)
                    .FirstOrDefaultAsync(b => b.BatchNumber == normalizedBatchNumber);

                if (batch == null)
                    return NotFoundMessage("Партия не найдена");

                var steps = await _context.TechSteps
                    .Where(ts => ts.TechCardId == batch.TechCardId)
                    .OrderBy(ts => ts.StepNumber)
                    .Select(ts => new
                    {
                        id = ts.Id,
                        stepNumber = ts.StepNumber,
                        name = ts.Name,
                        stepType = ts.StepType,
                        instruction = ts.Instruction,
                        isMandatory = ts.IsMandatory,
                        plannedParams = ts.PlannedParams,
                        toleranceParams = ts.ToleranceParams,
                        equipmentId = ts.EquipmentId,
                        equipmentName = ts.Equipment != null ? ts.Equipment.Name : null
                    })
                    .ToListAsync();

                var executions = await _context.BatchStepExecutions
                    .Where(bs => bs.BatchId == batch.Id)
                    .Select(bs => new
                    {
                        id = bs.Id,
                        stepId = bs.StepId,
                        stepNumber = bs.StepNumber,
                        status = bs.Status,
                        startedAt = bs.StartedAt,
                        finishedAt = bs.FinishedAt,
                        startedBy = bs.StartedBy,
                        finishedBy = bs.FinishedBy,
                        actualParams = bs.ActualParams,
                        equipmentId = bs.EquipmentId
                    })
                    .ToListAsync();

                var resultSteps = steps.Select(s =>
                {
                    var execution = executions.FirstOrDefault(e => e.stepNumber == s.stepNumber);

                    return new
                    {
                        s.id,
                        executionId = execution != null ? execution.id : 0,
                        s.stepNumber,
                        s.name,
                        s.stepType,
                        s.instruction,
                        s.isMandatory,
                        s.plannedParams,
                        s.toleranceParams,
                        s.equipmentId,
                        s.equipmentName,
                        status = execution != null ? execution.status : "Не начат",
                        startedAt = execution != null ? execution.startedAt : null,
                        finishedAt = execution != null ? execution.finishedAt : null,
                        startedBy = execution != null ? execution.startedBy : null,
                        finishedBy = execution != null ? execution.finishedBy : null,
                        actualParams = execution != null ? execution.actualParams : null
                    };
                }).ToList();

                var currentStep = resultSteps
                    .Where(s => s.status != "Завершён")
                    .OrderBy(s => s.stepNumber)
                    .FirstOrDefault();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        batch = new
                        {
                            id = batch.Id,
                            batchNumber = batch.BatchNumber,
                            productId = batch.ProductId,
                            productName = batch.Products != null ? batch.Products.Name : "",
                            line = batch.Line,
                            status = batch.Status,
                            labStatus = batch.LabStatus,
                            startedAt = batch.StartedAt,
                            finishedAt = batch.FinishedAt,
                            techCardId = batch.TechCardId
                        },
                        currentStep,
                        steps = resultSteps
                    },
                    message = "Программа партии получена"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения программы партии: " + ex.Message);
            }
        }

        // POST: api/operator/batch/B-2026-05-16-1/step/1/start
        [HttpPost]
        [Route("batch/{batchNumber}/step/{stepNumber:int}/start")]
        public async Task<IHttpActionResult> StartStep(string batchNumber, int stepNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(batchNumber))
                    return BadRequestMessage("Номер партии обязателен");

                if (stepNumber <= 0)
                    return BadRequestMessage("Некорректный номер шага");

                int userId = GetCurrentUserId();
                string normalizedBatchNumber = batchNumber.Trim();

                var batch = await _context.ProductionBatches
                    .FirstOrDefaultAsync(b => b.BatchNumber == normalizedBatchNumber);

                if (batch == null)
                    return NotFoundMessage("Партия не найдена");

                if (batch.Status == "Завершена" || batch.Status == "Заблокирована" || batch.Status == "Отменена")
                    return BadRequestMessage("Нельзя начать шаг для партии в статусе: " + batch.Status);

                bool hasCriticalDeviation = await _context.DeviationEvents.AnyAsync(d =>
                    d.BatchId == batch.Id &&
                    d.Severity == "Критично" &&
                    d.ResolvedAt == null);

                if (hasCriticalDeviation)
                    return BadRequestMessage("Нельзя начать шаг: по партии есть нерешённое критическое отклонение");

                var execution = await _context.BatchStepExecutions
                    .Include(bs => bs.TechSteps)
                    .FirstOrDefaultAsync(bs => bs.BatchId == batch.Id && bs.StepNumber == stepNumber);

                if (execution == null)
                    return NotFoundMessage("Шаг не найден");

                if (execution.Status != "Не начат")
                    return BadRequestMessage("Шаг уже начат или завершён");

                bool hasStepInProgress = await _context.BatchStepExecutions.AnyAsync(bs =>
                    bs.BatchId == batch.Id &&
                    bs.Status == "Выполняется");

                if (hasStepInProgress)
                    return BadRequestMessage("Нельзя начать новый шаг: другой шаг уже выполняется");

                bool previousMandatoryNotCompleted = await _context.BatchStepExecutions.AnyAsync(bs =>
                    bs.BatchId == batch.Id &&
                    bs.StepNumber < stepNumber &&
                    bs.TechSteps.IsMandatory &&
                    bs.Status != "Завершён");

                if (previousMandatoryNotCompleted)
                    return BadRequestMessage("Нельзя начать шаг: предыдущие обязательные шаги не завершены");

                string oldBatchStatus = batch.Status;

                execution.Status = "Выполняется";
                execution.StartedAt = DateTime.Now;
                execution.StartedBy = userId;

                if (batch.Status == "Подготовлена")
                {
                    batch.Status = "В работе";
                    batch.StartedAt = batch.StartedAt ?? DateTime.Now;
                }

                AddAudit(
                    userId,
                    "Начало выполнения шага",
                    "ProductionBatch",
                    batch.Id,
                    oldBatchStatus,
                    "Шаг " + stepNumber + " выполняется"
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        batchId = batch.Id,
                        batchNumber = batch.BatchNumber,
                        stepNumber = execution.StepNumber,
                        stepName = execution.TechSteps != null ? execution.TechSteps.Name : "",
                        stepStatus = execution.Status,
                        batchStatus = batch.Status,
                        startedAt = execution.StartedAt
                    },
                    message = "Шаг начат"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка начала шага: " + ex.Message);
            }
        }

        // POST: api/operator/batch/B-2026-05-16-1/step/1/complete
        [HttpPost]
        [Route("batch/{batchNumber}/step/{stepNumber:int}/complete")]
        public async Task<IHttpActionResult> CompleteStep(
            string batchNumber,
            int stepNumber,
            [FromBody] CompleteStepDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(batchNumber))
                    return BadRequestMessage("Номер партии обязателен");

                if (stepNumber <= 0)
                    return BadRequestMessage("Некорректный номер шага");

                if (dto == null)
                    return BadRequestMessage("Тело запроса пустое");

                string actualParamsJson = BuildActualParamsJson(dto);

                if (string.IsNullOrWhiteSpace(actualParamsJson))
                    return BadRequestMessage("Фактические параметры шага обязательны");

                int userId = GetCurrentUserId();
                string normalizedBatchNumber = batchNumber.Trim();

                var batch = await _context.ProductionBatches
                    .FirstOrDefaultAsync(b => b.BatchNumber == normalizedBatchNumber);

                if (batch == null)
                    return NotFoundMessage("Партия не найдена");

                if (batch.Status == "Завершена" || batch.Status == "Заблокирована" || batch.Status == "Отменена")
                    return BadRequestMessage("Нельзя завершить шаг для партии в статусе: " + batch.Status);

                var execution = await _context.BatchStepExecutions
                    .Include(bs => bs.TechSteps)
                    .FirstOrDefaultAsync(bs => bs.BatchId == batch.Id && bs.StepNumber == stepNumber);

                if (execution == null)
                    return NotFoundMessage("Шаг не найден");

                if (execution.Status != "Выполняется")
                    return BadRequestMessage("Шаг не находится в процессе выполнения");

                bool hasCriticalDeviation = await _context.DeviationEvents.AnyAsync(d =>
                    d.BatchId == batch.Id &&
                    d.StepExecutionId == execution.Id &&
                    d.Severity == "Критично" &&
                    d.ResolvedAt == null);

                if (hasCriticalDeviation)
                    return BadRequestMessage("Нельзя завершить шаг: по шагу есть нерешённое критическое отклонение");

                execution.ActualParams = actualParamsJson;
                execution.Status = "Завершён";
                execution.FinishedAt = DateTime.Now;
                execution.FinishedBy = userId;

                AddAudit(
                    userId,
                    "Завершение выполнения шага",
                    "ProductionBatch",
                    batch.Id,
                    "Шаг " + stepNumber + " выполняется",
                    "Шаг " + stepNumber + " завершён"
                );

                await _context.SaveChangesAsync();

                bool hasUnfinishedMandatorySteps = await _context.BatchStepExecutions.AnyAsync(bs =>
                    bs.BatchId == batch.Id &&
                    bs.TechSteps.IsMandatory &&
                    bs.Status != "Завершён");

                if (!hasUnfinishedMandatorySteps)
                {
                    string oldStatus = batch.Status;
                    batch.Status = "Завершена";
                    batch.FinishedAt = DateTime.Now;

                    AddAudit(
                        userId,
                        "Завершение производственной партии",
                        "ProductionBatch",
                        batch.Id,
                        oldStatus,
                        batch.Status
                    );

                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        batchId = batch.Id,
                        batchNumber = batch.BatchNumber,
                        stepNumber = execution.StepNumber,
                        stepStatus = execution.Status,
                        batchStatus = batch.Status,
                        finishedAt = execution.FinishedAt
                    },
                    message = "Шаг завершён"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка завершения шага: " + ex.Message);
            }
        }



        // GET: api/operator/batch/B-2026-05-16-1/journal
        [HttpGet]
        [Route("batch/{batchNumber}/journal")]
        public async Task<IHttpActionResult> GetBatchJournal(string batchNumber, int limit = 50)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(batchNumber))
                    return BadRequestMessage("Номер партии обязателен");

                if (limit < 1 || limit > 200)
                    return BadRequestMessage("limit должен быть от 1 до 200");

                string normalizedBatchNumber = batchNumber.Trim();

                var batch = await _context.ProductionBatches
                    .FirstOrDefaultAsync(b => b.BatchNumber == normalizedBatchNumber);

                if (batch == null)
                    return NotFoundMessage("Партия не найдена");

                var deviations = await _context.DeviationEvents
                    .Where(d => d.BatchId == batch.Id)
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(limit)
                    .Select(d => new
                    {
                        id = d.Id,
                        source = "Deviation",
                        eventType = d.EventType,
                        severity = d.Severity,
                        description = d.Description,
                        parameterName = d.ParameterName,
                        plannedValue = d.PlannedValue,
                        actualValue = d.ActualValue,
                        createdAt = d.CreatedAt,
                        createdBy = d.CreatedBy
                    })
                    .ToListAsync();

                var audits = await _context.AuditLogs
                    .Where(a => a.EntityType == "ProductionBatch" && a.EntityId == batch.Id)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(limit)
                    .Select(a => new
                    {
                        id = a.Id,
                        source = "Audit",
                        action = a.Action,
                        oldValue = a.OldValue,
                        newValue = a.NewValue,
                        createdAt = a.CreatedAt,
                        userId = a.UserId
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        batchId = batch.Id,
                        batchNumber = batch.BatchNumber,
                        deviations,
                        audits
                    },
                    message = "Журнал партии получен"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения журнала партии: " + ex.Message);
            }
        }

        // POST: api/operator/report-problem
        [HttpPost]
        [Route("report-problem")]
        public async Task<IHttpActionResult> ReportProblem([FromBody] ReportProblemDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequestMessage("Тело запроса пустое");

                if (string.IsNullOrWhiteSpace(dto.BatchNumber))
                    return BadRequestMessage("Номер партии обязателен");

                if (string.IsNullOrWhiteSpace(dto.ProblemType))
                    return BadRequestMessage("Тип проблемы обязателен");

                if (string.IsNullOrWhiteSpace(dto.Description))
                    return BadRequestMessage("Описание проблемы обязательно");

                string severity = NormalizeSeverity(dto.Severity);
                if (severity == null)
                    return BadRequestMessage("Критичность должна быть: Информация, Предупреждение или Критично");

                int userId = GetCurrentUserId();
                string normalizedBatchNumber = dto.BatchNumber.Trim();

                var batch = await _context.ProductionBatches
                    .FirstOrDefaultAsync(b => b.BatchNumber == normalizedBatchNumber);

                if (batch == null)
                    return NotFoundMessage("Партия не найдена");

                int? equipmentId = null;

                if (!string.IsNullOrWhiteSpace(dto.Equipment))
                {
                    string equipmentValue = dto.Equipment.Trim();

                    var equipment = await _context.Equipment
                        .FirstOrDefaultAsync(e =>
                            e.Code == equipmentValue ||
                            e.Name == equipmentValue);

                    if (equipment != null)
                        equipmentId = equipment.Id;
                }

                var deviation = new DeviationEvents
                {
                    BatchId = batch.Id,
                    StepExecutionId = null,
                    EventType = "Проблема оператора",
                    ParameterName = dto.ProblemType.Trim(),
                    PlannedValue = null,
                    ActualValue = null,
                    Severity = severity,
                    Description = dto.Description.Trim(),
                    CreatedAt = DateTime.Now,
                    CreatedBy = userId,
                    EquipmentId = equipmentId
                };

                _context.DeviationEvents.Add(deviation);

                _context.Notifications.Add(new Notifications
                {
                    UserId = userId,
                    Type = "Problem",
                    Title = "Проблема зарегистрирована",
                    Message = "Ваше сообщение о проблеме принято. Партия: " +
                              batch.BatchNumber + ". Тип: " + dto.ProblemType,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });

                await CreateNotificationsForRole(
                    "Технолог",
                    "Проблема на партии " + batch.BatchNumber,
                    "Отправитель: " + User.Identity.Name +
                    "\nТип: " + dto.ProblemType +
                    "\nОборудование: " + (dto.Equipment ?? "Не указано") +
                    "\nОписание: " + dto.Description +
                    "\nКритичность: " + severity
                );

                if (severity == "Критично")
                {
                    await CreateNotificationsForRole(
                        "Администратор",
                        "Критическая проблема на партии " + batch.BatchNumber,
                        "Тип: " + dto.ProblemType +
                        "\nОписание: " + dto.Description +
                        "\nКритичность: " + severity
                    );
                }

                AddAudit(
                    userId,
                    "Сообщение о проблеме оператором",
                    "ProductionBatch",
                    batch.Id,
                    null,
                    dto.ProblemType + ": " + dto.Description
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        deviationId = deviation.Id,
                        batchId = batch.Id,
                        batchNumber = batch.BatchNumber,
                        severity = deviation.Severity
                    },
                    message = "Сообщение о проблеме зарегистрировано"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка отправки сообщения: " + ex.Message);
            }
        }

        private string BuildActualParamsJson(CompleteStepDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.ActualParameters))
                return dto.ActualParameters.Trim();

            if (dto.ActualParams != null)
                return JsonConvert.SerializeObject(dto.ActualParams);

            return null;
        }

        private string NormalizeSeverity(string severity)
        {
            if (string.IsNullOrWhiteSpace(severity))
                return "Предупреждение";

            string value = severity.Trim().ToLowerInvariant();

            if (value == "информация" || value == "info")
                return "Информация";

            if (value == "предупреждение" || value == "warning")
                return "Предупреждение";

            if (value == "критично" || value == "critical")
                return "Критично";

            return null;
        }

        private async Task CreateNotificationsForRole(string roleName, string title, string message)
        {
            var users = await _context.Users
                .Where(u => u.Roles != null && u.Roles.Name == roleName && u.IsActive)
                .ToListAsync();

            foreach (var user in users)
            {
                _context.Notifications.Add(new Notifications
                {
                    UserId = user.Id,
                    Type = "Problem",
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
            return Content(HttpStatusCode.BadRequest, new { success = false, message });
        }

        private IHttpActionResult UnauthorizedMessage(string message)
        {
            return Content(HttpStatusCode.BadRequest, new { success = false, message });
        }

        private IHttpActionResult ServerError(string message)
        {
            return Content(HttpStatusCode.InternalServerError, new
            {
                success = false,
                message
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _context.Dispose();

            base.Dispose(disposing);
        }
    }

    public class ReportProblemDto
    {
        public string BatchNumber { get; set; }
        public string ProblemType { get; set; }
        public string Equipment { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }
    }

    public class CompleteStepDto
    {
        public string ActualParameters { get; set; }
        public object ActualParams { get; set; }
    }
}