using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using SZR_Production_API.Models;

namespace SZR_Production_API.Controllers
{
    [Authorize(Roles = "Технолог,Администратор")]
    [RoutePrefix("api/reports")]
    public class ReportsController : ApiController
    {
        private readonly SZR_ProductionEntities2 _context;

        public ReportsController()
        {
            _context = new SZR_ProductionEntities2();
        }

        // ================================================================
        // 1. Отчёт по производственным партиям за период
        // ================================================================
        [HttpGet]
        [Route("batches")]
        public async Task<IHttpActionResult> GetBatchesReport(
            DateTime from,
            DateTime to,
            string format = null)
        {
            try
            {
                if (from > to)
                    return BadRequestMessage("Дата начала не может быть больше даты окончания");

                var query = _context.ProductionBatches
                    .Where(b => b.CreatedAt >= from && b.CreatedAt <= to)
                    .Select(b => new BatchReportItem
                    {
                        BatchNumber = b.BatchNumber,
                        ProductName = b.Products.Name,
                        Date = b.CreatedAt,
                        Status = b.Status,
                        HasDeviations = _context.DeviationEvents.Any(d => d.BatchId == b.Id),
                        LabDecision = b.LabStatus
                    })
                    .OrderByDescending(b => b.Date);

                var data = await query.ToListAsync();

                if (format == "csv")
                    return ResponseMessage(CsvResult(data, "batches_report.csv"));

                return Ok(new
                {
                    success = true,
                    data,
                    message = "Отчёт по партиям сформирован"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка формирования отчёта по партиям: " + ex.Message);
            }
        }

        // ================================================================
        // 2. Отчёт по отклонениям за период
        // ================================================================
        [HttpGet]
        [Route("deviations")]
        public async Task<IHttpActionResult> GetDeviationsReport(
            DateTime from,
            DateTime to,
            string format = null)
        {
            try
            {
                if (from > to)
                    return BadRequestMessage("Дата начала не может быть больше даты окончания");

                var query = _context.DeviationEvents
                    .Where(d => d.CreatedAt >= from && d.CreatedAt <= to)
                    .Select(d => new DeviationReportItem
                    {
                        BatchNumber = d.ProductionBatches.BatchNumber,
                        StepNumber = d.StepExecutionId != null
                            ? (int?)d.BatchStepExecutions.StepNumber
                            : null,
                        ParameterName = d.ParameterName,
                        PlannedValue = d.PlannedValue,
                        ActualValue = d.ActualValue,
                        Severity = d.Severity,
                        CreatedAt = d.CreatedAt
                    })
                    .OrderByDescending(d => d.CreatedAt);

                var data = await query.ToListAsync();

                if (format == "csv")
                    return ResponseMessage(CsvResult(data, "batches_report.csv"));

                return Ok(new
                {
                    success = true,
                    data,
                    message = "Отчёт по отклонениям сформирован"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка формирования отчёта по отклонениям: " + ex.Message);
            }
        }

        // ================================================================
        // 3. Отчёт по использованию рецептур за период
        // ================================================================
        [HttpGet]
        [Route("recipe-usage")]
        public async Task<IHttpActionResult> GetRecipeUsageReport(
    DateTime from,
    DateTime to,
    string format = null)
        {
            try
            {
                if (from > to)
                    return BadRequestMessage("Дата начала не может быть больше даты окончания");

                // Группируем партии по продукту и рецептуре, считаем количество партий и сумму планового количества заказов (если есть)
                var query = _context.ProductionBatches
                    .Where(b => b.CreatedAt >= from && b.CreatedAt <= to && b.OrderId != null)
                    .GroupBy(b => new { b.ProductId, b.RecipeId, b.OrderId })
                    .Select(g => new
                    {
                        ProductId = g.Key.ProductId,
                        RecipeId = g.Key.RecipeId,
                        BatchCount = g.Count(),
                        PlannedQuantity = g.Key.OrderId != null ? _context.ProductionOrders.Where(o => o.Id == g.Key.OrderId).Select(o => o.PlannedQuantity).FirstOrDefault() : 0
                    })
                    .GroupBy(x => new { x.ProductId, x.RecipeId })
                    .Select(g => new RecipeUsageReportItem
                    {
                        ProductName = g.Key.ProductId != null ? _context.Products.Where(p => p.Id == g.Key.ProductId).Select(p => p.Name).FirstOrDefault() : "",
                        RecipeVersion = _context.Recipes.Where(r => r.Id == g.Key.RecipeId).Select(r => r.Version).FirstOrDefault(),
                        BatchCount = g.Sum(x => x.BatchCount),
                        TotalQuantity = g.Sum(x => x.PlannedQuantity)
                    });

                var data = await query.ToListAsync();

                if (format == "csv")
                    return ResponseMessage(CsvResult(data, "recipe_usage_report.csv"));

                return Ok(new { success = true, data, message = "Отчёт по использованию рецептур сформирован" });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка формирования отчёта: " + ex.Message);
            }
        }

        // ================================================================
        // 4. Отчёт по событиям экструдера (телеметрия) за период
        // ================================================================
        [HttpGet]
        [Route("extruder-events")]
        public async Task<IHttpActionResult> GetExtruderEventsReport(
            DateTime from,
            DateTime to,
            string format = null)
        {
            try
            {
                if (from > to)
                    return BadRequestMessage("Дата начала не может быть больше даты окончания");

                // Телеметрия связана по BatchNumber (строка), делаем JOIN через анонимный тип
                var query = _context.ExtruderTelemetry
                    .Where(t => t.Timestamp >= from && t.Timestamp <= to)
                    .Join(_context.ProductionBatches,
                        t => t.BatchNumber,
                        b => b.BatchNumber,
                        (t, b) => new ExtruderEventReportItem
                        {
                            BatchNumber = t.BatchNumber,
                            ZoneNumber = t.ZoneNumber,
                            Parameter = "Температура",
                            Value = t.CurrentTemperature,
                            Timestamp = t.Timestamp,
                            Deviation = t.Status != "Норма" ? "Да" : "Нет"
                        })
                    .OrderByDescending(e => e.Timestamp);

                var data = await query.ToListAsync();

                if (format == "csv")
                    return ResponseMessage(CsvResult(data, "batches_report.csv"));

                return Ok(new
                {
                    success = true,
                    data,
                    message = "Отчёт по событиям экструдера сформирован"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка формирования отчёта: " + ex.Message);
            }
        }

        // ================================================================
        // 5. Отчёт по лабораторным блокировкам за период
        // ================================================================
        [HttpGet]
        [Route("lab-blocks")]
        public async Task<IHttpActionResult> GetLabBlocksReport(
            DateTime from,
            DateTime to,
            string format = null)
        {
            try
            {
                if (from > to)
                    return BadRequestMessage("Дата начала не может быть больше даты окончания");

                // Блокировки сырья
                var rawBlocks = await _context.RawMaterialBatches
                    .Where(r => r.LabStatus == "Заблокирована" && r.DecisionAt >= from && r.DecisionAt <= to)
                    .Select(r => new LabBlockReportItem
                    {
                        BatchType = "Сырьё",
                        BatchNumber = r.BatchNumber,
                        ProductOrMaterial = r.RawMaterials.Name,
                        BlockDate = r.DecisionAt,
                        Reason = r.Comment,
                        Responsible = r.DecisionBy != null
                            ? _context.Users.Where(u => u.Id == r.DecisionBy).Select(u => u.FullName).FirstOrDefault()
                            : ""
                    })
                    .ToListAsync();

                // Блокировки готовой продукции
                var productBlocks = await _context.ProductionBatches
                    .Where(b => b.LabStatus == "Заблокирована" && b.FinishedAt >= from && b.FinishedAt <= to)
                    .Select(b => new LabBlockReportItem
                    {
                        BatchType = "Продукция",
                        BatchNumber = b.BatchNumber,
                        ProductOrMaterial = b.Products.Name,
                        BlockDate = b.FinishedAt,
                        Reason = b.LabStatus, // или брать из последнего комментария в LabTests
                        Responsible = ""
                    })
                    .ToListAsync();

                var data = rawBlocks.Concat(productBlocks).OrderByDescending(b => b.BlockDate).ToList();

                if (format == "csv")
                    return ResponseMessage(CsvResult(data, "batches_report.csv"));

                return Ok(new
                {
                    success = true,
                    data,
                    message = "Отчёт по лабораторным блокировкам сформирован"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка формирования отчёта: " + ex.Message);
            }
        }

        // ================================================================
        // Вспомогательные методы
        // ================================================================
        private IHttpActionResult BadRequestMessage(string message)
        {
            return Content(HttpStatusCode.BadRequest, ApiResponse<object>.Fail(message));
        }

        private IHttpActionResult ServerError(string message)
        {
            return Content(HttpStatusCode.InternalServerError, new { success = false, message });
        }

        /// <summary>
        /// Возвращает CSV-файл как HttpResponseMessage
        /// </summary>
        private HttpResponseMessage CsvResult<T>(System.Collections.Generic.List<T> data, string fileName)
        {
            var sb = new StringBuilder();

            // Заголовки из свойств первого элемента
            if (data.Any())
            {
                var props = typeof(T).GetProperties();
                sb.AppendLine(string.Join(";", props.Select(p => p.Name)));
                foreach (var item in data)
                {
                    var values = props.Select(p => p.GetValue(item)?.ToString()?.Replace(";", ",") ?? "");
                    sb.AppendLine(string.Join(";", values));
                }
            }

            var result = new HttpResponseMessage(HttpStatusCode.OK);
            result.Content = new StringContent(sb.ToString(), Encoding.UTF8, "text/csv");
            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = fileName
            };
            return result;
        }

        // ================================================================
        // DTO для отчётов
        // ================================================================
        public class BatchReportItem
        {
            public string BatchNumber { get; set; }
            public string ProductName { get; set; }
            public DateTime? Date { get; set; }
            public string Status { get; set; }
            public bool HasDeviations { get; set; }
            public string LabDecision { get; set; }
        }

        public class DeviationReportItem
        {
            public string BatchNumber { get; set; }
            public int? StepNumber { get; set; }
            public string ParameterName { get; set; }
            public string PlannedValue { get; set; }
            public string ActualValue { get; set; }
            public string Severity { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class RecipeUsageReportItem
        {
            public string ProductName { get; set; }
            public string RecipeVersion { get; set; }
            public int BatchCount { get; set; }
            public decimal? TotalQuantity { get; set; }
        }

        public class ExtruderEventReportItem
        {
            public string BatchNumber { get; set; }
            public int ZoneNumber { get; set; }
            public string Parameter { get; set; }
            public decimal Value { get; set; }
            public DateTime Timestamp { get; set; }
            public string Deviation { get; set; }
        }

        public class LabBlockReportItem
        {
            public string BatchType { get; set; } // "Сырьё" или "Продукция"
            public string BatchNumber { get; set; }
            public string ProductOrMaterial { get; set; }
            public DateTime? BlockDate { get; set; }
            public string Reason { get; set; }
            public string Responsible { get; set; }
        }
    }
}