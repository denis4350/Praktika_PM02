using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using SZR_Production_API.Models;

namespace SZR_Production_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/production")]
    public class ProductionController : ApiController
    {
        private readonly SZR_ProductionEntities2 _context;

        public ProductionController()
        {
            _context = new SZR_ProductionEntities2();
        }

        // GET: api/production/batches
        [HttpGet]
        [Route("batches")]
        public async Task<IHttpActionResult> GetBatches(string status = null, string line = null)
        {
            try
            {
                var query = _context.ProductionBatches.AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(b => b.Status == status);
                }

                if (!string.IsNullOrEmpty(line))
                {
                    query = query.Where(b => b.Line == line);
                }

                var batches = await query
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => new
                    {
                        b.Id,
                        b.BatchNumber,
                        b.OrderId,
                        b.Line,
                        b.Status,
                        b.LabStatus,
                        b.StartedAt,
                        b.FinishedAt
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = batches });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // GET: api/production/batches/active
        [HttpGet]
        [Route("batches/active")]
        public async Task<IHttpActionResult> GetActiveBatches()
        {
            try
            {
                var batches = await _context.ProductionBatches
                    .Where(b => b.Status == "В работе" || b.Status == "Подготовлена")
                    .Select(b => new
                    {
                        b.Id,
                        b.BatchNumber,
                        b.Line,
                        b.Status,
                        b.StartedAt
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = batches });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // GET: api/production/batches/{batchNumber}
        [HttpGet]
        [Route("batches/{batchNumber}")]
        public async Task<IHttpActionResult> GetBatch(string batchNumber)
        {
            try
            {
                var batch = await _context.ProductionBatches
                    .Where(b => b.BatchNumber == batchNumber)
                    .Select(b => new
                    {
                        b.Id,
                        b.BatchNumber,
                        b.OrderId,
                        b.ProductId,
                        b.RecipeId,
                        b.TechCardId,
                        b.Line,
                        b.Status,
                        b.LabStatus,
                        b.StartedAt,
                        b.FinishedAt
                    })
                    .FirstOrDefaultAsync();

                if (batch == null)
                {
                    return NotFound();
                }

                var steps = await _context.BatchStepExecutions
                    .Where(s => s.BatchId == batch.Id)
                    .OrderBy(s => s.StepNumber)
                    .Select(s => new
                    {
                        s.Id,
                        s.StepNumber,
                        s.Status,
                        s.StartedAt,
                        s.FinishedAt,
                        s.ActualParams
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = new { batch, steps } });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // POST: api/production/batches
        // POST: api/production/batches
        // POST: api/production/batches
        [HttpPost]
        [Route("batches")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> CreateBatch([FromBody] CreateBatchDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrEmpty(dto.ProductCode))
                {
                    return BadRequest("Неверные данные");
                }

                // Получаем продукт
                var product = await _context.Products
                    .Where(p => p.Code == dto.ProductCode)
                    .Select(p => new { p.Id })
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    return BadRequest("Продукт не найден");
                }

                // Получаем рецептуру
                var recipe = await _context.Recipes
                    .Where(r => r.ProductId == product.Id && r.Status == "Утверждена")
                    .Select(r => new { r.Id })
                    .FirstOrDefaultAsync();

                if (recipe == null)
                {
                    return BadRequest("Нет утвержденной рецептуры для продукта");
                }

                // Получаем техкарту
                var techCard = await _context.TechCards
                    .Where(t => t.ProductId == product.Id && t.Status == "Утверждена")
                    .Select(t => new { t.Id })
                    .FirstOrDefaultAsync();

                if (techCard == null)
                {
                    return BadRequest("Нет утвержденной технологической карты для продукта");
                }

                int userId = GetCurrentUserId();
                string batchNumber = $"B-{DateTime.Now:yyyyMMdd}-{new Random().Next(1, 9999)}";

                // Создаем партию
                var batch = new ProductionBatches
                {
                    BatchNumber = batchNumber,
                    OrderId = dto.OrderId,
                    ProductId = product.Id,
                    RecipeId = recipe.Id,
                    TechCardId = techCard.Id,
                    Line = dto.Line ?? "",
                    Status = "Подготовлена",
                    CreatedBy = userId,
                    CreatedAt = DateTime.Now
                };

                _context.ProductionBatches.Add(batch);
                await _context.SaveChangesAsync();

                // ==============================================
                // ДОБАВЛЯЕМ ШАГИ ДЛЯ ПАРТИИ (ЭТО ВАЖНО!)
                // ==============================================
                var steps = await _context.TechSteps
                    .Where(s => s.TechCardId == techCard.Id)
                    .OrderBy(s => s.StepNumber)
                    .ToListAsync();

                foreach (var step in steps)
                {
                    _context.BatchStepExecutions.Add(new BatchStepExecutions
                    {
                        BatchId = batch.Id,
                        StepId = step.Id,
                        StepNumber = step.StepNumber,
                        Status = "Не начат"
                    });
                }

                await _context.SaveChangesAsync();
                // ==============================================

                return Ok(new { success = true, data = new { batchNumber }, message = "Партия создана" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // POST: api/production/steps/start
        [HttpPost]
        [Route("steps/start")]
        [Authorize(Roles = "Технолог,Аппаратчик")]
        public async Task<IHttpActionResult> StartStep([FromBody] StartStepDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest("Не переданы данные");
                }

                if (string.IsNullOrEmpty(dto.BatchNumber))
                {
                    return BadRequest("Не указан номер партии");
                }

                // Находим партию
                var batch = await _context.ProductionBatches
                    .FirstOrDefaultAsync(b => b.BatchNumber == dto.BatchNumber);

                if (batch == null)
                {
                    return Content(HttpStatusCode.NotFound, new { success = false, message = $"Партия {dto.BatchNumber} не найдена" });
                }

                // Находим выполнение шага
                var stepExecution = await _context.BatchStepExecutions
                    .FirstOrDefaultAsync(s => s.BatchId == batch.Id && s.StepNumber == dto.StepNumber);

                if (stepExecution == null)
                {
                    return Content(HttpStatusCode.NotFound, new { success = false, message = $"Шаг {dto.StepNumber} для партии {dto.BatchNumber} не найден" });
                }

                // Проверяем статус
                if (stepExecution.Status != "Не начат")
                {
                    return BadRequest($"Шаг уже {stepExecution.Status}");
                }

                int userId = GetCurrentUserId();

                // Обновляем статус
                stepExecution.Status = "Выполняется";
                stepExecution.StartedAt = DateTime.Now;
                stepExecution.StartedBy = userId;

                // Если первый шаг - обновляем статус партии
                if (dto.StepNumber == 1 && batch.Status == "Подготовлена")
                {
                    batch.Status = "В работе";
                    batch.StartedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Шаг {dto.StepNumber} начат",
                    data = new { stepExecution.Status, stepExecution.StartedAt }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в StartStep: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                return InternalServerError(ex);
            }
        }

        // POST: api/production/steps/complete
        [HttpPost]
        [Route("steps/complete")]
        [Authorize(Roles = "Технолог,Аппаратчик")]
        public async Task<IHttpActionResult> CompleteStep([FromBody] CompleteStepDto dto)
        {
            try
            {
                var batch = await _context.ProductionBatches
                    .FirstOrDefaultAsync(b => b.BatchNumber == dto.BatchNumber);

                if (batch == null)
                {
                    return NotFound();
                }

                var stepExecution = await _context.BatchStepExecutions
                    .FirstOrDefaultAsync(s => s.BatchId == batch.Id && s.StepNumber == dto.StepNumber);

                if (stepExecution == null)
                {
                    return NotFound();
                }

                if (stepExecution.Status != "Выполняется")
                {
                    return BadRequest("Шаг не в процессе выполнения");
                }

                int userId = GetCurrentUserId();

                if (dto.ActualParams != null)
                {
                    stepExecution.ActualParams = Newtonsoft.Json.JsonConvert.SerializeObject(dto.ActualParams);
                }

                stepExecution.Status = "Завершен";
                stepExecution.FinishedAt = DateTime.Now;
                stepExecution.FinishedBy = userId;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"Шаг {dto.StepNumber} завершен" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        [HttpPost]
        [Route("deviations")]
        [Authorize(Roles = "Технолог,Аппаратчик,Лаборант")]
        public async Task<IHttpActionResult> RegisterDeviation([FromBody] RegisterDeviationDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrEmpty(dto.BatchNumber))
                {
                    return BadRequest("Неверные данные");
                }

                // Находим партию
                var batch = await _context.ProductionBatches
                    .FirstOrDefaultAsync(b => b.BatchNumber == dto.BatchNumber);

                if (batch == null)
                {
                    return Content(HttpStatusCode.NotFound, new { success = false, message = $"Партия {dto.BatchNumber} не найдена" });
                }

                int? stepExecutionId = null;

                // Если указан номер шага - находим выполнение шага
                if (dto.StepNumber.HasValue)
                {
                    var stepExecution = await _context.BatchStepExecutions
                        .FirstOrDefaultAsync(s => s.BatchId == batch.Id && s.StepNumber == dto.StepNumber.Value);

                    if (stepExecution != null)
                    {
                        stepExecutionId = stepExecution.Id;
                    }
                }

                // Создаем запись об отклонении
                var deviation = new DeviationEvents
                {
                    BatchId = batch.Id,
                    StepExecutionId = stepExecutionId,
                    EventType = "Отклонение параметра",
                    ParameterName = dto.ParameterName,
                    PlannedValue = dto.PlannedValue,
                    ActualValue = dto.ActualValue,
                    Severity = dto.Severity,
                    Description = dto.Description,
                    CreatedAt = DateTime.Now,
                    CreatedBy = GetCurrentUserId()
                };

                _context.DeviationEvents.Add(deviation);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Отклонение зарегистрировано" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        private int GetCurrentUserId()
        {
            var identity = User.Identity as System.Security.Claims.ClaimsIdentity;
            var userIdClaim = identity?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 1;
        }
    }

    public class CreateBatchDto
    {
        public string ProductCode { get; set; }
        public string Line { get; set; }
        public int? OrderId { get; set; }
    }

    public class StartStepDto
    {
        public string BatchNumber { get; set; }
        public int StepNumber { get; set; }
    }

    public class CompleteStepDto
    {
        public string BatchNumber { get; set; }
        public int StepNumber { get; set; }
        public object ActualParams { get; set; }
    }
    public class RegisterDeviationDto
    {
        public string BatchNumber { get; set; }
        public int? StepNumber { get; set; }
        public string ParameterName { get; set; }
        public string PlannedValue { get; set; }
        public string ActualValue { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }
    }

}