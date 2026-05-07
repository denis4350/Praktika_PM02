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
    [RoutePrefix("api/techcards")]
    public class TechCardsController : ApiController
    {
        private readonly SZR_ProductionEntities2 _context;

        public TechCardsController()
        {
            _context = new SZR_ProductionEntities2();
        }

        // GET: api/techcards
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetTechCards(int page = 1, int pageSize = 20, int? productId = null, string status = null)
        {
            var query = _context.TechCards.Include(t => t.Products).AsQueryable();

            if (productId.HasValue)
                query = query.Where(t => t.ProductId == productId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(t => t.Status == status);

            var totalCount = await query.CountAsync();
            var techCards = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new
                {
                    t.Id,
                    ProductName = t.Products.Name,
                    t.Version,
                    t.Status,
                    t.CreatedAt,
                    t.ApprovedAt,
                    StepCount = t.TechSteps.Count
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = techCards,
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        // GET: api/techcards/{id}
        [HttpGet]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> GetTechCard(int id)
        {
            var techCard = await _context.TechCards
                .Include(t => t.Products)
                .Include(t => t.TechSteps)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (techCard == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    techCard.Id,
                    techCard.ProductId,
                    ProductName = techCard.Products.Name,
                    techCard.Version,
                    techCard.Status,
                    techCard.CreatedAt,
                    techCard.ApprovedAt,
                    Steps = techCard.TechSteps.OrderBy(s => s.StepNumber).Select(s => new
                    {
                        s.Id,
                        s.StepNumber,
                        s.StepType,
                        s.Name,
                        s.Instruction,
                        s.IsMandatory,
                        PlannedParams = !string.IsNullOrEmpty(s.PlannedParams) ? Newtonsoft.Json.JsonConvert.DeserializeObject(s.PlannedParams) : null,
                        ToleranceParams = !string.IsNullOrEmpty(s.ToleranceParams) ? Newtonsoft.Json.JsonConvert.DeserializeObject(s.ToleranceParams) : null
                    })
                }
            });
        }

        // POST: api/techcards
        [HttpPost]
        [Route("")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> CreateTechCard([FromBody] CreateTechCardDto dto)
        {
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
            {
                return BadRequest("Продукт не найден");
            }

            int userId = GetCurrentUserId();

            var techCard = new TechCards
            {
                ProductId = dto.ProductId,
                Version = dto.Version,
                Status = "Черновик",
                CreatedBy = userId,
                CreatedAt = DateTime.Now
            };

            _context.TechCards.Add(techCard);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, data = new { techCard.Id, techCard.Version }, message = "Технологическая карта создана" });
        }

        // POST: api/techcards/{id}/steps
        [HttpPost]
        [Route("{id:int}/steps")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> AddStep(int id, [FromBody] AddStepDto dto)
        {
            var techCard = await _context.TechCards.FindAsync(id);
            if (techCard == null)
            {
                return NotFound();
            }

            if (techCard.Status != "Черновик")
            {
                return BadRequest("Нельзя изменять утвержденную карту");
            }

            var maxStepNumber = await _context.TechSteps
                .Where(s => s.TechCardId == id)
                .Select(s => (int?)s.StepNumber)
                .MaxAsync() ?? 0;

            var step = new TechSteps
            {
                TechCardId = id,
                StepNumber = maxStepNumber + 1,
                StepType = dto.StepType,
                Name = dto.Name,
                Instruction = dto.Instruction,
                IsMandatory = dto.IsMandatory,
                PlannedParams = dto.PlannedParams != null ? Newtonsoft.Json.JsonConvert.SerializeObject(dto.PlannedParams) : null,
                ToleranceParams = dto.ToleranceParams != null ? Newtonsoft.Json.JsonConvert.SerializeObject(dto.ToleranceParams) : null
            };

            _context.TechSteps.Add(step);
            await _context.SaveChangesAsync();

            // Возвращаем только ID и номер шага (без циклических ссылок)
            return Ok(new
            {
                success = true,
                data = new { step.Id, step.StepNumber, step.Name },
                message = "Шаг добавлен"
            });
        }

        // POST: api/techcards/{id}/approve
        [HttpPost]
        [Route("{id:int}/approve")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> ApproveTechCard(int id)
        {
            var techCard = await _context.TechCards
                .Include(t => t.TechSteps)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (techCard == null)
            {
                return NotFound();
            }

            if (techCard.Status != "Черновик")
            {
                return BadRequest("Можно утвердить только черновик");
            }

            if (techCard.TechSteps == null || !techCard.TechSteps.Any())
            {
                return BadRequest("Невозможно утвердить: нет ни одного шага");
            }

            // Архивируем старую активную карту для этого продукта
            var activeCard = await _context.TechCards
                .FirstOrDefaultAsync(t => t.ProductId == techCard.ProductId && t.Status == "Утверждена");

            if (activeCard != null)
            {
                activeCard.Status = "Архив";
            }

            techCard.Status = "Утверждена";
            techCard.ApprovedAt = DateTime.Now;
            techCard.ApprovedBy = GetCurrentUserId();

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Технологическая карта утверждена" });
        }
        // GET: api/techcards/for-batch/{batchNumber}
        [HttpGet]
        [Route("for-batch/{batchNumber}")]
        public async Task<IHttpActionResult> GetProgramForBatch(string batchNumber)
        {
            try
            {
                // Находим партию по номеру
                var batch = await _context.ProductionBatches
                    .FirstOrDefaultAsync(b => b.BatchNumber == batchNumber);

                if (batch == null)
                {
                    return Content(HttpStatusCode.NotFound, new { success = false, message = "Партия не найдена" });
                }

                // Находим техкарту
                var techCard = await _context.TechCards
                    .FirstOrDefaultAsync(t => t.Id == batch.TechCardId);

                if (techCard == null)
                {
                    return Content(HttpStatusCode.NotFound, new { success = false, message = "Технологическая карта не найдена" });
                }

                // Получаем все шаги техкарты
                var steps = await _context.TechSteps
                    .Where(s => s.TechCardId == techCard.Id)
                    .OrderBy(s => s.StepNumber)
                    .ToListAsync();

                // Получаем выполнения шагов для этой партии
                var stepExecutions = await _context.BatchStepExecutions
                    .Where(s => s.BatchId == batch.Id)
                    .ToDictionaryAsync(s => s.StepId, s => s);

                // Формируем результат
                var result = new
                {
                    BatchNumber = batch.BatchNumber,
                    ProductId = batch.ProductId,
                    TechCard = new
                    {
                        techCard.Id,
                        techCard.Version,
                        Steps = steps.Select(s => new
                        {
                            s.Id,
                            s.StepNumber,
                            s.StepType,
                            s.Name,
                            s.Instruction,
                            s.IsMandatory,
                            PlannedParams = !string.IsNullOrEmpty(s.PlannedParams)
                                ? Newtonsoft.Json.JsonConvert.DeserializeObject(s.PlannedParams)
                                : null,
                            Status = stepExecutions.ContainsKey(s.Id)
                                ? stepExecutions[s.Id].Status
                                : "Не начат",
                            StartedAt = stepExecutions.ContainsKey(s.Id)
                                ? stepExecutions[s.Id].StartedAt
                                : null,
                            FinishedAt = stepExecutions.ContainsKey(s.Id)
                                ? stepExecutions[s.Id].FinishedAt
                                : null,
                            ActualParams = stepExecutions.ContainsKey(s.Id) && stepExecutions[s.Id].ActualParams != null
                                ? Newtonsoft.Json.JsonConvert.DeserializeObject(stepExecutions[s.Id].ActualParams)
                                : null
                        })
                    }
                };

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                // InternalServerError принимает Exception, а не объект
                return InternalServerError(ex);
            }
        }

        private int GetCurrentUserId()
        {
            var identity = User.Identity as System.Security.Claims.ClaimsIdentity;
            var userIdClaim = identity?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }
    }

    public class CreateTechCardDto
    {
        public int ProductId { get; set; }
        public string Version { get; set; }
    }

    public class AddStepDto
    {
        public string StepType { get; set; }
        public string Name { get; set; }
        public string Instruction { get; set; }
        public bool IsMandatory { get; set; }
        public object PlannedParams { get; set; }
        public object ToleranceParams { get; set; }
    }
}