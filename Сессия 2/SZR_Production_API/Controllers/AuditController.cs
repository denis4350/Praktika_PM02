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
    [RoutePrefix("api/audit")]
    public class AuditController : ApiController
    {
        private readonly SZR_ProductionEntities2 _context;

        public AuditController()
        {
            _context = new SZR_ProductionEntities2();
        }

        // GET: api/audit/batch-history?batchId=1&batchType=product&page=1&pageSize=20
        [HttpGet]
        [Route("batch-history")]
        public async Task<IHttpActionResult> GetBatchHistory(
            int batchId,
            string batchType,
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                if (batchId <= 0)
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.Fail("Некорректный идентификатор партии."));
                }

                if (string.IsNullOrWhiteSpace(batchType))
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.Fail("Не указан тип партии. Допустимые значения: rawmaterial, product."));
                }

                if (page < 1)
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.Fail("Номер страницы должен быть больше 0."));
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.Fail("Размер страницы должен быть от 1 до 100."));
                }

                var normalizedBatchType = batchType.Trim().ToLowerInvariant();

                if (normalizedBatchType != "rawmaterial" && normalizedBatchType != "product")
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.Fail("batchType должен быть rawmaterial или product."));
                }

                string entityType = normalizedBatchType == "rawmaterial"
                    ? "RawMaterialBatch"
                    : "ProductionBatch";

                var query = _context.AuditLogs
                    .Where(a => a.EntityType == entityType && a.EntityId == batchId);

                int totalCount = await query.CountAsync();

                var items = await query
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Join(
                        _context.Users,
                        audit => audit.UserId,
                        user => user.Id,
                        (audit, user) => new
                        {
                            id = audit.Id,
                            userId = audit.UserId,
                            userName = user.FullName,
                            action = audit.Action,
                            entityType = audit.EntityType,
                            entityId = audit.EntityId,
                            oldValue = audit.OldValue,
                            newValue = audit.NewValue,
                            createdAt = audit.CreatedAt,
                            ipAddress = audit.IpAddress
                        })
                    .ToListAsync();

                var pagination = new PaginationInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(ApiResponse<object>.Ok(items, "История партии успешно получена.", pagination));
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    ApiResponse<object>.Fail("Ошибка при получении истории партии: " + ex.Message));
            }
        }

        // GET: api/audit/entity-history?entityType=ProductionBatch&entityId=1&page=1&pageSize=20
        [HttpGet]
        [Route("entity-history")]
        public async Task<IHttpActionResult> GetEntityHistory(
            string entityType,
            int entityId,
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(entityType))
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.Fail("Не указан тип сущности."));
                }

                if (entityId <= 0)
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.Fail("Некорректный идентификатор сущности."));
                }

                if (page < 1)
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.Fail("Номер страницы должен быть больше 0."));
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.Fail("Размер страницы должен быть от 1 до 100."));
                }

                string normalizedEntityType = entityType.Trim();

                var query = _context.AuditLogs
                    .Where(a => a.EntityType == normalizedEntityType && a.EntityId == entityId);

                int totalCount = await query.CountAsync();

                var items = await query
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Join(
                        _context.Users,
                        audit => audit.UserId,
                        user => user.Id,
                        (audit, user) => new
                        {
                            id = audit.Id,
                            userId = audit.UserId,
                            userName = user.FullName,
                            action = audit.Action,
                            entityType = audit.EntityType,
                            entityId = audit.EntityId,
                            oldValue = audit.OldValue,
                            newValue = audit.NewValue,
                            createdAt = audit.CreatedAt,
                            ipAddress = audit.IpAddress
                        })
                    .ToListAsync();

                var pagination = new PaginationInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(ApiResponse<object>.Ok(items, "История сущности успешно получена.", pagination));
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    ApiResponse<object>.Fail("Ошибка при получении истории сущности: " + ex.Message));
            }
        }

        // POST: api/audit/add
        [HttpPost]
        [Route("add")]
        public async Task<IHttpActionResult> AddAuditLog([FromBody] AuditLogDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.Fail("Тело запроса пустое."));
                }

                if (dto.UserId <= 0)
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.Fail("Некорректный UserId."));
                }

                if (string.IsNullOrWhiteSpace(dto.Action))
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.Fail("Поле Action обязательно."));
                }

                if (string.IsNullOrWhiteSpace(dto.EntityType))
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.Fail("Поле EntityType обязательно."));
                }

                if (dto.EntityId <= 0)
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.Fail("Некорректный EntityId."));
                }

                bool userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);

                if (!userExists)
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.Fail("Пользователь с указанным UserId не найден."));
                }

                var auditLog = new AuditLogs
                {
                    UserId = dto.UserId,
                    Action = dto.Action.Trim(),
                    EntityType = dto.EntityType.Trim(),
                    EntityId = dto.EntityId,
                    OldValue = dto.OldValue,
                    NewValue = dto.NewValue,
                    CreatedAt = DateTime.Now,
                    IpAddress = dto.IpAddress
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                var result = new
                {
                    id = auditLog.Id,
                    auditLog.UserId,
                    auditLog.Action,
                    auditLog.EntityType,
                    auditLog.EntityId,
                    auditLog.CreatedAt
                };

                return Ok(ApiResponse<object>.Ok(result, "Запись аудита успешно добавлена."));
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    ApiResponse<object>.Fail("Ошибка при добавлении записи аудита: " + ex.Message));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    public class AuditLogDto
    {
        public int UserId { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public int EntityId { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string IpAddress { get; set; }
    }

   
}