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
    [RoutePrefix("api/raw-materials")]
    public class RawMaterialsController : ApiController
    {
        private readonly SZR_ProductionEntities2 _context;

        public RawMaterialsController()
        {
            _context = new SZR_ProductionEntities2();
        }

        // GET: api/raw-materials?page=1&pageSize=20&search=&category=&isActive=true
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetRawMaterials(
            int page = 1,
            int pageSize = 20,
            string search = null,
            string category = null,
            bool? isActive = null)
        {
            try
            {
                var validation = ValidatePagination(page, pageSize);
                if (validation != null)
                    return validation;

                var query = _context.RawMaterials.AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string value = search.Trim();
                    query = query.Where(r => r.Name.Contains(value) || r.Code.Contains(value));
                }

                if (!string.IsNullOrWhiteSpace(category))
                {
                    string normalizedCategory = category.Trim();
                    query = query.Where(r => r.Category == normalizedCategory);
                }

                if (isActive.HasValue)
                {
                    query = query.Where(r => r.IsActive == isActive.Value);
                }

                int totalCount = await query.CountAsync();

                var materials = await query
                    .OrderBy(r => r.Code)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        id = r.Id,
                        code = r.Code,
                        name = r.Name,
                        category = r.Category,
                        unit = r.Unit,
                        isActive = r.IsActive,
                        usedInRecipes = _context.RecipeComponents.Any(c => c.RawMaterialId == r.Id),
                        batchesCount = _context.RawMaterialBatches.Count(b => b.MaterialId == r.Id)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = materials,
                    pagination = BuildPagination(page, pageSize, totalCount),
                    message = "Список сырья получен"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения сырья: " + ex.Message);
            }
        }

        // GET: api/raw-materials/5
        [HttpGet]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> GetRawMaterial(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор сырья");

                var material = await _context.RawMaterials
                    .Where(r => r.Id == id)
                    .Select(r => new
                    {
                        id = r.Id,
                        code = r.Code,
                        name = r.Name,
                        category = r.Category,
                        unit = r.Unit,
                        isActive = r.IsActive,
                        usedInRecipes = _context.RecipeComponents.Any(c => c.RawMaterialId == r.Id),
                        batchesCount = _context.RawMaterialBatches.Count(b => b.MaterialId == r.Id)
                    })
                    .FirstOrDefaultAsync();

                if (material == null)
                    return NotFoundMessage("Сырьё не найдено");

                return Ok(new
                {
                    success = true,
                    data = material,
                    message = "Сырьё получено"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения сырья: " + ex.Message);
            }
        }

        // POST: api/raw-materials
        [HttpPost]
        [Route("")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> CreateRawMaterial([FromBody] CreateRawMaterialDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequestMessage("Тело запроса пустое");

                if (string.IsNullOrWhiteSpace(dto.Code))
                    return BadRequestMessage("Код сырья обязателен");

                if (string.IsNullOrWhiteSpace(dto.Name))
                    return BadRequestMessage("Наименование сырья обязательно");

                if (string.IsNullOrWhiteSpace(dto.Unit))
                    return BadRequestMessage("Единица измерения обязательна");

                string code = dto.Code.Trim();
                string name = dto.Name.Trim();
                string unit = dto.Unit.Trim();
                string normalizedCategory = string.IsNullOrWhiteSpace(dto.Category) ? null : dto.Category.Trim();

                bool exists = await _context.RawMaterials.AnyAsync(r => r.Code == code);

                if (exists)
                    return Content(HttpStatusCode.Conflict, new
                    {
                        success = false,
                        message = "Сырьё с таким кодом уже существует"
                    });

                var material = new RawMaterials
                {
                    Code = code,
                    Name = name,
                    Category = normalizedCategory,
                    Unit = unit,
                    IsActive = true
                };

                _context.RawMaterials.Add(material);
                await _context.SaveChangesAsync();

                AddAudit(
                    GetCurrentUserId(),
                    "Создание сырья",
                    "RawMaterial",
                    material.Id,
                    null,
                    material.Code + " / " + material.Name
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = material.Id,
                        code = material.Code,
                        name = material.Name,
                        category = material.Category,
                        unit = material.Unit,
                        isActive = material.IsActive
                    },
                    message = "Сырьё создано"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка создания сырья: " + ex.Message);
            }
        }

        // PUT: api/raw-materials/5
        [HttpPut]
        [Route("{id:int}")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> UpdateRawMaterial(int id, [FromBody] UpdateRawMaterialDto dto)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор сырья");

                if (dto == null)
                    return BadRequestMessage("Тело запроса пустое");

                var material = await _context.RawMaterials.FindAsync(id);

                if (material == null)
                    return NotFoundMessage("Сырьё не найдено");

                string oldValue = material.Code + " / " + material.Name + " / " + material.Category + " / " + material.Unit + " / " + material.IsActive;

                if (!string.IsNullOrWhiteSpace(dto.Name))
                    material.Name = dto.Name.Trim();

                if (dto.Category != null)
                    material.Category = string.IsNullOrWhiteSpace(dto.Category) ? null : dto.Category.Trim();

                if (!string.IsNullOrWhiteSpace(dto.Unit))
                    material.Unit = dto.Unit.Trim();

                if (dto.IsActive.HasValue)
                    material.IsActive = dto.IsActive.Value;

                string newValue = material.Code + " / " + material.Name + " / " + material.Category + " / " + material.Unit + " / " + material.IsActive;

                AddAudit(
                    GetCurrentUserId(),
                    "Изменение сырья",
                    "RawMaterial",
                    material.Id,
                    oldValue,
                    newValue
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = material.Id,
                        code = material.Code,
                        name = material.Name,
                        category = material.Category,
                        unit = material.Unit,
                        isActive = material.IsActive
                    },
                    message = "Сырьё обновлено"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка обновления сырья: " + ex.Message);
            }
        }

        // DELETE: api/raw-materials/5
        [HttpDelete]
        [Route("{id:int}")]
        [Authorize(Roles = "Администратор")]
        public async Task<IHttpActionResult> DeleteRawMaterial(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор сырья");

                var material = await _context.RawMaterials.FindAsync(id);

                if (material == null)
                    return NotFoundMessage("Сырьё не найдено");

                if (!material.IsActive)
                    return BadRequestMessage("Сырьё уже деактивировано");

                string oldValue = material.IsActive.ToString();

                material.IsActive = false;

                AddAudit(
                    GetCurrentUserId(),
                    "Деактивация сырья",
                    "RawMaterial",
                    material.Id,
                    oldValue,
                    material.IsActive.ToString()
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = material.Id,
                        code = material.Code,
                        isActive = material.IsActive
                    },
                    message = "Сырьё деактивировано"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка деактивации сырья: " + ex.Message);
            }
        }

        // GET: api/raw-materials/categories
        [HttpGet]
        [Route("categories")]
        public async Task<IHttpActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.RawMaterials
                    .Where(r => r.Category != null && r.Category != "")
                    .Select(r => r.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = categories,
                    message = "Список категорий сырья получен"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения категорий сырья: " + ex.Message);
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

    public class CreateRawMaterialDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Unit { get; set; }
    }

    public class UpdateRawMaterialDto
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Unit { get; set; }
        public bool? IsActive { get; set; }
    }
}