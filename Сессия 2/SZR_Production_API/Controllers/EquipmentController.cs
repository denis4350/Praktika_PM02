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
    [RoutePrefix("api/equipment")]
    public class EquipmentController : ApiController
    {
        private readonly SZR_ProductionEntities2 _context;

        public EquipmentController()
        {
            _context = new SZR_ProductionEntities2();
        }

        // GET: api/equipment?page=1&pageSize=20&line=Линия-1
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetEquipment(
    int page = 1, int pageSize = 20, string line = null)
        {
            try
            {
                // ... валидация page/pageSize через BadRequestMessage (уже возвращает ApiResponse<object>.Fail)

                var query = _context.Equipment.AsQueryable();
                // ... фильтрация

                int totalCount = await query.CountAsync();
                var equipment = await query
                    .OrderBy(e => e.Code)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(e => new
                    {
                        id = e.Id,
                        code = e.Code,
                        name = e.Name,
                        line = e.Line,
                        isActive = e.IsActive
                    })
                    .ToListAsync();

                return Ok(ApiResponse<object>.Ok(
                    equipment,
                    "Список оборудования получен",
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
                return ServerError("Ошибка получения списка оборудования: " + ex.Message);
            }
        }
        private IHttpActionResult ServerError(string message)
        {
            return Content(HttpStatusCode.InternalServerError, ApiResponse<object>.Fail(message));
        }

        // GET: api/equipment/lines
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
                    .OrderBy(line => line)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = lines,
                    message = "Список линий получен"
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = "Ошибка получения списка линий: " + ex.Message
                });
            }
        }

        // GET: api/equipment/5
        [HttpGet]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> GetEquipmentById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Некорректный идентификатор оборудования"
                    });
                }

                var equipment = await _context.Equipment
                    .Where(e => e.Id == id)
                    .Select(e => new
                    {
                        id = e.Id,
                        code = e.Code,
                        name = e.Name,
                        line = e.Line,
                        isActive = e.IsActive
                    })
                    .FirstOrDefaultAsync();

                if (equipment == null)
                {
                    return Content(HttpStatusCode.NotFound, new
                    {
                        success = false,
                        message = "Оборудование не найдено"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = equipment,
                    message = "Оборудование получено"
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = "Ошибка получения оборудования: " + ex.Message
                });
            }
        }

        // POST: api/equipment
        [HttpPost]
        [Route("")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> CreateEquipment([FromBody] CreateEquipmentDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Тело запроса пустое"
                    });
                }

                if (string.IsNullOrWhiteSpace(dto.Code))
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Код оборудования обязателен"
                    });
                }

                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Наименование оборудования обязательно"
                    });
                }

                string code = dto.Code.Trim();
                string name = dto.Name.Trim();
                string line = string.IsNullOrWhiteSpace(dto.Line) ? null : dto.Line.Trim();

                bool codeExists = await _context.Equipment.AnyAsync(e => e.Code == code);

                if (codeExists)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Оборудование с таким кодом уже существует"
                    });
                }

                var equipment = new Equipment
                {
                    Code = code,
                    Name = name,
                    Line = line,
                    IsActive = true
                };

                _context.Equipment.Add(equipment);
                await _context.SaveChangesAsync();

                var result = new
                {
                    id = equipment.Id,
                    code = equipment.Code,
                    name = equipment.Name,
                    line = equipment.Line,
                    isActive = equipment.IsActive
                };

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "Оборудование создано"
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = "Ошибка создания оборудования: " + ex.Message
                });
            }
        }

        // PUT: api/equipment/5
        [HttpPut]
        [Route("{id:int}")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> UpdateEquipment(int id, [FromBody] UpdateEquipmentDto dto)
        {
            try
            {
                if (id <= 0)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Некорректный идентификатор оборудования"
                    });
                }

                if (dto == null)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Тело запроса пустое"
                    });
                }

                var equipment = await _context.Equipment.FindAsync(id);

                if (equipment == null)
                {
                    return Content(HttpStatusCode.NotFound, new
                    {
                        success = false,
                        message = "Оборудование не найдено"
                    });
                }

                if (!string.IsNullOrWhiteSpace(dto.Name))
                {
                    equipment.Name = dto.Name.Trim();
                }

                if (dto.Line != null)
                {
                    equipment.Line = string.IsNullOrWhiteSpace(dto.Line) ? null : dto.Line.Trim();
                }

                if (dto.IsActive.HasValue)
                {
                    equipment.IsActive = dto.IsActive.Value;
                }

                await _context.SaveChangesAsync();

                var result = new
                {
                    id = equipment.Id,
                    code = equipment.Code,
                    name = equipment.Name,
                    line = equipment.Line,
                    isActive = equipment.IsActive
                };

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "Оборудование обновлено"
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = "Ошибка обновления оборудования: " + ex.Message
                });
            }
        }

        // DELETE: api/equipment/5
        [HttpDelete]
        [Route("{id:int}")]
        [Authorize(Roles = "Администратор")]
        public async Task<IHttpActionResult> DeleteEquipment(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Некорректный идентификатор оборудования"
                    });
                }

                var equipment = await _context.Equipment.FindAsync(id);

                if (equipment == null)
                {
                    return Content(HttpStatusCode.NotFound, new
                    {
                        success = false,
                        message = "Оборудование не найдено"
                    });
                }

                equipment.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = equipment.Id,
                        code = equipment.Code,
                        isActive = equipment.IsActive
                    },
                    message = "Оборудование архивировано"
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = "Ошибка архивирования оборудования: " + ex.Message
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

    public class CreateEquipmentDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Line { get; set; }
    }

    public class UpdateEquipmentDto
    {
        public string Name { get; set; }
        public string Line { get; set; }
        public bool? IsActive { get; set; }
    }
}