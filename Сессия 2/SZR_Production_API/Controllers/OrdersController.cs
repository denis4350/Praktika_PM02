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
    [Authorize(Roles = "Технолог,Администратор,Аппаратчик,Лаборант")]
    [RoutePrefix("api/orders")]
    public class OrdersController : ApiController
    {
        private readonly SZR_ProductionEntities2 _context;

        public OrdersController()
        {
            _context = new SZR_ProductionEntities2();
        }

        // GET: api/orders?page=1&pageSize=20&status=Создан&product=гербицид
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetOrders(
            int page = 1,
            int pageSize = 20,
            string status = null,
            string product = null)
        {
            try
            {
                var validation = ValidatePagination(page, pageSize);
                if (validation != null)
                    return validation;

                var query = _context.ProductionOrders.AsQueryable();

                if (!string.IsNullOrWhiteSpace(status))
                {
                    string normalizedStatus = status.Trim();
                    query = query.Where(o => o.Status == normalizedStatus);
                }

                if (!string.IsNullOrWhiteSpace(product))
                {
                    string normalizedProduct = product.Trim();
                    query = query.Where(o =>
                        o.Products.Name.Contains(normalizedProduct) ||
                        o.Products.Code.Contains(normalizedProduct));
                }

                int totalCount = await query.CountAsync();

                var orders = await query
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new
                    {
                        id = o.Id,
                        orderNumber = o.OrderNumber,
                        productId = o.ProductId,
                        productName = o.Products != null ? o.Products.Name : "",
                        plannedQuantity = o.PlannedQuantity,
                        unit = o.Unit,
                        status = o.Status,
                        plannedStartDate = o.PlannedStartDate,
                        createdBy = o.CreatedBy,
                        createdAt = o.CreatedAt,
                        batchesCount = _context.ProductionBatches.Count(b => b.OrderId == o.Id)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = orders,
                    pagination = BuildPagination(page, pageSize, totalCount),
                    message = "Список производственных заказов получен"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения заказов: " + ex.Message);
            }
        }

        // GET: api/orders/5
        [HttpGet]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> GetOrder(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор заказа");

                var order = await _context.ProductionOrders
                    .Include(o => o.Products)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                    return NotFoundMessage("Заказ не найден");

                var batches = await _context.ProductionBatches
                    .Where(b => b.OrderId == id)
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => new
                    {
                        id = b.Id,
                        batchNumber = b.BatchNumber,
                        productId = b.ProductId,
                        line = b.Line,
                        status = b.Status,
                        labStatus = b.LabStatus,
                        startedAt = b.StartedAt,
                        finishedAt = b.FinishedAt,
                        createdAt = b.CreatedAt
                    })
                    .ToListAsync();

                var result = new
                {
                    id = order.Id,
                    orderNumber = order.OrderNumber,
                    productId = order.ProductId,
                    productName = order.Products != null ? order.Products.Name : "",
                    plannedQuantity = order.PlannedQuantity,
                    unit = order.Unit,
                    status = order.Status,
                    plannedStartDate = order.PlannedStartDate,
                    createdAt = order.CreatedAt,
                    createdBy = order.CreatedBy,
                    batches
                };

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "Производственный заказ получен"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения заказа: " + ex.Message);
            }
        }

        // POST: api/orders
        [HttpPost]
        [Route("")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequestMessage("Тело запроса пустое");

                if (dto.ProductId <= 0)
                    return BadRequestMessage("Некорректный идентификатор продукта");

                if (dto.PlannedQuantity <= 0)
                    return BadRequestMessage("Плановое количество должно быть больше 0");

                string unit = string.IsNullOrWhiteSpace(dto.Unit) ? "кг" : dto.Unit.Trim();

                var product = await _context.Products.FindAsync(dto.ProductId);
                if (product == null)
                    return NotFoundMessage("Продукт не найден");

                if (product.Status != "Активен")
                    return BadRequestMessage("Нельзя создать заказ по неактивному продукту");

                int userId = GetCurrentUserId();

                string orderNumber = "PO-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" +
                                     Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

                var order = new ProductionOrders
                {
                    OrderNumber = orderNumber,
                    ProductId = dto.ProductId,
                    PlannedQuantity = dto.PlannedQuantity,
                    Unit = unit,
                    Status = "Создан",
                    PlannedStartDate = dto.PlannedStartDate,
                    CreatedBy = userId,
                    CreatedAt = DateTime.Now
                };

                _context.ProductionOrders.Add(order);

                AddAudit(
                    userId,
                    "Создание производственного заказа",
                    "ProductionOrder",
                    0,
                    null,
                    orderNumber
                );

                await _context.SaveChangesAsync();

                var result = new
                {
                    id = order.Id,
                    orderNumber = order.OrderNumber,
                    productId = order.ProductId,
                    productName = product.Name,
                    plannedQuantity = order.PlannedQuantity,
                    unit = order.Unit,
                    status = order.Status,
                    plannedStartDate = order.PlannedStartDate,
                    createdAt = order.CreatedAt
                };

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "Заказ создан"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка создания заказа: " + ex.Message);
            }
        }

        // PUT: api/orders/5
        [HttpPut]
        [Route("{id:int}")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> UpdateOrder(int id, [FromBody] UpdateOrderDto dto)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор заказа");

                if (dto == null)
                    return BadRequestMessage("Тело запроса пустое");

                var order = await _context.ProductionOrders.FindAsync(id);
                if (order == null)
                    return NotFoundMessage("Заказ не найден");

                if (IsFinalOrStartedOrderStatus(order.Status))
                    return BadRequestMessage("Нельзя изменить заказ в статусе: " + order.Status);

                string oldValue = "Количество: " + order.PlannedQuantity +
                                  ", Дата запуска: " + order.PlannedStartDate;

                if (dto.PlannedQuantity.HasValue)
                {
                    if (dto.PlannedQuantity.Value <= 0)
                        return BadRequestMessage("Плановое количество должно быть больше 0");

                    order.PlannedQuantity = dto.PlannedQuantity.Value;
                }

                if (dto.PlannedStartDate.HasValue)
                    order.PlannedStartDate = dto.PlannedStartDate.Value;

                if (!string.IsNullOrWhiteSpace(dto.Unit))
                    order.Unit = dto.Unit.Trim();

                string newValue = "Количество: " + order.PlannedQuantity +
                                  ", Дата запуска: " + order.PlannedStartDate;

                AddAudit(
                    GetCurrentUserId(),
                    "Изменение производственного заказа",
                    "ProductionOrder",
                    order.Id,
                    oldValue,
                    newValue
                );

                await _context.SaveChangesAsync();

                var result = new
                {
                    id = order.Id,
                    orderNumber = order.OrderNumber,
                    plannedQuantity = order.PlannedQuantity,
                    unit = order.Unit,
                    status = order.Status,
                    plannedStartDate = order.PlannedStartDate
                };

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "Заказ обновлён"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка обновления заказа: " + ex.Message);
            }
        }

        // POST: api/orders/5/start
        [HttpPost]
        [Route("{id:int}/start")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> StartOrder(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор заказа");

                var order = await _context.ProductionOrders.FindAsync(id);
                if (order == null)
                    return NotFoundMessage("Заказ не найден");

                if (order.Status != "Создан" && order.Status != "Черновик")
                    return BadRequestMessage("Можно запустить только заказ в статусе Создан");

                var activeRecipe = await _context.Recipes
                    .Where(r => r.ProductId == order.ProductId && r.Status == "Утверждена")
                    .OrderByDescending(r => r.ApprovedAt)
                    .ThenByDescending(r => r.Id)
                    .FirstOrDefaultAsync();

                var activeTechCard = await _context.TechCards
                    .Where(tc => tc.ProductId == order.ProductId && tc.Status == "Утверждена")
                    .OrderByDescending(tc => tc.ApprovedAt)
                    .ThenByDescending(tc => tc.Id)
                    .FirstOrDefaultAsync();

                if (activeRecipe == null)
                    return BadRequestMessage("Невозможно запустить заказ: для продукта нет утверждённой рецептуры");

                if (activeTechCard == null)
                    return BadRequestMessage("Невозможно запустить заказ: для продукта нет утверждённой технологической карты");

                bool hasSteps = await _context.TechSteps.AnyAsync(s => s.TechCardId == activeTechCard.Id);
                if (!hasSteps)
                    return BadRequestMessage("Невозможно запустить заказ: технологическая карта не содержит шагов");

                string oldStatus = order.Status;
                order.Status = "В работе";

                AddAudit(
                    GetCurrentUserId(),
                    "Запуск производственного заказа",
                    "ProductionOrder",
                    order.Id,
                    oldStatus,
                    order.Status
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = order.Id,
                        orderNumber = order.OrderNumber,
                        status = order.Status,
                        recipeId = activeRecipe.Id,
                        techCardId = activeTechCard.Id
                    },
                    message = "Заказ запущен в работу"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка запуска заказа: " + ex.Message);
            }
        }

        // POST: api/orders/5/batches
        [HttpPost]
        [Route("{id:int}/batches")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> CreateBatchFromOrder(int id, [FromBody] CreateBatchFromOrderDto dto)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор заказа");

                if (dto == null)
                    return BadRequestMessage("Тело запроса пустое");

                if (string.IsNullOrWhiteSpace(dto.Line))
                    return BadRequestMessage("Линия производства обязательна");

                var order = await _context.ProductionOrders.FindAsync(id);
                if (order == null)
                    return NotFoundMessage("Заказ не найден");

                if (order.Status != "В работе")
                    return BadRequestMessage("Создать партию можно только для заказа в статусе В работе");

                var recipe = await _context.Recipes
                    .Where(r => r.ProductId == order.ProductId && r.Status == "Утверждена")
                    .OrderByDescending(r => r.ApprovedAt)
                    .ThenByDescending(r => r.Id)
                    .FirstOrDefaultAsync();

                var techCard = await _context.TechCards
                    .Where(tc => tc.ProductId == order.ProductId && tc.Status == "Утверждена")
                    .OrderByDescending(tc => tc.ApprovedAt)
                    .ThenByDescending(tc => tc.Id)
                    .FirstOrDefaultAsync();

                if (recipe == null)
                    return BadRequestMessage("Для продукта нет утверждённой рецептуры");

                if (techCard == null)
                    return BadRequestMessage("Для продукта нет утверждённой технологической карты");

                var techSteps = await _context.TechSteps
                    .Where(s => s.TechCardId == techCard.Id)
                    .OrderBy(s => s.StepNumber)
                    .ToListAsync();

                if (!techSteps.Any())
                    return BadRequestMessage("Технологическая карта не содержит шагов");

                int userId = GetCurrentUserId();

                string batchNumber = "B-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" +
                                     Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

                var batch = new ProductionBatches
                {
                    BatchNumber = batchNumber,
                    OrderId = order.Id,
                    ProductId = order.ProductId,
                    RecipeId = recipe.Id,
                    TechCardId = techCard.Id,
                    Line = dto.Line.Trim(),
                    Status = "Подготовлена",
                    LabStatus = "Ожидает",
                    StartedAt = null,
                    FinishedAt = null,
                    CreatedBy = userId,
                    CreatedAt = DateTime.Now,
                    EquipmentId = dto.EquipmentId
                };

                _context.ProductionBatches.Add(batch);
                await _context.SaveChangesAsync();

                foreach (var step in techSteps)
                {
                    _context.BatchStepExecutions.Add(new BatchStepExecutions
                    {
                        BatchId = batch.Id,
                        StepId = step.Id,
                        StepNumber = step.StepNumber,
                        Status = "Не начат",
                        StartedAt = null,
                        FinishedAt = null,
                        StartedBy = null,
                        FinishedBy = null,
                        ActualParams = null,
                        EquipmentId = step.EquipmentId
                    });
                }

                AddAudit(
                    userId,
                    "Создание производственной партии из заказа",
                    "ProductionBatch",
                    batch.Id,
                    null,
                    batch.BatchNumber
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        batchId = batch.Id,
                        batchNumber = batch.BatchNumber,
                        orderId = order.Id,
                        orderNumber = order.OrderNumber,
                        productId = batch.ProductId,
                        recipeId = batch.RecipeId,
                        techCardId = batch.TechCardId,
                        line = batch.Line,
                        status = batch.Status,
                        labStatus = batch.LabStatus,
                        stepsCreated = techSteps.Count
                    },
                    message = "Производственная партия создана"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка создания производственной партии: " + ex.Message);
            }
        }

        // DELETE: api/orders/5
        [HttpDelete]
        [Route("{id:int}")]
        [Authorize(Roles = "Администратор")]
        public async Task<IHttpActionResult> CancelOrder(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор заказа");

                var order = await _context.ProductionOrders.FindAsync(id);
                if (order == null)
                    return NotFoundMessage("Заказ не найден");

                if (order.Status == "Завершён" || order.Status == "Завершен")
                    return BadRequestMessage("Нельзя отменить завершённый заказ");

                bool hasActiveBatches = await _context.ProductionBatches.AnyAsync(b =>
                    b.OrderId == order.Id &&
                    (b.Status == "В работе" || b.Status == "Подготовлена"));

                if (hasActiveBatches)
                    return BadRequestMessage("Нельзя отменить заказ: по нему есть активные производственные партии");

                string oldStatus = order.Status;
                order.Status = "Отменён";

                AddAudit(
                    GetCurrentUserId(),
                    "Отмена производственного заказа",
                    "ProductionOrder",
                    order.Id,
                    oldStatus,
                    order.Status
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = order.Id,
                        orderNumber = order.OrderNumber,
                        status = order.Status
                    },
                    message = "Заказ отменён"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка отмены заказа: " + ex.Message);
            }
        }

        private bool IsFinalOrStartedOrderStatus(string status)
        {
            return status == "В работе" ||
                   status == "Завершён" ||
                   status == "Завершен" ||
                   status == "Отменён" ||
                   status == "Отменен";
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
        // GET: api/orders/batches?page=1&pageSize=20
        [HttpGet]
        [Route("batches")]
        public async Task<IHttpActionResult> GetBatches(int page = 1, int pageSize = 20)
        {
            try
            {
                if (page < 1 || pageSize < 1 || pageSize > 100)
                    return Content(HttpStatusCode.BadRequest, ApiResponse<object>.Fail("Некорректные параметры пагинации"));

                var query = _context.ProductionBatches.AsQueryable();

                int totalCount = await query.CountAsync();

                var batches = await query
                    .OrderByDescending(b => b.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => new
                    {
                        b.Id,
                        b.BatchNumber,
                        b.OrderId,
                        b.ProductId,
                        ProductName = b.Products.Name,
                        b.Line,
                        b.Status,
                        b.LabStatus,
                        b.StartedAt,
                        b.FinishedAt,
                        b.CreatedAt
                    })
                    .ToListAsync();

                return Ok(ApiResponse<object>.Ok(
                    batches,
                    "Список производственных партий получен",
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
                return Content(HttpStatusCode.InternalServerError,
                    ApiResponse<object>.Fail("Ошибка получения партий: " + ex.Message));
            }
        }

        private IHttpActionResult BadRequestMessage(string message)
        {
            return Content(HttpStatusCode.BadRequest, ApiResponse<object>.Fail(message));
        }

        private IHttpActionResult NotFoundMessage(string message)
        {
            return Content(HttpStatusCode.BadRequest, ApiResponse<object>.Fail(message));
        }

        private IHttpActionResult UnauthorizedMessage(string message)
        {
            return Content(HttpStatusCode.BadRequest, ApiResponse<object>.Fail(message));
        }

        private IHttpActionResult ServerError(string message)
        {
            return Content(HttpStatusCode.BadRequest, ApiResponse<object>.Fail(message));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _context.Dispose();

            base.Dispose(disposing);
        }
    }

    public class CreateOrderDto
    {
        public int ProductId { get; set; }
        public decimal PlannedQuantity { get; set; }
        public string Unit { get; set; }
        public DateTime? PlannedStartDate { get; set; }
    }

    public class UpdateOrderDto
    {
        public decimal? PlannedQuantity { get; set; }
        public string Unit { get; set; }
        public DateTime? PlannedStartDate { get; set; }
    }

    public class CreateBatchFromOrderDto
    {
        public string Line { get; set; }
        public int? EquipmentId { get; set; }
    }
}