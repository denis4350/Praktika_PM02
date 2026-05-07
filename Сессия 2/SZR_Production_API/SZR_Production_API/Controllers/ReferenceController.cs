using System.Data.Entity;
using System.Linq;
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
            // Добавлено .ToListAsync() после Select
            var products = await _context.Products
                .Select(p => new { p.Id, p.Code, p.Name })
                .ToListAsync();

            var rawMaterials = await _context.RawMaterials
                .Select(r => new { r.Id, r.Code, r.Name, r.Unit })
                .ToListAsync();

            var equipment = await _context.Equipment
                .Select(e => new { e.Id, e.Code, e.Name, e.Line })
                .ToListAsync();

            var roles = await _context.Roles
                .Select(r => new { r.Id, r.Name })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    products,
                    rawMaterials,
                    equipment,
                    roles
                }
            });
        }
    }
}