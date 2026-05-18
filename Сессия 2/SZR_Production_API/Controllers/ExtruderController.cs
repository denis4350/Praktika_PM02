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
    [RoutePrefix("api/extruder")]
    public class ExtruderController : ApiController
    {
        private readonly SZR_ProductionEntities2 _context;

        public ExtruderController()
        {
            _context = new SZR_ProductionEntities2();
        }

        // ============================================================
        // 1. Программы экструдера
        // ============================================================

        // GET: api/extruder/programs?page=1&pageSize=20&productId=1
        [HttpGet]
        [Route("programs")]
        public async Task<IHttpActionResult> GetPrograms(
            int page = 1,
            int pageSize = 20,
            int? productId = null)
        {
            try
            {
                if (page < 1 || pageSize < 1 || pageSize > 100)
                    return BadRequestMessage("Некорректные параметры пагинации");

                var query = _context.ExtruderPrograms.AsQueryable();

                if (productId.HasValue)
                    query = query.Where(p => p.ProductId == productId.Value);

                int totalCount = await query.CountAsync();

                var programs = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Description,
                        p.ProductId,
                        ProductName = p.Products.Name,
                        p.Status,
                        p.CreatedAt,
                        p.ActivatedAt,
                        ZoneCount = p.ExtruderZones.Count
                    })
                    .ToListAsync();

                return Ok(ApiResponse<object>.Ok(
                    programs,
                    "Список программ экструдера получен",
                    new PaginationInfo
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                ));
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения программ: " + ex.Message);
            }
        }

        // GET: api/extruder/programs/5
        [HttpGet]
        [Route("programs/{id:int}")]
        public async Task<IHttpActionResult> GetProgram(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор программы");

                var program = await _context.ExtruderPrograms
                    .Where(p => p.Id == id)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Description,
                        p.ProductId,
                        ProductName = p.Products.Name,
                        p.Status,
                        p.CreatedAt,
                        p.ActivatedAt
                    })
                    .FirstOrDefaultAsync();

                if (program == null)
                    return NotFoundMessage("Программа не найдена");

                var zones = await _context.ExtruderZones
                    .Where(z => z.ProgramId == id)
                    .OrderBy(z => z.ZoneNumber)
                    .Select(z => new
                    {
                        z.Id,
                        z.ZoneNumber,
                        z.ZoneName,
                        z.TemperatureSetpoint,
                        z.TemperatureMin,
                        z.TemperatureMax,
                        z.PressureSetpoint,
                        z.PressureMin,
                        z.PressureMax,
                        z.ScrewSpeed,
                        z.FeedRate
                    })
                    .ToListAsync();

                return Ok(ApiResponse<object>.Ok(
                    new { program, zones },
                    "Программа экструдера получена"
                ));
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения программы: " + ex.Message);
            }
        }

        // POST: api/extruder/programs
        [HttpPost]
        [Route("programs")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> CreateProgram([FromBody] CreateExtruderProgramDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Name) || dto.ProductId <= 0)
                    return BadRequestMessage("Некорректные данные программы");

                if (dto.Zones == null || !dto.Zones.Any())
                    return BadRequestMessage("Необходимо указать хотя бы одну зону");

                var product = await _context.Products.FindAsync(dto.ProductId);
                if (product == null)
                    return NotFoundMessage("Продукт не найден");

                var program = new ExtruderPrograms
                {
                    Name = dto.Name.Trim(),
                    Description = dto.Description,
                    ProductId = dto.ProductId,
                    Status = "Черновик",
                    CreatedAt = DateTime.Now
                };

                _context.ExtruderPrograms.Add(program);
                await _context.SaveChangesAsync();

                foreach (var zone in dto.Zones)
                {
                    _context.ExtruderZones.Add(new ExtruderZones
                    {
                        ProgramId = program.Id,
                        ZoneNumber = zone.ZoneNumber,
                        ZoneName = zone.ZoneName,
                        TemperatureSetpoint = zone.TemperatureSetpoint,
                        TemperatureMin = zone.TemperatureMin,
                        TemperatureMax = zone.TemperatureMax,
                        PressureSetpoint = zone.PressureSetpoint,
                        PressureMin = zone.PressureMin,
                        PressureMax = zone.PressureMax,
                        ScrewSpeed = zone.ScrewSpeed,
                        FeedRate = zone.FeedRate
                    });
                }

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(
                    new { program.Id, program.Name, program.Status },
                    "Программа создана"
                ));
            }
            catch (UnauthorizedAccessException)
            {
                return UnauthorizedMessage("Не авторизован");
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка создания программы: " + ex.Message);
            }
        }

        // PUT: api/extruder/programs/5/activate
        [HttpPut]
        [Route("programs/{id:int}/activate")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> ActivateProgram(int id)
        {
            try
            {
                var program = await _context.ExtruderPrograms.FindAsync(id);
                if (program == null)
                    return NotFoundMessage("Программа не найдена");

                if (program.Status == "Активна")
                    return BadRequestMessage("Программа уже активна");

                program.Status = "Активна";
                program.ActivatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(null, "Программа активирована"));
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка активации программы: " + ex.Message);
            }
        }

        // DELETE: api/extruder/programs/5
        [HttpDelete]
        [Route("programs/{id:int}")]
        [Authorize(Roles = "Администратор")]
        public async Task<IHttpActionResult> ArchiveProgram(int id)
        {
            try
            {
                var program = await _context.ExtruderPrograms.FindAsync(id);
                if (program == null)
                    return NotFoundMessage("Программа не найдена");

                program.Status = "Архивирована";
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(null, "Программа архивирована"));
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка архивирования: " + ex.Message);
            }
        }

        // ============================================================
        // 2. Загрузка программы на партию
        // ============================================================
        [HttpPost]
        [Route("load/{programId:int}")]
        [Authorize(Roles = "Технолог,Администратор,Аппаратчик")]
        public async Task<IHttpActionResult> LoadProgram(int programId, [FromBody] LoadProgramDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.BatchNumber))
                    return BadRequestMessage("Не указан номер партии");

                var program = await _context.ExtruderPrograms
                    .Include(p => p.ExtruderZones)
                    .FirstOrDefaultAsync(p => p.Id == programId);

                if (program == null)
                    return NotFoundMessage("Программа не найдена");

                if (program.Status != "Активна")
                    return BadRequestMessage("Нельзя загрузить неактивную программу");

                var batch = await _context.ProductionBatches
                    .FirstOrDefaultAsync(b => b.BatchNumber == dto.BatchNumber);

                if (batch == null)
                    return NotFoundMessage("Партия не найдена");

                var log = new ExtruderLogs
                {
                    BatchId = batch.Id,
                    ProgramId = programId,
                    LoadedAt = DateTime.Now,
                    LoadedBy = GetCurrentUserId(),
                    Parameters = JsonConvert.SerializeObject(program.ExtruderZones)
                };

                _context.ExtruderLogs.Add(log);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(
                    new { batchId = batch.Id, programId = program.Id },
                    "Программа загружена в экструдер"
                ));
            }
            catch (UnauthorizedAccessException)
            {
                return UnauthorizedMessage("Не авторизован");
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка загрузки программы: " + ex.Message);
            }
        }

        // ============================================================
        // 3. Телеметрия экструдера (по партии)
        // ============================================================
        [HttpGet]
        [Route("telemetry/{batchNumber}")]
        [Authorize(Roles = "Технолог,Администратор,Аппаратчик")]
        public async Task<IHttpActionResult> GetTelemetry(
            string batchNumber,
            int limit = 100)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(batchNumber))
                    return BadRequestMessage("Номер партии обязателен");

                if (limit < 1 || limit > 500)
                    return BadRequestMessage("Лимит должен быть от 1 до 500");

                var telemetry = await _context.ExtruderTelemetry
                    .Where(t => t.BatchNumber == batchNumber)
                    .OrderByDescending(t => t.Timestamp)
                    .Take(limit)
                    .Select(t => new
                    {
                        t.Id,
                        t.BatchNumber,
                        t.ZoneNumber,
                        CurrentTemperature = t.CurrentTemperature,
                        CurrentPressure = t.CurrentPressure,
                        CurrentSpeed = t.CurrentSpeed,
                        Timestamp = t.Timestamp,
                        Status = t.Status
                    })
                    .ToListAsync();

                return Ok(ApiResponse<object>.Ok(telemetry, "Телеметрия получена"));
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения телеметрии: " + ex.Message);
            }
        }

        // ============================================================
        // Вспомогательные методы
        // ============================================================
        private int GetCurrentUserId()
        {
            var identity = User?.Identity as ClaimsIdentity;
            if (identity == null || !identity.IsAuthenticated)
                throw new UnauthorizedAccessException("Пользователь не авторизован");
            var claim = identity.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !int.TryParse(claim.Value, out int userId))
                throw new UnauthorizedAccessException("Некорректный идентификатор пользователя");
            return userId;
        }

        private IHttpActionResult BadRequestMessage(string message)
            => Content(HttpStatusCode.BadRequest, ApiResponse<object>.Fail(message));

        private IHttpActionResult NotFoundMessage(string message)
            => Content(HttpStatusCode.NotFound, ApiResponse<object>.Fail(message));

        private IHttpActionResult UnauthorizedMessage(string message)
            => Content(HttpStatusCode.Unauthorized, ApiResponse<object>.Fail(message));

        private IHttpActionResult ServerError(string message)
            => Content(HttpStatusCode.InternalServerError, ApiResponse<object>.Fail(message));

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _context.Dispose();
            base.Dispose(disposing);
        }
    }

    // DTO
    public class CreateExtruderProgramDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int ProductId { get; set; }
        public ExtruderZoneDto[] Zones { get; set; }
    }

    public class ExtruderZoneDto
    {
        public int ZoneNumber { get; set; }
        public string ZoneName { get; set; }
        public decimal TemperatureSetpoint { get; set; }
        public decimal TemperatureMin { get; set; }
        public decimal TemperatureMax { get; set; }
        public decimal PressureSetpoint { get; set; }
        public decimal PressureMin { get; set; }
        public decimal PressureMax { get; set; }
        public int ScrewSpeed { get; set; }
        public int FeedRate { get; set; }
    }

    public class LoadProgramDto
    {
        public string BatchNumber { get; set; }
    }
}