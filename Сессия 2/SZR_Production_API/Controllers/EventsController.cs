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
    [RoutePrefix("api/events")]
    public class EventsController : ApiController
    {
        private readonly SZR_ProductionEntities2 _context;

        public EventsController()
        {
            _context = new SZR_ProductionEntities2();
        }

        // GET: api/events?page=1&pageSize=20&eventType=Отклонение параметра&severity=Критично&from=2026-05-01&to=2026-05-16
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetEvents(
            int page = 1,
            int pageSize = 20,
            string eventType = null,
            string severity = null,
            DateTime? from = null,
            DateTime? to = null)
        {
            try
            {
                if (page < 1)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Номер страницы должен быть больше 0"
                    });
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Размер страницы должен быть от 1 до 100"
                    });
                }

                if (from.HasValue && to.HasValue && from.Value > to.Value)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Дата начала периода не может быть больше даты окончания"
                    });
                }

                var query = _context.DeviationEvents.AsQueryable();

                if (!string.IsNullOrWhiteSpace(eventType))
                {
                    string normalizedEventType = eventType.Trim();
                    query = query.Where(e => e.EventType == normalizedEventType);
                }

                if (!string.IsNullOrWhiteSpace(severity))
                {
                    string normalizedSeverity = severity.Trim();
                    query = query.Where(e => e.Severity == normalizedSeverity);
                }

                if (from.HasValue)
                {
                    query = query.Where(e => e.CreatedAt >= from.Value);
                }

                if (to.HasValue)
                {
                    query = query.Where(e => e.CreatedAt <= to.Value);
                }

                int totalCount = await query.CountAsync();

                var events = await query
                    .OrderByDescending(e => e.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Join(
                        _context.ProductionBatches,
                        e => e.BatchId,
                        b => b.Id,
                        (e, b) => new
                        {
                            id = e.Id,
                            batchId = e.BatchId,
                            batchNumber = b.BatchNumber,
                            stepExecutionId = e.StepExecutionId,
                            eventType = e.EventType,
                            parameterName = e.ParameterName,
                            plannedValue = e.PlannedValue,
                            actualValue = e.ActualValue,
                            severity = e.Severity,
                            description = e.Description,
                            createdAt = e.CreatedAt,
                            createdBy = e.CreatedBy,
                            resolvedAt = e.ResolvedAt,
                            resolvedBy = e.ResolvedBy,
                            equipmentId = e.EquipmentId
                        })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = events,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    },
                    message = "Список событий получен"
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = "Ошибка получения событий: " + ex.Message
                });
            }
        }

        // GET: api/events/batch/5?page=1&pageSize=20
        [HttpGet]
        [Route("batch/{batchId:int}")]
        public async Task<IHttpActionResult> GetEventsByBatch(
            int batchId,
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                if (batchId <= 0)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Некорректный идентификатор партии"
                    });
                }

                if (page < 1)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Номер страницы должен быть больше 0"
                    });
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Размер страницы должен быть от 1 до 100"
                    });
                }

                bool batchExists = await _context.ProductionBatches.AnyAsync(b => b.Id == batchId);

                if (!batchExists)
                {
                    return Content(HttpStatusCode.NotFound, new
                    {
                        success = false,
                        message = "Производственная партия не найдена"
                    });
                }

                var query = _context.DeviationEvents
                    .Where(e => e.BatchId == batchId);

                int totalCount = await query.CountAsync();

                var events = await query
                    .OrderByDescending(e => e.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(e => new
                    {
                        id = e.Id,
                        batchId = e.BatchId,
                        stepExecutionId = e.StepExecutionId,
                        eventType = e.EventType,
                        parameterName = e.ParameterName,
                        plannedValue = e.PlannedValue,
                        actualValue = e.ActualValue,
                        severity = e.Severity,
                        description = e.Description,
                        createdAt = e.CreatedAt,
                        createdBy = e.CreatedBy,
                        resolvedAt = e.ResolvedAt,
                        resolvedBy = e.ResolvedBy,
                        equipmentId = e.EquipmentId
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = events,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    },
                    message = "События партии получены"
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = "Ошибка получения событий партии: " + ex.Message
                });
            }
        }

        // GET: api/events/latest?limit=10
        [HttpGet]
        [Route("latest")]
        public async Task<IHttpActionResult> GetLatestEvents(int limit = 10)
        {
            try
            {
                if (limit < 1 || limit > 50)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "limit должен быть от 1 до 50"
                    });
                }

                var events = await _context.DeviationEvents
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(limit)
                    .Join(
                        _context.ProductionBatches,
                        e => e.BatchId,
                        b => b.Id,
                        (e, b) => new
                        {
                            id = e.Id,
                            batchId = e.BatchId,
                            batchNumber = b.BatchNumber,
                            eventType = e.EventType,
                            severity = e.Severity,
                            description = e.Description,
                            createdAt = e.CreatedAt,
                            parameterName = e.ParameterName,
                            actualValue = e.ActualValue,
                            plannedValue = e.PlannedValue
                        })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = events,
                    message = "Последние события получены"
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = "Ошибка получения последних событий: " + ex.Message
                });
            }
        }

        // GET: api/events/critical?limit=10
        [HttpGet]
        [Route("critical")]
        public async Task<IHttpActionResult> GetCriticalEvents(int limit = 10)
        {
            try
            {
                if (limit < 1 || limit > 50)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "limit должен быть от 1 до 50"
                    });
                }

                var events = await _context.DeviationEvents
                    .Where(e => e.Severity == "Критично")
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(limit)
                    .Join(
                        _context.ProductionBatches,
                        e => e.BatchId,
                        b => b.Id,
                        (e, b) => new
                        {
                            id = e.Id,
                            batchId = e.BatchId,
                            batchNumber = b.BatchNumber,
                            eventType = e.EventType,
                            severity = e.Severity,
                            description = e.Description,
                            createdAt = e.CreatedAt,
                            parameterName = e.ParameterName,
                            actualValue = e.ActualValue,
                            plannedValue = e.PlannedValue
                        })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = events,
                    message = "Критические события получены"
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = "Ошибка получения критических событий: " + ex.Message
                });
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
}