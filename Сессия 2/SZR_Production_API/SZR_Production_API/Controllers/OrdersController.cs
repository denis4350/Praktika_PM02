using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using SZR_Production_API.Models;

namespace SZR_Production_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/orders")]
    public class OrdersController : ApiController
    {
        private readonly SZR_ProductionEntities2 _context;

        public OrdersController()
        {
            _context = new SZR_ProductionEntities2();
        }

        // GET: api/orders
        // GET: api/orders
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetOrders(int page = 1, int pageSize = 20, string status = null)
        {
            var query = _context.ProductionOrders.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var totalCount = await query.CountAsync();

            var ordersList = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Получаем продукты отдельно
            var productIds = ordersList.Select(o => o.ProductId).Distinct().ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name);

            var orders = ordersList.Select(o => new
            {
                o.Id,
                o.OrderNumber,
                ProductName = products.ContainsKey(o.ProductId) ? products[o.ProductId] : "",
                o.PlannedQuantity,
                o.Unit,
                o.Status,
                o.PlannedStartDate,
                o.CreatedAt
            }).ToList();

            return Ok(new
            {
                success = true,
                data = orders,
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        // GET: api/orders/{id}
        [HttpGet]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> GetOrder(int id)
        {
            // Получаем заказ без Include
            var order = await _context.ProductionOrders
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Получаем продукт отдельно
            var product = await _context.Products.FindAsync(order.ProductId);
            var productName = product != null ? product.Name : "";

            // Получаем партии для этого заказа (без лишних данных)
            var batches = await _context.ProductionBatches
                .Where(b => b.OrderId == id)
                .Select(b => new
                {
                    b.Id,
                    b.BatchNumber,
                    b.Status,
                    b.StartedAt,
                    b.FinishedAt,
                    b.Line
                })
                .ToListAsync();

            // Формируем результат без циклических ссылок
            var result = new
            {
                order.Id,
                order.OrderNumber,
                order.ProductId,
                ProductName = productName,
                order.PlannedQuantity,
                order.Unit,
                order.Status,
                order.PlannedStartDate,
                order.CreatedAt,
                order.CreatedBy,
                Batches = batches
            };

            return Ok(new { success = true, data = result });
        }

        // POST: api/orders
        // POST: api/orders
        [HttpPost]
        [Route("")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
            {
                return BadRequest("Продукт не найден");
            }

            int userId = GetCurrentUserId();
            var orderNumber = $"PO-{DateTime.Now:yyyyMMdd}-{new Random().Next(1, 999)}";

            var order = new ProductionOrders
            {
                OrderNumber = orderNumber,
                ProductId = dto.ProductId,
                PlannedQuantity = dto.PlannedQuantity,
                Unit = "кг",
                Status = "Черновик",
                PlannedStartDate = dto.PlannedStartDate,
                CreatedBy = userId,
                CreatedAt = DateTime.Now
            };

            _context.ProductionOrders.Add(order);
            await _context.SaveChangesAsync();

            // Возвращаем ТОЛЬКО нужные поля, без циклических ссылок
            var result = new
            {
                order.Id,
                order.OrderNumber,
                order.ProductId,
                ProductName = product.Name,
                order.PlannedQuantity,
                order.Unit,
                order.Status,
                order.PlannedStartDate,
                order.CreatedAt
            };

            return Ok(new { success = true, data = result, message = "Заказ создан" });
        }

        // PUT: api/orders/{id}
        // PUT: api/orders/{id}
        [HttpPut]
        [Route("{id:int}")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> UpdateOrder(int id, [FromBody] UpdateOrderDto dto)
        {
            var order = await _context.ProductionOrders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            if (order.Status == "В работе" || order.Status == "Завершен")
            {
                return BadRequest("Нельзя изменить запущенный или завершенный заказ");
            }

            if (dto.PlannedQuantity.HasValue)
                order.PlannedQuantity = dto.PlannedQuantity.Value;

            if (dto.PlannedStartDate.HasValue)
                order.PlannedStartDate = dto.PlannedStartDate.Value;

            await _context.SaveChangesAsync();

            // Возвращаем только нужные поля
            var result = new
            {
                order.Id,
                order.OrderNumber,
                order.PlannedQuantity,
                order.Status,
                order.PlannedStartDate
            };

            return Ok(new { success = true, data = result, message = "Заказ обновлен" });
        }

        // POST: api/orders/{id}/start
        // POST: api/orders/{id}/start
        [HttpPost]
        [Route("{id:int}/start")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> StartOrder(int id)
        {
            var order = await _context.ProductionOrders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            if (order.Status != "Черновик")
            {
                return BadRequest("Можно запустить только черновик заказа");
            }

            order.Status = "В работе";
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Заказ запущен в работу" });
        }

        // DELETE: api/orders/{id}
        // DELETE: api/orders/{id}
        [HttpDelete]
        [Route("{id:int}")]
        [Authorize(Roles = "Администратор")]
        public async Task<IHttpActionResult> CancelOrder(int id)
        {
            var order = await _context.ProductionOrders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            if (order.Status == "Завершен")
            {
                return BadRequest("Нельзя отменить завершенный заказ");
            }

            order.Status = "Отменен";
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Заказ отменен" });
        }

        public class CreateOrderDto
        {
            public int ProductId { get; set; }
            public decimal PlannedQuantity { get; set; }
            public DateTime? PlannedStartDate { get; set; }
        }

        public class UpdateOrderDto
        {
            public decimal? PlannedQuantity { get; set; }
            public DateTime? PlannedStartDate { get; set; }
        }
        private int GetCurrentUserId()
        {
            var identity = User.Identity as System.Security.Claims.ClaimsIdentity;
            var userIdClaim = identity?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 1;
        }
    }
}