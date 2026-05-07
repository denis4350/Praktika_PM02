using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using SZR_Production_API.Models;  // ← для контекста из .edmx

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

        // GET: api/laboratory/raw-material-batches
        [HttpGet]
        [Route("raw-material-batches")]
        public async Task<IHttpActionResult> GetRawMaterialBatches(string status = null)
        {
            var query = _context.RawMaterialBatches.AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.LabStatus == status);
            else
                query = query.Where(r => r.LabStatus == "Ожидает" || r.LabStatus == "В работе");

            var batches = await query
                .OrderByDescending(r => r.ArrivalDate)
                .Select(r => new
                {
                    r.Id,
                    r.BatchNumber,
                    r.SupplierBatch,
                    MaterialName = r.RawMaterials != null ? r.RawMaterials.Name : "",
                    r.Supplier,
                    r.Quantity,
                    r.Unit,
                    r.ArrivalDate,
                    r.LabStatus,
                    HasTest = _context.LabTests.Any(t => t.ObjectId == r.Id && t.ObjectType == "RawMaterial")
                })
                .ToListAsync();

            return Ok(new { success = true, data = batches });
        }

        // GET: api/laboratory/product-batches
        [HttpGet]
        [Route("product-batches")]
        public async Task<IHttpActionResult> GetProductBatches(string status = null)
        {
            var query = _context.ProductionBatches.AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(b => b.LabStatus == status);
            else
                query = query.Where(b => b.LabStatus == "Ожидает" || b.LabStatus == "В работе" || (b.Status == "Ожидает контроля" && b.LabStatus == null));

            var batches = await query
                .OrderByDescending(b => b.FinishedAt)
                .Select(b => new
                {
                    b.Id,
                    b.BatchNumber,
                    ProductName = b.Products != null ? b.Products.Name : "",
                    b.Line,
                    b.Status,
                    b.LabStatus,
                    b.FinishedAt,
                    HasTest = _context.LabTests.Any(t => t.ObjectId == b.Id && t.ObjectType == "Product")
                })
                .ToListAsync();

            return Ok(new { success = true, data = batches });
        }

        // POST: api/laboratory/tests
        [HttpPost]
        [Route("tests")]
        public async Task<IHttpActionResult> CreateTest([FromBody] CreateTestDto dto)
        {
            int userId = GetCurrentUserId();

            var test = new LabTests
            {
                TestNumber = GenerateTestNumber(),
                TestType = dto.TestType,
                ObjectType = dto.ObjectType,
                ObjectId = dto.ObjectId,
                AssignedAt = DateTime.Now,
                AssignedBy = userId,
                Status = "Создано",
                Priority = dto.Priority ?? "Обычный",
                Comment = dto.Comment
            };

            _context.LabTests.Add(test);
            await _context.SaveChangesAsync();

            // Добавляем параметры
            if (dto.Parameters != null)
            {
                foreach (var param in dto.Parameters)
                {
                    _context.LabTestParameters.Add(new LabTestParameters
                    {
                        TestId = test.Id,
                        ParameterName = param.ParameterName,
                        NormMin = param.NormMin,
                        NormMax = param.NormMax,
                        Unit = param.Unit
                    });
                }
                await _context.SaveChangesAsync();
            }

            return Ok(new { success = true, data = new { test.Id, test.TestNumber }, message = "Испытание создано" });
        }

        // PUT: api/laboratory/tests/{id}/results
        [HttpPut]
        [Route("tests/{id}/results")]
        public async Task<IHttpActionResult> UpdateTestResults(int id, [FromBody] UpdateTestResultsDto dto)
        {
            var test = await _context.LabTests.FindAsync(id);
            if (test == null)
            {
                return NotFound();
            }

            var parameters = await _context.LabTestParameters
                .Where(p => p.TestId == id)
                .ToListAsync();

            foreach (var param in parameters)
            {
                var value = dto.Results.FirstOrDefault(r => r.ParameterId == param.Id);
                if (value != null)
                {
                    param.ActualValue = value.ActualValue;
                    param.IsPassed = CheckIfPassed(param.NormMin, param.NormMax, value.ActualValue);
                }
            }

            test.Status = "В работе";
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Результаты сохранены" });
        }

        // POST: api/laboratory/tests/{id}/complete
        [HttpPost]
        [Route("tests/{id}/complete")]
        public async Task<IHttpActionResult> CompleteTest(int id)
        {
            var test = await _context.LabTests
                .Include(t => t.LabTestParameters)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (test == null)
            {
                return NotFound();
            }

            var allPassed = test.LabTestParameters.All(p => p.IsPassed == true);
            var anyFailed = test.LabTestParameters.Any(p => p.IsPassed == false);

            test.Status = "Завершено";
            test.ExecutedAt = DateTime.Now;
            test.ExecutedBy = GetCurrentUserId();

            await _context.SaveChangesAsync();

            string result = allPassed ? "Соответствует" : (anyFailed ? "Не соответствует" : "Требуется проверка");

            return Ok(new { success = true, data = new { Result = result }, message = "Испытание завершено" });
        }

        // POST: api/laboratory/decisions/raw-material
        [HttpPost]
        [Route("decisions/raw-material")]
        [Authorize(Roles = "Лаборант,Технолог,Администратор")]
        public async Task<IHttpActionResult> DecideRawMaterialBatch([FromBody] RawMaterialDecisionDto dto)
        {
            try
            {
                if (dto == null || dto.BatchId == 0)
                {
                    return Content(HttpStatusCode.BadRequest, new { success = false, message = "Не указан ID партии" });
                }

                // Находим партию сырья
                var batch = await _context.RawMaterialBatches.FindAsync(dto.BatchId);
                if (batch == null)
                {
                    return Content(HttpStatusCode.NotFound, new { success = false, message = $"Партия сырья с ID {dto.BatchId} не найдена" });
                }

                // Проверяем, есть ли завершенные испытания
                var hasCompletedTest = await _context.LabTests
                    .AnyAsync(t => t.ObjectId == dto.BatchId
                                && t.ObjectType == "RawMaterial"
                                && t.Status == "Завершено");

                if (!hasCompletedTest)
                {
                    return Content(HttpStatusCode.BadRequest, new { success = false, message = "Невозможно принять решение: нет завершенных испытаний" });
                }

                // Обновляем статус партии
                batch.LabStatus = dto.Decision;
                batch.DecisionAt = DateTime.Now;
                batch.DecisionBy = GetCurrentUserId();
                batch.Comment = dto.Comment;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Партия сырья {batch.BatchNumber} {dto.Decision}",
                    data = new { batch.BatchNumber, batch.LabStatus, batch.DecisionAt }
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // GET: api/laboratory/tests/archive
        [HttpGet]
        [Route("tests/archive")]
        public async Task<IHttpActionResult> GetTestArchive(int page = 1, int pageSize = 20, string objectType = null)
        {
            var query = _context.LabTests
                .Where(t => t.Status == "Завершено")
                .Include(t => t.LabTestParameters)
                .AsQueryable();

            if (!string.IsNullOrEmpty(objectType))
                query = query.Where(t => t.ObjectType == objectType);

            var totalCount = await query.CountAsync();
            var tests = await query
                .OrderByDescending(t => t.ExecutedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new
                {
                    t.Id,
                    t.TestNumber,
                    t.TestType,
                    t.ObjectType,
                    t.ObjectId,
                    t.AssignedAt,
                    t.ExecutedAt,
                    t.ExecutedBy,
                    Parameters = t.LabTestParameters.Select(p => new
                    {
                        p.ParameterName,
                        p.NormMin,
                        p.NormMax,
                        p.ActualValue,
                        p.Unit,
                        p.IsPassed
                    })
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = tests,
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }
        [HttpPost]
        [Route("decisions/product")]
        [Authorize(Roles = "Лаборант,Технолог,Администратор")]
        public async Task<IHttpActionResult> DecideProductBatch([FromBody] ProductDecisionDto dto)
        {
            try
            {
                if (dto == null || dto.BatchId == 0)
                {
                    return Content(HttpStatusCode.BadRequest, new { success = false, message = "Не указан ID партии" });
                }

                // Находим партию продукции
                var batch = await _context.ProductionBatches.FindAsync(dto.BatchId);
                if (batch == null)
                {
                    return Content(HttpStatusCode.NotFound, new { success = false, message = $"Партия продукции с ID {dto.BatchId} не найдена" });
                }

                // Проверяем, есть ли завершенные испытания
                var hasCompletedTest = await _context.LabTests
                    .AnyAsync(t => t.ObjectId == dto.BatchId
                                && t.ObjectType == "Product"
                                && t.Status == "Завершено");

                if (!hasCompletedTest)
                {
                    return Content(HttpStatusCode.BadRequest, new { success = false, message = "Невозможно принять решение: нет завершенных испытаний" });
                }

                // Обновляем статус партии
                batch.LabStatus = dto.Decision;

                if (dto.Decision == "Разрешена")
                {
                    batch.Status = "Завершена";
                }
                else if (dto.Decision == "Заблокирована")
                {
                    batch.Status = "Заблокирована";
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Партия продукции {batch.BatchNumber} {dto.Decision}",
                    data = new { batch.BatchNumber, batch.LabStatus, batch.Status }
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        private bool CheckIfPassed(decimal? normMin, decimal? normMax, decimal? actualValue)
        {
            if (!actualValue.HasValue) return false;
            if (normMin.HasValue && actualValue < normMin) return false;
            if (normMax.HasValue && actualValue > normMax) return false;
            return true;
        }

        private string GenerateTestNumber()
        {
            return $"TST-{DateTime.Now:yyyyMMdd}-{new Random().Next(1, 9999)}";
        }

        private int GetCurrentUserId()
        {
            var identity = User.Identity as System.Security.Claims.ClaimsIdentity;
            var userIdClaim = identity?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
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
    }
}