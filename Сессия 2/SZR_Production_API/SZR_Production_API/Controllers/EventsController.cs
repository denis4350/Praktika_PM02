using System;
using System.Data.Entity;
using System.Linq;
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

        // GET: api/events
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
            var query = _context.DeviationEvents.AsQueryable();

            if (!string.IsNullOrEmpty(eventType))
                query = query.Where(e => e.EventType == eventType);

            if (!string.IsNullOrEmpty(severity))
                query = query.Where(e => e.Severity == severity);

            if (from.HasValue)
                query = query.Where(e => e.CreatedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(e => e.CreatedAt <= to.Value);

            var totalCount = await query.CountAsync();

            var eventsList = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Получаем BatchNumber отдельно
            var batchIds = eventsList.Select(e => e.BatchId).Distinct().ToList();
            var batches = await _context.ProductionBatches
                .Where(b => batchIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.BatchNumber);

            var events = eventsList.Select(e => new
            {
                e.Id,
                e.EventType,
                e.ParameterName,
                e.PlannedValue,
                e.ActualValue,
                e.Severity,
                e.Description,
                e.CreatedAt,
                BatchNumber = batches.ContainsKey(e.BatchId) ? batches[e.BatchId] : ""
            }).ToList();

            return Ok(new
            {
                success = true,
                data = events,
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        // GET: api/events/batch/{batchId}
        [HttpGet]
        [Route("batch/{batchId}")]
        public async Task<IHttpActionResult> GetEventsByBatch(int batchId, int page = 1, int pageSize = 20)
        {
            var query = _context.DeviationEvents.Where(e => e.BatchId == batchId);

            var totalCount = await query.CountAsync();
            var events = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new
                {
                    e.Id,
                    e.EventType,
                    e.ParameterName,
                    e.PlannedValue,
                    e.ActualValue,
                    e.Severity,
                    e.Description,
                    e.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = events,
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        // GET: api/events/latest
        [HttpGet]
        [Route("latest")]
        public async Task<IHttpActionResult> GetLatestEvents(int limit = 10)
        {
            var eventsList = await _context.DeviationEvents
                .OrderByDescending(e => e.CreatedAt)
                .Take(limit)
                .ToListAsync();

            var batchIds = eventsList.Select(e => e.BatchId).Distinct().ToList();
            var batches = await _context.ProductionBatches
                .Where(b => batchIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.BatchNumber);

            var events = eventsList.Select(e => new
            {
                e.Id,
                e.EventType,
                e.Severity,
                e.Description,
                e.CreatedAt,
                BatchNumber = batches.ContainsKey(e.BatchId) ? batches[e.BatchId] : ""
            }).ToList();

            return Ok(new { success = true, data = events });
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