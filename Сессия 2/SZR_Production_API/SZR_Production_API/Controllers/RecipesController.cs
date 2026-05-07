using System;
using System.Data.Entity;
using System.Linq;
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

        // GET: api/recipes
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetRecipes(int page = 1, int pageSize = 20, int? productId = null, string status = null)
        {
            var query = _context.Recipes.AsQueryable();

            if (productId.HasValue)
                query = query.Where(r => r.ProductId == productId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            var totalCount = await query.CountAsync();

            var recipesList = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Получаем продукты отдельно
            var productIds = recipesList.Select(r => r.ProductId).Distinct().ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name);

            // Получаем количество компонентов отдельно
            var recipeIds = recipesList.Select(r => r.Id).ToList();
            var componentsCount = await _context.RecipeComponents
                .Where(c => recipeIds.Contains(c.RecipeId))
                .GroupBy(c => c.RecipeId)
                .Select(g => new { RecipeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.RecipeId, g => g.Count);

            var recipes = recipesList.Select(r => new
            {
                r.Id,
                ProductName = products.ContainsKey(r.ProductId) ? products[r.ProductId] : "",
                r.Version,
                r.Status,
                r.CreatedAt,
                r.ApprovedAt,
                ComponentCount = componentsCount.ContainsKey(r.Id) ? componentsCount[r.Id] : 0
            }).ToList();

            return Ok(new
            {
                success = true,
                data = recipes,
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        // GET: api/recipes/{id}
        [HttpGet]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> GetRecipe(int id)
        {
            var recipe = await _context.Recipes
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recipe == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(recipe.ProductId);
            var productName = product != null ? product.Name : "";

            var components = await _context.RecipeComponents
                .Where(c => c.RecipeId == id)
                .Include(c => c.RawMaterials)
                .OrderBy(c => c.LoadOrder)
                .ToListAsync();

            var result = new
            {
                recipe.Id,
                recipe.ProductId,
                ProductName = productName,
                recipe.Version,
                recipe.Status,
                recipe.CreatedAt,
                recipe.ApprovedAt,
                Components = components.Select(c => new
                {
                    c.Id,
                    c.RawMaterialId,
                    MaterialName = c.RawMaterials != null ? c.RawMaterials.Name : "",
                    c.Percentage,
                    c.ToleranceMin,
                    c.ToleranceMax,
                    c.LoadOrder
                }),
                TotalPercentage = components.Sum(c => c.Percentage)
            };

            return Ok(new { success = true, data = result });
        }

        // POST: api/recipes
        [HttpPost]
        [Route("")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> CreateRecipe([FromBody] CreateRecipeDto dto)
        {
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
            {
                return BadRequest("Продукт не найден");
            }

            int userId = GetCurrentUserId();

            var recipe = new Recipes  // ← Recipes (с S) из .edmx
            {
                ProductId = dto.ProductId,
                Version = dto.Version,
                Status = "Черновик",
                CreatedBy = userId,
                CreatedAt = DateTime.Now
            };

            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, data = new { recipe.Id, recipe.Version }, message = "Рецептура создана" });
        }

        // PUT: api/recipes/{id}/components
        [HttpPut]
        [Route("{id:int}/components")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> UpdateComponents(int id, [FromBody] UpdateComponentsDto dto)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
            {
                return NotFound();
            }

            if (recipe.Status != "Черновик")
            {
                return BadRequest("Нельзя изменять утвержденную или архивную рецептуру");
            }

            // Удаляем старые компоненты
            var oldComponents = _context.RecipeComponents.Where(c => c.RecipeId == id);
            _context.RecipeComponents.RemoveRange(oldComponents);

            // Добавляем новые
            foreach (var comp in dto.Components)
            {
                _context.RecipeComponents.Add(new RecipeComponents  // ← RecipeComponents (с S) из .edmx
                {
                    RecipeId = id,
                    RawMaterialId = comp.RawMaterialId,
                    Percentage = comp.Percentage,
                    ToleranceMin = comp.ToleranceMin,
                    ToleranceMax = comp.ToleranceMax,
                    LoadOrder = comp.LoadOrder
                });
            }

            await _context.SaveChangesAsync();

            // Проверка суммы
            var total = dto.Components.Sum(c => c.Percentage);
            string warning = total != 100 ? $" Внимание: сумма долей составляет {total}%, а не 100%" : "";

            return Ok(new { success = true, message = "Состав рецептуры обновлен" + warning });
        }

        // POST: api/recipes/{id}/approve
        [HttpPost]
        [Route("{id:int}/approve")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> ApproveRecipe(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
            {
                return NotFound();
            }

            if (recipe.Status != "Черновик")
            {
                return BadRequest("Можно утвердить только черновик");
            }

            var components = await _context.RecipeComponents
                .Where(c => c.RecipeId == id)
                .ToListAsync();

            var totalPercentage = components.Sum(c => c.Percentage);
            if (totalPercentage != 100)
            {
                return BadRequest($"Невозможно утвердить. Сумма долей {totalPercentage}%, должно быть 100%");
            }

            // Проверяем, нет ли уже утвержденной рецептуры для этого продукта
            var activeRecipe = await _context.Recipes
                .FirstOrDefaultAsync(r => r.ProductId == recipe.ProductId && r.Status == "Утверждена");

            if (activeRecipe != null && activeRecipe.Id != id)
            {
                activeRecipe.Status = "Архив";
            }

            recipe.Status = "Утверждена";
            recipe.ApprovedAt = DateTime.Now;
            recipe.ApprovedBy = GetCurrentUserId();

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Рецептура утверждена" });
        }

        // DELETE: api/recipes/{id}
        [HttpDelete]
        [Route("{id:int}")]
        [Authorize(Roles = "Администратор")]
        public async Task<IHttpActionResult> ArchiveRecipe(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
            {
                return NotFound();
            }

            recipe.Status = "Архив";
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Рецептура архивирована" });
        }

        private int GetCurrentUserId()
        {
            var identity = User.Identity as System.Security.Claims.ClaimsIdentity;
            var userIdClaim = identity?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }
    }

    public class CreateRecipeDto
    {
        public int ProductId { get; set; }
        public string Version { get; set; }
    }

    public class UpdateComponentsDto
    {
        public ComponentDto[] Components { get; set; }
    }

    public class ComponentDto
    {
        public int RawMaterialId { get; set; }
        public decimal Percentage { get; set; }
        public decimal? ToleranceMin { get; set; }
        public decimal? ToleranceMax { get; set; }
        public int LoadOrder { get; set; }
    }
}