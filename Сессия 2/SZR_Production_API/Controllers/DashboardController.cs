using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using SZR_Production_API.Models;

namespace SZR_Production_API.Controllers
{
    [Authorize(Roles = "Технолог,Администратор")]
    [RoutePrefix("api/dashboard")]
    public class DashboardController : ApiController
    {
        private readonly SZR_ProductionEntities2 _context;

        public DashboardController()
        {
            _context = new SZR_ProductionEntities2();
        }

        // GET: api/dashboard
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetDashboardData()
        {
            try
            {
                // ---------- KPI ----------
                var activeProducts = await _context.Products.CountAsync(p => p.Status == "Активен");
                var activeRecipes = await _context.Recipes.CountAsync(r => r.Status == "Утверждена");
                var activeTechCards = await _context.TechCards.CountAsync(t => t.Status == "Утверждена");
                var ordersInProgress = await _context.ProductionOrders.CountAsync(o => o.Status == "В работе");
                var batchesInProduction = await _context.ProductionBatches
                    .CountAsync(b => b.Status == "В работе" || b.Status == "Подготовлена");

                var lastWeek = DateTime.Now.AddDays(-7);
                var batchesWithDeviations = await _context.DeviationEvents
                    .Where(d => d.CreatedAt >= lastWeek)
                    .Select(d => d.BatchId).Distinct().CountAsync();

                var batchesWaitingLab = await _context.ProductionBatches
                    .CountAsync(b => b.LabStatus == "Ожидает" || (b.LabStatus == null && b.Status == "Ожидает контроля"));

                var kpi = new
                {
                    ActiveProducts = activeProducts,
                    ActiveRecipes = activeRecipes,
                    ActiveTechCards = activeTechCards,
                    OrdersInProgress = ordersInProgress,
                    BatchesInProduction = batchesInProduction,
                    BatchesWithDeviations = batchesWithDeviations,
                    BatchesWaitingLab = batchesWaitingLab
                };

                // ---------- Лента последних событий ----------
                // Объединяем аудит-записи (статусные действия по партиям) и отклонения
                var auditEvents = await _context.AuditLogs
                    .Where(a => a.EntityType == "ProductionBatch" &&
                                (a.Action == "Создание производственной партии" ||
                                 a.Action.Contains("Начало выполнения шага") ||
                                 a.Action.Contains("Завершение выполнения шага") ||
                                 a.Action.Contains("Завершение производственной партии") ||
                                 a.Action.Contains("лабораторного решения")))
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(10)
                    .Select(a => new
                    {
                        Source = "Audit",
                        EventType = a.Action,
                        BatchNumber = _context.ProductionBatches
                            .Where(b => b.Id == a.EntityId)
                            .Select(b => b.BatchNumber)
                            .FirstOrDefault(),
                        Description = a.NewValue ?? a.Action,
                        Timestamp = a.CreatedAt,
                        Severity = "Info"
                    })
                    .ToListAsync();

                var deviationEvents = await _context.DeviationEvents
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(10)
                    .Select(d => new
                    {
                        Source = "Deviation",
                        EventType = d.EventType,
                        BatchNumber = d.ProductionBatches.BatchNumber,
                        Description = d.Description,
                        Timestamp = d.CreatedAt,
                        Severity = d.Severity
                    })
                    .ToListAsync();

                // Объединяем, сортируем по времени, берём последние 10
                var combinedEvents = auditEvents
                    .Concat(deviationEvents)
                    .OrderByDescending(e => e.Timestamp)
                    .Take(10)
                    .Select(e => new
                    {
                        e.EventType,
                        e.BatchNumber,
                        e.Description,
                        e.Timestamp,
                        e.Severity
                    })
                    .ToList();

                // ---------- Критические отклонения ----------
                var criticalDeviations = await _context.DeviationEvents
                    .Where(d => d.Severity == "Критично" && d.ResolvedAt == null)
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(5)
                    .Select(d => new
                    {
                        d.Id,
                        BatchNumber = d.ProductionBatches.BatchNumber,
                        d.ParameterName,
                        d.ActualValue,
                        d.PlannedValue,
                        d.Description,
                        OccurredAt = d.CreatedAt
                    })
                    .ToListAsync();

                // ---------- Партии на анализе (ожидают лабораторного решения) ----------
                var batchesForAnalysis = await _context.ProductionBatches
                    .Where(b => b.LabStatus == "Ожидает" || (b.LabStatus == null && b.Status == "Ожидает контроля"))
                    .OrderByDescending(b => b.FinishedAt)
                    .Take(5)
                    .Select(b => new
                    {
                        b.Id,
                        b.BatchNumber,
                        ProductName = b.Products.Name,
                        b.LabStatus,
                        b.FinishedAt,
                        DeviationCount = _context.DeviationEvents.Count(d => d.BatchId == b.Id)
                    })
                    .ToListAsync();

                // ---------- Формируем итоговый ответ ----------
                var dashboardData = new
                {
                    KPIs = kpi,
                    RecentEvents = combinedEvents,
                    CriticalDeviations = criticalDeviations,
                    BatchesForAnalysis = batchesForAnalysis
                };

                return Ok(ApiResponse<object>.Ok(dashboardData, "Данные для панели управления получены"));
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    ApiResponse<object>.Fail("Ошибка получения данных дашборда: " + ex.Message));
            }
        }
    }
}