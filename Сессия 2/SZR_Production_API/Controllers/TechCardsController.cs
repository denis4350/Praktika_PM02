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
    [Authorize]
    [RoutePrefix("api/techcards")]
    public class TechCardsController : ApiController
    {
        private readonly SZR_ProductionEntities2 _context;

        public TechCardsController()
        {
            _context = new SZR_ProductionEntities2();
        }

        // GET: api/techcards?page=1&pageSize=20&productId=1&status=Черновик
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetTechCards(
            int page = 1,
            int pageSize = 20,
            int? productId = null,
            string status = null)
        {
            try
            {
                var validation = ValidatePagination(page, pageSize);
                if (validation != null)
                    return validation;

                var query = _context.TechCards.AsQueryable();

                if (productId.HasValue)
                    query = query.Where(t => t.ProductId == productId.Value);

                if (!string.IsNullOrWhiteSpace(status))
                {
                    string normalizedStatus = status.Trim();
                    query = query.Where(t => t.Status == normalizedStatus);
                }

                int totalCount = await query.CountAsync();

                var techCards = await query
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new
                    {
                        id = t.Id,
                        productId = t.ProductId,
                        productName = t.Products != null ? t.Products.Name : "",
                        version = t.Version,
                        status = t.Status,
                        createdBy = t.CreatedBy,
                        createdAt = t.CreatedAt,
                        approvedBy = t.ApprovedBy,
                        approvedAt = t.ApprovedAt,
                        stepCount = _context.TechSteps.Count(s => s.TechCardId == t.Id)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = techCards,
                    pagination = BuildPagination(page, pageSize, totalCount),
                    message = "Список технологических карт получен"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения техкарт: " + ex.Message);
            }
        }

        // GET: api/techcards/5
        [HttpGet]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> GetTechCard(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор технологической карты");

                var techCard = await _context.TechCards
                    .Where(t => t.Id == id)
                    .Select(t => new
                    {
                        id = t.Id,
                        productId = t.ProductId,
                        productName = t.Products != null ? t.Products.Name : "",
                        version = t.Version,
                        status = t.Status,
                        createdBy = t.CreatedBy,
                        createdAt = t.CreatedAt,
                        approvedBy = t.ApprovedBy,
                        approvedAt = t.ApprovedAt
                    })
                    .FirstOrDefaultAsync();

                if (techCard == null)
                    return NotFoundMessage("Технологическая карта не найдена");

                var steps = await _context.TechSteps
                    .Where(s => s.TechCardId == id)
                    .OrderBy(s => s.StepNumber)
                    .Select(s => new
                    {
                        id = s.Id,
                        techCardId = s.TechCardId,
                        stepNumber = s.StepNumber,
                        stepType = s.StepType,
                        name = s.Name,
                        instruction = s.Instruction,
                        isMandatory = s.IsMandatory,
                        plannedParams = s.PlannedParams,
                        toleranceParams = s.ToleranceParams,
                        equipmentId = s.EquipmentId,
                        equipmentName = s.Equipment != null ? s.Equipment.Name : null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        techCard,
                        steps
                    },
                    message = "Технологическая карта получена"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения техкарты: " + ex.Message);
            }
        }

        // POST: api/techcards
        [HttpPost]
        [Route("")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> CreateTechCard([FromBody] CreateTechCardRequestDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequestMessage("Тело запроса пустое");

                if (dto.ProductId <= 0)
                    return BadRequestMessage("Некорректный идентификатор продукта");

                if (string.IsNullOrWhiteSpace(dto.Version))
                    return BadRequestMessage("Версия технологической карты обязательна");

                string version = dto.Version.Trim();

                var product = await _context.Products.FindAsync(dto.ProductId);
                if (product == null)
                    return NotFoundMessage("Продукт не найден");

                if (product.Status != "Активен")
                    return BadRequestMessage("Нельзя создать технологическую карту для неактивного продукта");

                bool exists = await _context.TechCards.AnyAsync(t =>
                    t.ProductId == dto.ProductId &&
                    t.Version == version);

                if (exists)
                {
                    return Content(HttpStatusCode.Conflict, new
                    {
                        success = false,
                        message = "Технологическая карта для продукта '" + product.Name + "' с версией '" + version + "' уже существует"
                    });
                }

                int userId = GetCurrentUserId();

                var techCard = new TechCards
                {
                    ProductId = dto.ProductId,
                    Version = version,
                    Status = "Черновик",
                    CreatedBy = userId,
                    CreatedAt = DateTime.Now
                };

                _context.TechCards.Add(techCard);
                await _context.SaveChangesAsync();

                AddAudit(
                    userId,
                    "Создание технологической карты",
                    "TechCard",
                    techCard.Id,
                    null,
                    "Продукт: " + product.Name + ", версия: " + techCard.Version
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = techCard.Id,
                        productId = techCard.ProductId,
                        productName = product.Name,
                        version = techCard.Version,
                        status = techCard.Status,
                        createdAt = techCard.CreatedAt
                    },
                    message = "Технологическая карта создана"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка создания техкарты: " + ex.Message);
            }
        }

        // POST: api/techcards/5/steps
        [HttpPost]
        [Route("{id:int}/steps")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> AddStep(int id, [FromBody] AddTechStepRequestDto dto)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор технологической карты");

                if (dto == null)
                    return BadRequestMessage("Тело запроса пустое");

                if (string.IsNullOrWhiteSpace(dto.Name))
                    return BadRequestMessage("Наименование шага обязательно");

                if (string.IsNullOrWhiteSpace(dto.StepType))
                    return BadRequestMessage("Тип шага обязателен");

                var techCard = await _context.TechCards.FindAsync(id);
                if (techCard == null)
                    return NotFoundMessage("Технологическая карта не найдена");

                if (techCard.Status != "Черновик")
                    return BadRequestMessage("Нельзя изменять технологическую карту в статусе: " + techCard.Status);

                if (dto.EquipmentId.HasValue)
                {
                    bool equipmentExists = await _context.Equipment.AnyAsync(e =>
                        e.Id == dto.EquipmentId.Value &&
                        e.IsActive);

                    if (!equipmentExists)
                        return BadRequestMessage("Активное оборудование с указанным ID не найдено");
                }

                int maxStepNumber = await _context.TechSteps
                    .Where(s => s.TechCardId == id)
                    .Select(s => (int?)s.StepNumber)
                    .MaxAsync() ?? 0;

                var step = new TechSteps
                {
                    TechCardId = id,
                    StepNumber = maxStepNumber + 1,
                    StepType = dto.StepType.Trim(),
                    Name = dto.Name.Trim(),
                    Instruction = dto.Instruction,
                    IsMandatory = dto.IsMandatory,
                    PlannedParams = dto.PlannedParams != null ? JsonConvert.SerializeObject(dto.PlannedParams) : null,
                    ToleranceParams = dto.ToleranceParams != null ? JsonConvert.SerializeObject(dto.ToleranceParams) : null,
                    EquipmentId = dto.EquipmentId
                };

                _context.TechSteps.Add(step);

                AddAudit(
                    GetCurrentUserId(),
                    "Добавление шага технологической карты",
                    "TechCard",
                    techCard.Id,
                    null,
                    "Шаг " + step.StepNumber + ": " + step.Name
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = step.Id,
                        techCardId = step.TechCardId,
                        stepNumber = step.StepNumber,
                        stepType = step.StepType,
                        name = step.Name,
                        instruction = step.Instruction,
                        isMandatory = step.IsMandatory,
                        plannedParams = step.PlannedParams,
                        toleranceParams = step.ToleranceParams,
                        equipmentId = step.EquipmentId
                    },
                    message = "Шаг добавлен"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка добавления шага: " + ex.Message);
            }
        }

        // PUT: api/techcards/5/steps/10
        [HttpPut]
        [Route("{techCardId:int}/steps/{stepId:int}")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> UpdateStep(
            int techCardId,
            int stepId,
            [FromBody] UpdateTechStepRequestDto dto)
        {
            try
            {
                if (techCardId <= 0 || stepId <= 0)
                    return BadRequestMessage("Некорректный идентификатор техкарты или шага");

                if (dto == null)
                    return BadRequestMessage("Тело запроса пустое");

                var techCard = await _context.TechCards.FindAsync(techCardId);
                if (techCard == null)
                    return NotFoundMessage("Технологическая карта не найдена");

                if (techCard.Status != "Черновик")
                    return BadRequestMessage("Нельзя изменять технологическую карту в статусе: " + techCard.Status);

                var step = await _context.TechSteps
                    .FirstOrDefaultAsync(s => s.Id == stepId && s.TechCardId == techCardId);

                if (step == null)
                    return NotFoundMessage("Шаг технологической карты не найден");

                string oldValue = "Шаг " + step.StepNumber + ": " + step.Name;

                if (!string.IsNullOrWhiteSpace(dto.Name))
                    step.Name = dto.Name.Trim();

                if (!string.IsNullOrWhiteSpace(dto.StepType))
                    step.StepType = dto.StepType.Trim();

                if (dto.Instruction != null)
                    step.Instruction = dto.Instruction;

                if (dto.IsMandatory.HasValue)
                    step.IsMandatory = dto.IsMandatory.Value;

                if (dto.PlannedParams != null)
                    step.PlannedParams = JsonConvert.SerializeObject(dto.PlannedParams);

                if (dto.ToleranceParams != null)
                    step.ToleranceParams = JsonConvert.SerializeObject(dto.ToleranceParams);

                if (dto.EquipmentId.HasValue)
                {
                    bool equipmentExists = await _context.Equipment.AnyAsync(e =>
                        e.Id == dto.EquipmentId.Value &&
                        e.IsActive);

                    if (!equipmentExists)
                        return BadRequestMessage("Активное оборудование с указанным ID не найдено");

                    step.EquipmentId = dto.EquipmentId.Value;
                }

                AddAudit(
                    GetCurrentUserId(),
                    "Изменение шага технологической карты",
                    "TechCard",
                    techCard.Id,
                    oldValue,
                    "Шаг " + step.StepNumber + ": " + step.Name
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = step.Id,
                        techCardId = step.TechCardId,
                        stepNumber = step.StepNumber,
                        stepType = step.StepType,
                        name = step.Name,
                        instruction = step.Instruction,
                        isMandatory = step.IsMandatory,
                        plannedParams = step.PlannedParams,
                        toleranceParams = step.ToleranceParams,
                        equipmentId = step.EquipmentId
                    },
                    message = "Шаг обновлён"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка обновления шага: " + ex.Message);
            }
        }

        // DELETE: api/techcards/5/steps/10
        [HttpDelete]
        [Route("{techCardId:int}/steps/{stepId:int}")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> DeleteStep(int techCardId, int stepId)
        {
            try
            {
                if (techCardId <= 0 || stepId <= 0)
                    return BadRequestMessage("Некорректный идентификатор техкарты или шага");

                var techCard = await _context.TechCards.FindAsync(techCardId);
                if (techCard == null)
                    return NotFoundMessage("Технологическая карта не найдена");

                if (techCard.Status != "Черновик")
                    return BadRequestMessage("Нельзя изменять технологическую карту в статусе: " + techCard.Status);

                var step = await _context.TechSteps
                    .FirstOrDefaultAsync(s => s.Id == stepId && s.TechCardId == techCardId);

                if (step == null)
                    return NotFoundMessage("Шаг технологической карты не найден");

                int deletedStepNumber = step.StepNumber;
                string deletedStepName = step.Name;

                _context.TechSteps.Remove(step);

                var nextSteps = await _context.TechSteps
                    .Where(s => s.TechCardId == techCardId && s.StepNumber > deletedStepNumber)
                    .ToListAsync();

                foreach (var nextStep in nextSteps)
                {
                    nextStep.StepNumber -= 1;
                }

                AddAudit(
                    GetCurrentUserId(),
                    "Удаление шага технологической карты",
                    "TechCard",
                    techCard.Id,
                    "Шаг " + deletedStepNumber + ": " + deletedStepName,
                    null
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        techCardId,
                        deletedStepId = stepId,
                        deletedStepNumber
                    },
                    message = "Шаг удалён"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка удаления шага: " + ex.Message);
            }
        }

        // PUT: api/techcards/5/steps/reorder
        [HttpPut]
        [Route("{id:int}/steps/reorder")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> ReorderSteps(int id, [FromBody] ReorderTechStepsRequestDto dto)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (id <= 0)
                        return BadRequestMessage("Некорректный идентификатор технологической карты");

                    if (dto == null || dto.Items == null || !dto.Items.Any())
                        return BadRequestMessage("Не указан новый порядок шагов");

                    var techCard = await _context.TechCards.FindAsync(id);
                    if (techCard == null)
                        return NotFoundMessage("Технологическая карта не найдена");

                    if (techCard.Status != "Черновик")
                        return BadRequestMessage("Нельзя изменять технологическую карту в статусе: " + techCard.Status);

                    var steps = await _context.TechSteps
                        .Where(s => s.TechCardId == id)
                        .ToListAsync();

                    if (steps.Count != dto.Items.Length)
                        return BadRequestMessage("Количество переданных шагов не совпадает с количеством шагов техкарты");

                    var duplicateNumbers = dto.Items
                        .GroupBy(i => i.StepNumber)
                        .Any(g => g.Count() > 1);

                    if (duplicateNumbers)
                        return BadRequestMessage("Номера шагов не должны повторяться");

                    foreach (var item in dto.Items)
                    {
                        if (item.StepId <= 0 || item.StepNumber <= 0)
                            return BadRequestMessage("Некорректный StepId или StepNumber");

                        var step = steps.FirstOrDefault(s => s.Id == item.StepId);

                        if (step == null)
                            return BadRequestMessage("Шаг с ID " + item.StepId + " не относится к этой техкарте");

                        step.StepNumber = item.StepNumber;
                    }

                    AddAudit(
                        GetCurrentUserId(),
                        "Изменение порядка шагов технологической карты",
                        "TechCard",
                        techCard.Id,
                        null,
                        "Порядок шагов изменён"
                    );

                    await _context.SaveChangesAsync();
                    transaction.Commit();

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            techCardId = techCard.Id,
                            stepsCount = steps.Count
                        },
                        message = "Порядок шагов изменён"
                    });
                }
                catch (UnauthorizedAccessException ex)
                {
                    transaction.Rollback();
                    return UnauthorizedMessage(ex.Message);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return ServerError("Ошибка изменения порядка шагов: " + ex.Message);
                }
            }
        }

        // POST: api/techcards/5/approve
        [HttpPost]
        [Route("{id:int}/approve")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> ApproveTechCard(int id)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (id <= 0)
                        return BadRequestMessage("Некорректный идентификатор технологической карты");

                    var techCard = await _context.TechCards
                        .Include(t => t.TechSteps)
                        .FirstOrDefaultAsync(t => t.Id == id);

                    if (techCard == null)
                        return NotFoundMessage("Технологическая карта не найдена");

                    if (techCard.Status != "Черновик" && techCard.Status != "На согласовании")
                        return BadRequestMessage("Можно утвердить только черновик или техкарту на согласовании");

                    if (techCard.TechSteps == null || !techCard.TechSteps.Any())
                        return BadRequestMessage("Невозможно утвердить: нет ни одного шага");

                    bool hasDuplicateStepNumbers = techCard.TechSteps
                        .GroupBy(s => s.StepNumber)
                        .Any(g => g.Count() > 1);

                    if (hasDuplicateStepNumbers)
                        return BadRequestMessage("Невозможно утвердить: в карте есть повторяющиеся номера шагов");

                    bool hasInvalidStep = techCard.TechSteps.Any(s =>
                        string.IsNullOrWhiteSpace(s.Name) ||
                        string.IsNullOrWhiteSpace(s.StepType) ||
                        s.StepNumber <= 0);

                    if (hasInvalidStep)
                        return BadRequestMessage("Невозможно утвердить: не все шаги заполнены корректно");

                    int userId = GetCurrentUserId();

                    var activeCards = await _context.TechCards
                        .Where(t => t.ProductId == techCard.ProductId &&
                                    t.Status == "Утверждена" &&
                                    t.Id != techCard.Id)
                        .ToListAsync();

                    foreach (var activeCard in activeCards)
                    {
                        activeCard.Status = "Архивирована";

                        AddAudit(
                            userId,
                            "Архивация предыдущей утверждённой технологической карты",
                            "TechCard",
                            activeCard.Id,
                            "Утверждена",
                            "Архивирована"
                        );
                    }

                    string oldStatus = techCard.Status;

                    techCard.Status = "Утверждена";
                    techCard.ApprovedAt = DateTime.Now;
                    techCard.ApprovedBy = userId;

                    AddAudit(
                        userId,
                        "Утверждение технологической карты",
                        "TechCard",
                        techCard.Id,
                        oldStatus,
                        techCard.Status
                    );

                    await _context.SaveChangesAsync();
                    transaction.Commit();

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            techCardId = techCard.Id,
                            productId = techCard.ProductId,
                            version = techCard.Version,
                            status = techCard.Status,
                            approvedAt = techCard.ApprovedAt,
                            approvedBy = techCard.ApprovedBy,
                            stepCount = techCard.TechSteps.Count
                        },
                        message = "Технологическая карта утверждена"
                    });
                }
                catch (UnauthorizedAccessException ex)
                {
                    transaction.Rollback();
                    return UnauthorizedMessage(ex.Message);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return ServerError("Ошибка утверждения техкарты: " + ex.Message);
                }
            }
        }

        // DELETE: api/techcards/5
        [HttpDelete]
        [Route("{id:int}")]
        [Authorize(Roles = "Администратор")]
        public async Task<IHttpActionResult> ArchiveTechCard(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор технологической карты");

                var techCard = await _context.TechCards.FindAsync(id);
                if (techCard == null)
                    return NotFoundMessage("Технологическая карта не найдена");

                if (techCard.Status == "Архивирована")
                    return BadRequestMessage("Технологическая карта уже архивирована");

                bool usedInBatches = await _context.ProductionBatches.AnyAsync(b => b.TechCardId == techCard.Id);

                string oldStatus = techCard.Status;
                techCard.Status = "Архивирована";

                AddAudit(
                    GetCurrentUserId(),
                    usedInBatches
                        ? "Архивация технологической карты, использованной в партиях"
                        : "Архивация технологической карты",
                    "TechCard",
                    techCard.Id,
                    oldStatus,
                    techCard.Status
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        techCardId = techCard.Id,
                        status = techCard.Status,
                        usedInBatches
                    },
                    message = "Технологическая карта архивирована"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка архивации: " + ex.Message);
            }
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
            return Content(HttpStatusCode.BadRequest, new { success = false, message });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _context.Dispose();

            base.Dispose(disposing);
        }
    }

    public class CreateTechCardRequestDto
    {
        public int ProductId { get; set; }
        public string Version { get; set; }
    }

    public class AddTechStepRequestDto
    {
        public string StepType { get; set; }
        public string Name { get; set; }
        public string Instruction { get; set; }
        public bool IsMandatory { get; set; }
        public object PlannedParams { get; set; }
        public object ToleranceParams { get; set; }
        public int? EquipmentId { get; set; }
    }

    public class UpdateTechStepRequestDto
    {
        public string StepType { get; set; }
        public string Name { get; set; }
        public string Instruction { get; set; }
        public bool? IsMandatory { get; set; }
        public object PlannedParams { get; set; }
        public object ToleranceParams { get; set; }
        public int? EquipmentId { get; set; }
    }

    public class ReorderTechStepsRequestDto
    {
        public ReorderTechStepItemDto[] Items { get; set; }
    }

    public class ReorderTechStepItemDto
    {
        public int StepId { get; set; }
        public int StepNumber { get; set; }
    }
}