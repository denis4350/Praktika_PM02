using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using SZR_Production_API.Models;

namespace SZR_Production_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/raw-materials")]
    public class RawMaterialsController : ApiController
    {
        private readonly SZR_ProductionEntities2 _context;

        public RawMaterialsController()
        {
            _context = new SZR_ProductionEntities2();
        }

        // GET: api/raw-materials
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetRawMaterials(int page = 1, int pageSize = 20, string search = null)
        {
            var query = _context.RawMaterials.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => r.Name.Contains(search) || r.Code.Contains(search));
            }

            var totalCount = await query.CountAsync();
            var materials = await query
                .OrderBy(r => r.Code)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    r.Id,
                    r.Code,
                    r.Name,
                    r.Category,
                    r.Unit,
                    r.IsActive
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = materials,
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        // GET: api/raw-materials/{id}
        [HttpGet]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> GetRawMaterial(int id)
        {
            var material = await _context.RawMaterials.FindAsync(id);
            if (material == null)
            {
                return NotFound();
            }

            return Ok(new { success = true, data = material });
        }

        // POST: api/raw-materials
        [HttpPost]
        [Route("")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> CreateRawMaterial([FromBody] CreateRawMaterialDto dto)
        {
            var existing = await _context.RawMaterials.FirstOrDefaultAsync(r => r.Code == dto.Code);
            if (existing != null)
            {
                return BadRequest("Сырье с таким кодом уже существует");
            }

            var material = new RawMaterials
            {
                Code = dto.Code,
                Name = dto.Name,
                Category = dto.Category,
                Unit = dto.Unit,
                IsActive = true
            };

            _context.RawMaterials.Add(material);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, data = material, message = "Сырье создано" });
        }

        // PUT: api/raw-materials/{id}
        [HttpPut]
        [Route("{id:int}")]
        [Authorize(Roles = "Технолог,Администратор")]
        public async Task<IHttpActionResult> UpdateRawMaterial(int id, [FromBody] UpdateRawMaterialDto dto)
        {
            var material = await _context.RawMaterials.FindAsync(id);
            if (material == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(dto.Name))
                material.Name = dto.Name;

            if (!string.IsNullOrEmpty(dto.Category))
                material.Category = dto.Category;

            if (!string.IsNullOrEmpty(dto.Unit))
                material.Unit = dto.Unit;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, data = material, message = "Сырье обновлено" });
        }

        // DELETE: api/raw-materials/{id}
        [HttpDelete]
        [Route("{id:int}")]
        [Authorize(Roles = "Администратор")]
        public async Task<IHttpActionResult> DeleteRawMaterial(int id)
        {
            var material = await _context.RawMaterials.FindAsync(id);
            if (material == null)
            {
                return NotFound();
            }

            // Проверяем, используется ли сырье в рецептурах
            var isUsed = await _context.RecipeComponents.AnyAsync(c => c.RawMaterialId == id);
            if (isUsed)
            {
                return BadRequest("Невозможно удалить: сырье используется в рецептурах");
            }

            material.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Сырье деактивировано" });
        }
    }

    public class CreateRawMaterialDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Unit { get; set; }
    }

    public class UpdateRawMaterialDto
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Unit { get; set; }
    }
}