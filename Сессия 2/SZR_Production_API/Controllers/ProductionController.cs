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
    [RoutePrefix("api/products")]
    public class ProductsController : ApiController
    {
        private readonly SZR_ProductionEntities2 _context;

        public ProductsController()
        {
            _context = new SZR_ProductionEntities2();
        }

        // GET: api/products?page=1&pageSize=20&search=гербицид&status=Активен
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetProducts(
            int page = 1,
            int pageSize = 20,
            string search = null,
            string status = null)
        {
            try
            {
                if (page < 1 || pageSize < 1 || pageSize > 100)
                    return BadRequestMessage("Некорректные параметры пагинации");

                var query = _context.Products.AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string value = search.Trim();
                    query = query.Where(p => p.Name.Contains(value) || p.Code.Contains(value));
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    string normalizedStatus = status.Trim();
                    query = query.Where(p => p.Status == normalizedStatus);
                }

                int totalCount = await query.CountAsync();

                var products = await query
                    .OrderBy(p => p.Code)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.Id,
                        p.Code,
                        p.Name,
                        p.ProductType,
                        p.Form,
                        p.Status,
                        p.CreatedAt
                    })
                    .ToListAsync();

                return Ok(ApiResponse<object>.Ok(
                    products,
                    "Список продукции получен",
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
                return ServerError("Ошибка получения продукции: " + ex.Message);
            }
        }

        // GET: api/products/5
        [HttpGet]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> GetProduct(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор продукта");

                var product = await _context.Products
                    .Where(p => p.Id == id)
                    .Select(p => new
                    {
                        p.Id,
                        p.Code,
                        p.Name,
                        p.ProductType,
                        p.Form,
                        p.Status,
                        p.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (product == null)
                    return NotFoundMessage("Продукт не найден");

                return Ok(ApiResponse<object>.Ok(product, "Продукт получен"));
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения продукта: " + ex.Message);
            }
        }

        // POST: api/products
        [HttpPost]
        [Route("")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> CreateProduct([FromBody] CreateProductDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Code))
                    return BadRequestMessage("Код продукта обязателен");

                string code = dto.Code.Trim();
                bool exists = await _context.Products.AnyAsync(p => p.Code == code);
                if (exists)
                    return ConflictMessage("Продукт с таким кодом уже существует");

                var product = new Products
                {
                    Code = code,
                    Name = string.IsNullOrWhiteSpace(dto.Name) ? code : dto.Name.Trim(),
                    ProductType = dto.ProductType,
                    Form = dto.Form,
                    Status = "Активен",
                    CreatedBy = GetCurrentUserId(),
                    CreatedAt = DateTime.Now
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                var result = new
                {
                    product.Id,
                    product.Code,
                    product.Name,
                    product.ProductType,
                    product.Form,
                    product.Status,
                    product.CreatedAt
                };

                return Ok(ApiResponse<object>.Ok(result, "Продукт создан"));
            }
            catch (UnauthorizedAccessException)
            {
                return UnauthorizedMessage("Не авторизован");
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка создания продукта: " + ex.Message);
            }
        }

        // PUT: api/products/5
        [HttpPut]
        [Route("{id:int}")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequestMessage("Тело запроса пустое");

                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return NotFoundMessage("Продукт не найден");

                if (!string.IsNullOrWhiteSpace(dto.Name))
                    product.Name = dto.Name.Trim();
                if (!string.IsNullOrWhiteSpace(dto.ProductType))
                    product.ProductType = dto.ProductType.Trim();
                if (!string.IsNullOrWhiteSpace(dto.Form))
                    product.Form = dto.Form.Trim();
                if (!string.IsNullOrWhiteSpace(dto.Status))
                    product.Status = dto.Status.Trim();

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(null, "Продукт обновлён"));
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка обновления продукта: " + ex.Message);
            }
        }

        // DELETE: api/products/5
        [HttpDelete]
        [Route("{id:int}")]
        [Authorize(Roles = "Администратор")]   // или Технолог,Администратор
        public async Task<IHttpActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return Content(HttpStatusCode.NotFound, ApiResponse<object>.Fail("Продукт не найден"));

                // Проверяем, не используется ли продукт
                bool isUsed = await _context.Recipes.AnyAsync(r => r.ProductId == id) ||
                              await _context.TechCards.AnyAsync(t => t.ProductId == id) ||
                              await _context.ProductionOrders.AnyAsync(o => o.ProductId == id) ||
                              await _context.ProductionBatches.AnyAsync(b => b.ProductId == id);

                if (isUsed)
                {
                    return Content(HttpStatusCode.Conflict, ApiResponse<object>.Fail(
                        "Невозможно удалить продукт: он используется в рецептурах, техкартах, заказах или партиях. Сначала удалите или измените связанные записи."));
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(null, "Продукт полностью удалён"));
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, ApiResponse<object>.Fail("Ошибка удаления: " + ex.Message));
            }
        }


        // ---------- Вспомогательные методы ----------
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

        private IHttpActionResult ConflictMessage(string message)
            => Content(HttpStatusCode.Conflict, ApiResponse<object>.Fail(message));

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

    // DTO классы для продуктов (можно вынести в отдельную папку DTO)
    public class CreateProductDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string ProductType { get; set; }
        public string Form { get; set; }
    }

    public class UpdateProductDto
    {
        public string Name { get; set; }
        public string ProductType { get; set; }
        public string Form { get; set; }
        public string Status { get; set; }
    }
}