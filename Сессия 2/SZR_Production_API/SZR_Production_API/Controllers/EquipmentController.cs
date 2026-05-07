using System;
using System.Data.Entity;
using System.Linq;
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

        // GET: api/equipment
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetEquipment(int page = 1, int pageSize = 20, string line = null)
        {
            var query = _context.Equipment.AsQueryable();

            if (!string.IsNullOrEmpty(line))
            {
                query = query.Where(e => e.Line == line);
            }

            var totalCount = await query.CountAsync();
            var equipment = await query
                .OrderBy(e => e.Code)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new
                {
                    e.Id,
                    e.Code,
                    e.Name,
                    e.Line,
                    e.IsActive
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = equipment,
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        // GET: api/equipment/lines
        [HttpGet]
        [Route("lines")]
        public async Task<IHttpActionResult> GetLines()
        {
            var lines = await _context.Equipment
                .Where(e => e.IsActive)
                .Select(e => e.Line)
                .Distinct()
                .ToListAsync();

            return Ok(new { success = true, data = lines });
        }

        // GET: api/equipment/{id}
        [HttpGet]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> GetEquipmentById(int id)
        {
            var equipment = await _context.Equipment.FindAsync(id);
            if (equipment == null)
            {
                return NotFound();
            }

            return Ok(new { success = true, data = equipment });
        }

        // POST: api/equipment
        [HttpPost]
        [Route("")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> CreateEquipment([FromBody] CreateEquipmentDto dto)
        {
            var existing = await _context.Equipment.FirstOrDefaultAsync(e => e.Code == dto.Code);
            if (existing != null)
            {
                return BadRequest("Оборудование с таким кодом уже существует");
            }

            var equipment = new Equipment
            {
                Code = dto.Code,
                Name = dto.Name,
                Line = dto.Line,
                IsActive = true
            };

            _context.Equipment.Add(equipment);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, data = equipment, message = "Оборудование создано" });
        }

        // PUT: api/equipment/{id}
        [HttpPut]
        [Route("{id:int}")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> UpdateEquipment(int id, [FromBody] UpdateEquipmentDto dto)
        {
            var equipment = await _context.Equipment.FindAsync(id);
            if (equipment == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(dto.Name))
                equipment.Name = dto.Name;

            if (!string.IsNullOrEmpty(dto.Line))
                equipment.Line = dto.Line;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, data = equipment, message = "Оборудование обновлено" });
        }

        // DELETE: api/equipment/{id}
        [HttpDelete]
        [Route("{id:int}")]
        [Authorize(Roles = "Администратор")]
        public async Task<IHttpActionResult> DeleteEquipment(int id)
        {
            var equipment = await _context.Equipment.FindAsync(id);
            if (equipment == null)
            {
                return NotFound();
            }

            equipment.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Оборудование деактивировано" });
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
    }
}