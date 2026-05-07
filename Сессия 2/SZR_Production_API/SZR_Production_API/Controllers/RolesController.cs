using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using SZR_Production_API.Models;

namespace SZR_Production_API.Controllers
{
    [Authorize(Roles = "Администратор")]
    [RoutePrefix("api/roles")]
    public class RolesController : ApiController
    {
        private readonly SZR_ProductionEntities2 _context;

        public RolesController()
        {
            _context = new SZR_ProductionEntities2();
        }

        // GET: api/roles
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetRoles()
        {
            var roles = await _context.Roles
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Description,
                    UserCount = _context.Users.Count(u => u.RoleId == r.Id && u.IsActive)
                })
                .ToListAsync();

            return Ok(new { success = true, data = roles });
        }

        // GET: api/roles/{id}
        [HttpGet]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> GetRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            return Ok(new { success = true, data = role });
        }

        // GET: api/roles/{id}/users
        [HttpGet]
        [Route("{id:int}/users")]
        public async Task<IHttpActionResult> GetUsersByRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var users = await _context.Users
                .Where(u => u.RoleId == id && u.IsActive)
                .Select(u => new
                {
                    u.Id,
                    u.Login,
                    u.FullName,
                    u.Department
                })
                .ToListAsync();

            return Ok(new { success = true, data = users });
        }
    }
}