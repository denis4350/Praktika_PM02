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
    [RoutePrefix("api/recipes")]
    public class RecipesController : ApiController
    {
        private readonly SZR_ProductionEntities2 _context;

        public RecipesController()
        {
            _context = new SZR_ProductionEntities2();
        }

        // GET: api/recipes?page=1&pageSize=20&productId=1&status=Черновик
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetRecipes(
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

                var query = _context.Recipes.AsQueryable();

                if (productId.HasValue)
                    query = query.Where(r => r.ProductId == productId.Value);

                if (!string.IsNullOrWhiteSpace(status))
                {
                    string normalizedStatus = status.Trim();
                    query = query.Where(r => r.Status == normalizedStatus);
                }

                int totalCount = await query.CountAsync();

                var recipes = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        id = r.Id,
                        productId = r.ProductId,
                        productName = r.Products != null ? r.Products.Name : "",
                        version = r.Version,
                        status = r.Status,
                        createdBy = r.CreatedBy,
                        createdAt = r.CreatedAt,
                        approvedBy = r.ApprovedBy,
                        approvedAt = r.ApprovedAt,
                        componentCount = _context.RecipeComponents.Count(c => c.RecipeId == r.Id),
                        totalPercentage = _context.RecipeComponents
                            .Where(c => c.RecipeId == r.Id)
                            .Select(c => (decimal?)c.Percentage)
                            .Sum() ?? 0
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = recipes,
                    pagination = BuildPagination(page, pageSize, totalCount),
                    message = "Список рецептур получен"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения рецептур: " + ex.Message);
            }
        }

        // GET: api/recipes/5
        [HttpGet]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> GetRecipe(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор рецептуры");

                var recipe = await _context.Recipes
                    .Where(r => r.Id == id)
                    .Select(r => new
                    {
                        id = r.Id,
                        productId = r.ProductId,
                        productName = r.Products != null ? r.Products.Name : "",
                        version = r.Version,
                        status = r.Status,
                        createdBy = r.CreatedBy,
                        createdAt = r.CreatedAt,
                        approvedBy = r.ApprovedBy,
                        approvedAt = r.ApprovedAt
                    })
                    .FirstOrDefaultAsync();

                if (recipe == null)
                    return NotFoundMessage("Рецептура не найдена");

                var components = await _context.RecipeComponents
                    .Where(c => c.RecipeId == id)
                    .OrderBy(c => c.LoadOrder)
                    .Select(c => new
                    {
                        id = c.Id,
                        rawMaterialId = c.RawMaterialId,
                        rawMaterialCode = c.RawMaterials != null ? c.RawMaterials.Code : "",
                        rawMaterialName = c.RawMaterials != null ? c.RawMaterials.Name : "",
                        percentage = c.Percentage,
                        toleranceMin = c.ToleranceMin,
                        toleranceMax = c.ToleranceMax,
                        loadOrder = c.LoadOrder
                    })
                    .ToListAsync();

                decimal totalPercentage = components.Sum(c => c.percentage);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        recipe,
                        components,
                        totalPercentage
                    },
                    message = "Рецептура получена"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения рецептуры: " + ex.Message);
            }
        }

        // POST: api/recipes
        [HttpPost]
        [Route("")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> CreateRecipe([FromBody] CreateRecipeRequestDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequestMessage("Тело запроса пустое");

                if (dto.ProductId <= 0)
                    return BadRequestMessage("Некорректный идентификатор продукта");

                if (string.IsNullOrWhiteSpace(dto.Version))
                    return BadRequestMessage("Версия рецептуры обязательна");

                string version = dto.Version.Trim();

                var product = await _context.Products.FindAsync(dto.ProductId);
                if (product == null)
                    return NotFoundMessage("Продукт не найден");

                if (product.Status != "Активен")
                    return BadRequestMessage("Нельзя создать рецептуру для неактивного продукта");

                bool exists = await _context.Recipes.AnyAsync(r =>
                    r.ProductId == dto.ProductId &&
                    r.Version == version);

                if (exists)
                {
                    return Content(HttpStatusCode.Conflict, new
                    {
                        success = false,
                        message = "Рецептура для продукта '" + product.Name + "' с версией '" + version + "' уже существует"
                    });
                }

                int userId = GetCurrentUserId();

                var recipe = new Recipes
                {
                    ProductId = dto.ProductId,
                    Version = version,
                    Status = "Черновик",
                    CreatedBy = userId,
                    CreatedAt = DateTime.Now
                };

                _context.Recipes.Add(recipe);
                await _context.SaveChangesAsync();

                AddAudit(
                    userId,
                    "Создание рецептуры",
                    "Recipe",
                    recipe.Id,
                    null,
                    "Продукт: " + product.Name + ", версия: " + recipe.Version
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = recipe.Id,
                        productId = recipe.ProductId,
                        productName = product.Name,
                        version = recipe.Version,
                        status = recipe.Status,
                        createdAt = recipe.CreatedAt
                    },
                    message = "Рецептура создана"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка создания рецептуры: " + ex.Message);
            }
        }

        // PUT: api/recipes/5/components
        [HttpPut]
        [Route("{id:int}/components")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> UpdateComponents(
            int id,
            [FromBody] UpdateRecipeComponentsRequestDto dto)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (id <= 0)
                        return BadRequestMessage("Некорректный идентификатор рецептуры");

                    if (dto == null || dto.Components == null || !dto.Components.Any())
                        return BadRequestMessage("Не указан состав рецептуры");

                    var recipe = await _context.Recipes.FindAsync(id);
                    if (recipe == null)
                        return NotFoundMessage("Рецептура не найдена");

                    if (recipe.Status != "Черновик")
                        return BadRequestMessage("Нельзя изменять рецептуру в статусе: " + recipe.Status);

                    var duplicateMaterial = dto.Components
                        .GroupBy(c => c.RawMaterialId)
                        .FirstOrDefault(g => g.Key > 0 && g.Count() > 1);

                    if (duplicateMaterial != null)
                        return BadRequestMessage("Один и тот же компонент не должен повторяться в составе рецептуры");

                    var duplicateLoadOrder = dto.Components
                        .GroupBy(c => c.LoadOrder)
                        .FirstOrDefault(g => g.Key > 0 && g.Count() > 1);

                    if (duplicateLoadOrder != null)
                        return BadRequestMessage("Порядок загрузки компонентов не должен повторяться");

                    foreach (var comp in dto.Components)
                    {
                        if (comp.RawMaterialId <= 0)
                            return BadRequestMessage("Некорректный идентификатор сырья");

                        if (comp.Percentage <= 0 || comp.Percentage > 100)
                            return BadRequestMessage("Доля компонента должна быть больше 0 и не больше 100");

                        if (comp.LoadOrder <= 0)
                            return BadRequestMessage("Порядок загрузки должен быть больше 0");

                        if (comp.ToleranceMin.HasValue && comp.ToleranceMax.HasValue &&
                            comp.ToleranceMin.Value > comp.ToleranceMax.Value)
                        {
                            return BadRequestMessage("Минимальный допуск не может быть больше максимального");
                        }

                        bool materialExists = await _context.RawMaterials.AnyAsync(r =>
                            r.Id == comp.RawMaterialId &&
                            r.IsActive);

                        if (!materialExists)
                            return BadRequestMessage("Активное сырьё с ID " + comp.RawMaterialId + " не найдено");
                    }

                    decimal total = dto.Components.Sum(c => c.Percentage);

                    var oldComponents = await _context.RecipeComponents
                        .Where(c => c.RecipeId == id)
                        .ToListAsync();

                    string oldValue = "Компонентов: " + oldComponents.Count +
                                      ", сумма: " + oldComponents.Sum(c => c.Percentage);

                    _context.RecipeComponents.RemoveRange(oldComponents);
                    await _context.SaveChangesAsync();

                    foreach (var comp in dto.Components.OrderBy(c => c.LoadOrder))
                    {
                        _context.RecipeComponents.Add(new RecipeComponents
                        {
                            RecipeId = id,
                            RawMaterialId = comp.RawMaterialId,
                            Percentage = comp.Percentage,
                            ToleranceMin = comp.ToleranceMin,
                            ToleranceMax = comp.ToleranceMax,
                            LoadOrder = comp.LoadOrder
                        });
                    }

                    AddAudit(
                        GetCurrentUserId(),
                        "Изменение состава рецептуры",
                        "Recipe",
                        recipe.Id,
                        oldValue,
                        "Компонентов: " + dto.Components.Length + ", сумма: " + total
                    );

                    await _context.SaveChangesAsync();
                    transaction.Commit();

                    string warning = total != 100
                        ? " Внимание: сумма долей составляет " + total + "%, а не 100%."
                        : "";

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            recipeId = recipe.Id,
                            componentCount = dto.Components.Length,
                            totalPercentage = total
                        },
                        message = "Состав рецептуры обновлён." + warning
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
                    return ServerError("Ошибка обновления состава: " + ex.Message);
                }
            }
        }

        // POST: api/recipes/5/approve
        [HttpPost]
        [Route("{id:int}/approve")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> ApproveRecipe(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор рецептуры");

                var recipe = await _context.Recipes.FindAsync(id);
                if (recipe == null)
                    return NotFoundMessage("Рецептура не найдена");

                if (recipe.Status != "Черновик" && recipe.Status != "На согласовании")
                    return BadRequestMessage("Можно утвердить только черновик или рецептуру на согласовании");

                var components = await _context.RecipeComponents
                    .Where(c => c.RecipeId == id)
                    .ToListAsync();

                if (!components.Any())
                    return BadRequestMessage("Невозможно утвердить рецептуру без компонентов");

                decimal totalPercentage = components.Sum(c => c.Percentage);
                if (totalPercentage != 100)
                    return BadRequestMessage("Невозможно утвердить. Сумма долей " + totalPercentage + "%, должно быть 100%");

                int userId = GetCurrentUserId();
                string oldStatus = recipe.Status;

                recipe.Status = "Утверждена";
                recipe.ApprovedAt = DateTime.Now;
                recipe.ApprovedBy = userId;

                AddAudit(userId, "Утверждение рецептуры", "Recipe", recipe.Id, oldStatus, recipe.Status);

                await _context.SaveChangesAsync();   // триггер автоматически архивирует другие рецептуры

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        recipeId = recipe.Id,
                        productId = recipe.ProductId,
                        version = recipe.Version,
                        status = recipe.Status,
                        approvedAt = recipe.ApprovedAt,
                        approvedBy = recipe.ApprovedBy
                    },
                    message = "Рецептура утверждена"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка утверждения рецептуры: " + ex.Message);
            }
        }

        // DELETE: api/recipes/5
        [HttpDelete]
        [Route("{id:int}")]
        [Authorize(Roles = "Администратор")]
        public async Task<IHttpActionResult> DeleteRecipe(int id)
        {
            try
            {
                var recipe = await _context.Recipes.FindAsync(id);
                if (recipe == null)
                    return Content(HttpStatusCode.NotFound, ApiResponse<object>.Fail("Рецептура не найдена"));

                // Проверяем, используется ли рецептура в партиях
                bool usedInBatches = await _context.ProductionBatches.AnyAsync(b => b.RecipeId == id);
                if (usedInBatches)
                {
                    return Content(HttpStatusCode.Conflict, ApiResponse<object>.Fail(
                        "Невозможно удалить рецептуру: она используется в производственных партиях. Сначала удалите или измените партии."));
                }

                // Удаляем компоненты рецептуры
                var components = await _context.RecipeComponents.Where(c => c.RecipeId == id).ToListAsync();
                _context.RecipeComponents.RemoveRange(components);

                // Удаляем саму рецептуру
                _context.Recipes.Remove(recipe);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(null, "Рецептура полностью удалена"));
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, ApiResponse<object>.Fail("Ошибка удаления: " + ex.Message));
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

    public class CreateRecipeRequestDto
    {
        public int ProductId { get; set; }
        public string Version { get; set; }
    }

    public class UpdateRecipeComponentsRequestDto
    {
        public RecipeComponentRequestDto[] Components { get; set; }
    }

    public class RecipeComponentRequestDto
    {
        public int RawMaterialId { get; set; }
        public decimal Percentage { get; set; }
        public decimal? ToleranceMin { get; set; }
        public decimal? ToleranceMax { get; set; }
        public int LoadOrder { get; set; }
    }
}