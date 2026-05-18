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
    [RoutePrefix("api/reference")]
    public class ReferenceController : ApiController
    {
        private readonly SZR_ProductionEntities2 _context;

        public ReferenceController()
        {
            _context = new SZR_ProductionEntities2();
        }

        // GET: api/reference/all
        [HttpGet]
        [Route("all")]
        public async Task<IHttpActionResult> GetAllReferences()
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.Status == "Активен")
                    .OrderBy(p => p.Code)
                    .Select(p => new
                    {
                        id = p.Id,
                        code = p.Code,
                        name = p.Name,
                        productType = p.ProductType,
                        form = p.Form
                    })
                    .ToListAsync();

                var rawMaterials = await _context.RawMaterials
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.Code)
                    .Select(r => new
                    {
                        id = r.Id,
                        code = r.Code,
                        name = r.Name,
                        category = r.Category,
                        unit = r.Unit
                    })
                    .ToListAsync();

                var equipment = await _context.Equipment
                    .Where(e => e.IsActive)
                    .OrderBy(e => e.Code)
                    .Select(e => new
                    {
                        id = e.Id,
                        code = e.Code,
                        name = e.Name,
                        line = e.Line
                    })
                    .ToListAsync();

                var roles = await _context.Roles
                    .OrderBy(r => r.Name)
                    .Select(r => new
                    {
                        id = r.Id,
                        name = r.Name,
                        description = r.Description
                    })
                    .ToListAsync();

                var lines = await _context.Equipment
                    .Where(e => e.IsActive && e.Line != null && e.Line != "")
                    .Select(e => e.Line)
                    .Distinct()
                    .OrderBy(l => l)
                    .ToListAsync();

                var rawMaterialCategories = await _context.RawMaterials
                    .Where(r => r.Category != null && r.Category != "")
                    .Select(r => r.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        products,
                        rawMaterials,
                        equipment,
                        roles,
                        lines,
                        rawMaterialCategories,
                        statuses = GetStatuses(),
                        dictionaries = GetDictionaries()
                    },
                    message = "Справочные данные получены",
                    pagination = (object)null
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения справочных данных: " + ex.Message);
            }
        }

        // GET: api/reference/products
        [HttpGet]
        [Route("products")]
        public async Task<IHttpActionResult> GetProducts()
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.Status == "Активен")
                    .OrderBy(p => p.Code)
                    .Select(p => new
                    {
                        id = p.Id,
                        code = p.Code,
                        name = p.Name,
                        productType = p.ProductType,
                        form = p.Form
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = products,
                    message = "Справочник продукции получен"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения продукции: " + ex.Message);
            }
        }

        // GET: api/reference/raw-materials
        [HttpGet]
        [Route("raw-materials")]
        public async Task<IHttpActionResult> GetRawMaterials()
        {
            try
            {
                var rawMaterials = await _context.RawMaterials
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.Code)
                    .Select(r => new
                    {
                        id = r.Id,
                        code = r.Code,
                        name = r.Name,
                        category = r.Category,
                        unit = r.Unit
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = rawMaterials,
                    message = "Справочник сырья получен"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения сырья: " + ex.Message);
            }
        }

        // GET: api/reference/equipment
        [HttpGet]
        [Route("equipment")]
        public async Task<IHttpActionResult> GetEquipment()
        {
            try
            {
                var equipment = await _context.Equipment
                    .Where(e => e.IsActive)
                    .OrderBy(e => e.Code)
                    .Select(e => new
                    {
                        id = e.Id,
                        code = e.Code,
                        name = e.Name,
                        line = e.Line
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = equipment,
                    message = "Справочник оборудования получен"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения оборудования: " + ex.Message);
            }
        }

        // GET: api/reference/roles
        [HttpGet]
        [Route("roles")]
        [Authorize(Roles = "Администратор")]
        public async Task<IHttpActionResult> GetRoles()
        {
            try
            {
                var roles = await _context.Roles
                    .OrderBy(r => r.Name)
                    .Select(r => new
                    {
                        id = r.Id,
                        name = r.Name,
                        description = r.Description
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = roles,
                    message = "Справочник ролей получен"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения ролей: " + ex.Message);
            }
        }

        // GET: api/reference/lines
        [HttpGet]
        [Route("lines")]
        public async Task<IHttpActionResult> GetLines()
        {
            try
            {
                var lines = await _context.Equipment
                    .Where(e => e.IsActive && e.Line != null && e.Line != "")
                    .Select(e => e.Line)
                    .Distinct()
                    .OrderBy(l => l)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = lines,
                    message = "Список производственных линий получен"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения линий: " + ex.Message);
            }
        }

        // GET: api/reference/statuses
        [HttpGet]
        [Route("statuses")]
        public IHttpActionResult GetReferenceStatuses()
        {
            return Ok(new
            {
                success = true,
                data = GetStatuses(),
                message = "Справочник статусов получен"
            });
        }

        // GET: api/reference/dictionaries
        [HttpGet]
        [Route("dictionaries")]
        public IHttpActionResult GetReferenceDictionaries()
        {
            return Ok(new
            {
                success = true,
                data = GetDictionaries(),
                message = "Вспомогательные справочники получены"
            });
        }

        private object GetStatuses()
        {
            return new
            {
                productStatuses = new[]
                {
                    "Активен",
                    "Архивирован"
                },

                orderStatuses = new[]
                {
                    "Создан",
                    "В работе",
                    "Завершён",
                    "Отменён"
                },

                recipeStatuses = new[]
                {
                    "Черновик",
                    "На согласовании",
                    "Утверждена",
                    "Архивирована"
                },

                techCardStatuses = new[]
                {
                    "Черновик",
                    "На согласовании",
                    "Утверждена",
                    "Архивирована"
                },

                batchStatuses = new[]
                {
                    "Подготовлена",
                    "В работе",
                    "Приостановлена",
                    "Завершена",
                    "Заблокирована",
                    "Отменена"
                },

                stepStatuses = new[]
                {
                    "Не начат",
                    "Выполняется",
                    "Завершён"
                },

                labStatuses = new[]
                {
                    "Ожидает",
                    "В работе",
                    "Разрешена",
                    "Заблокирована"
                },

                labTestStatuses = new[]
                {
                    "Создано",
                    "В работе",
                    "Завершено",
                    "Отменено"
                },

                deviationSeverity = new[]
                {
                    "Информация",
                    "Предупреждение",
                    "Критично"
                },

                extruderProgramStatuses = new[]
                {
                    "Черновик",
                    "Активна",
                    "Архивирована"
                }
            };
        }

        private object GetDictionaries()
        {
            return new
            {
                productTypes = new[]
                {
                    "Гербицид",
                    "Инсектицид",
                    "Фунгицид",
                    "Протравитель",
                    "Регулятор роста"
                },

                productForms = new[]
                {
                    "Гранулы",
                    "Порошок",
                    "Концентрат",
                    "Суспензия",
                    "Раствор"
                },

                materialCategories = new[]
                {
                    "Действующее вещество",
                    "Наполнитель",
                    "Стабилизатор",
                    "Связующее",
                    "Краситель",
                    "Упаковочный компонент"
                },

                units = new[]
                {
                    "кг",
                    "г",
                    "л",
                    "мл",
                    "шт"
                },

                labTestTypes = new[]
                {
                    "Входной контроль",
                    "Повторный анализ",
                    "Контроль готовой продукции",
                    "Контроль после уточнения данных"
                },

                labPriorities = new[]
                {
                    "Обычный",
                    "Высокий",
                    "Срочный"
                },

                labDecisions = new[]
                {
                    "Разрешена",
                    "Заблокирована"
                },

                techStepTypes = new[]
                {
                    "Подготовка",
                    "Загрузка сырья",
                    "Смешивание",
                    "Экструзия",
                    "Охлаждение",
                    "Фасовка",
                    "Контроль"
                },

                deviationTypes = new[]
                {
                    "Отклонение параметра",
                    "Проблема оператора",
                    "Сбой оборудования",
                    "Лабораторная блокировка",
                    "Комментарий технолога"
                }
            };
        }

        private IHttpActionResult ServerError(string message)
        {
            return Content(HttpStatusCode.InternalServerError, new
            {
                success = false,
                message
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _context.Dispose();

            base.Dispose(disposing);
        }
    }
}