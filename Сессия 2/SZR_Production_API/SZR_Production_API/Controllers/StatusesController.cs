using System.Threading.Tasks;
using System.Web.Http;
using SZR_Production_API.Models;

namespace SZR_Production_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/statuses")]
    public class StatusesController : ApiController
    {
        private readonly SZR_ProductionEntities2 _context;

        public StatusesController()
        {
            _context = new SZR_ProductionEntities2();
        }

        // GET: api/statuses/batch
        [HttpGet]
        [Route("batch")]
        public IHttpActionResult GetBatchStatuses()
        {
            var statuses = new[]
            {
                new { value = "Подготовлена", label = "Подготовлена" },
                new { value = "В работе", label = "В работе" },
                new { value = "Ожидает контроля", label = "Ожидает контроля" },
                new { value = "Завершена", label = "Завершена" },
                new { value = "Заблокирована", label = "Заблокирована" }
            };

            return Ok(new { success = true, data = statuses });
        }

        // GET: api/statuses/step
        [HttpGet]
        [Route("step")]
        public IHttpActionResult GetStepStatuses()
        {
            var statuses = new[]
            {
                new { value = "Не начат", label = "Не начат" },
                new { value = "Выполняется", label = "Выполняется" },
                new { value = "Завершен", label = "Завершен" },
                new { value = "Пропущен", label = "Пропущен" }
            };

            return Ok(new { success = true, data = statuses });
        }

        // GET: api/statuses/lab
        [HttpGet]
        [Route("lab")]
        public IHttpActionResult GetLabStatuses()
        {
            var statuses = new[]
            {
                new { value = "Создано", label = "Создано" },
                new { value = "В работе", label = "В работе" },
                new { value = "Завершено", label = "Завершено" }
            };

            return Ok(new { success = true, data = statuses });
        }

        // GET: api/statuses/order
        [HttpGet]
        [Route("order")]
        public IHttpActionResult GetOrderStatuses()
        {
            var statuses = new[]
            {
                new { value = "Черновик", label = "Черновик" },
                new { value = "В работе", label = "В работе" },
                new { value = "Завершен", label = "Завершен" },
                new { value = "Отменен", label = "Отменен" }
            };

            return Ok(new { success = true, data = statuses });
        }

        // GET: api/statuses/recipe
        [HttpGet]
        [Route("recipe")]
        public IHttpActionResult GetRecipeStatuses()
        {
            var statuses = new[]
            {
                new { value = "Черновик", label = "Черновик" },
                new { value = "Утверждена", label = "Утверждена" },
                new { value = "Архив", label = "Архив" }
            };

            return Ok(new { success = true, data = statuses });
        }
    }
}