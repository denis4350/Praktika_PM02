using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using SZR_Production_API.Models;

namespace SZR_Production_API.Controllers
{
    [Authorize(Roles = "Администратор")]
    [RoutePrefix("api/users")]
    public class UsersController : ApiController
    {
        private readonly SZR_ProductionEntities2 _context;

        public UsersController()
        {
            _context = new SZR_ProductionEntities2();
        }

        // GET: api/users
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetUsers(int page = 1, int pageSize = 20, string search = null)
        {
            var query = _context.Users.Include(u => u.Roles).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.Login.Contains(search) || u.FullName.Contains(search));
            }

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderBy(u => u.Login)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.Login,
                    u.FullName,
                    RoleName = u.Roles.Name,
                    u.Department,
                    u.IsActive,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = users,
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        // GET: api/users/{id}
        [HttpGet]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> GetUser(int id)
        {
            var user = await _context.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    user.Id,
                    user.Login,
                    user.FullName,
                    RoleName = user.Roles.Name,
                    user.RoleId,
                    user.Department,
                    user.IsActive,
                    user.CreatedAt
                }
            });
        }

        // PUT: api/users/{id}
        [HttpPut]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(dto.FullName))
                user.FullName = dto.FullName;

            if (!string.IsNullOrEmpty(dto.Department))
                user.Department = dto.Department;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Пользователь обновлен" });
        }

        // PUT: api/users/{id}/role
        [HttpPut]
        [Route("{id:int}/role")]
        public async Task<IHttpActionResult> ChangeUserRole(int id, [FromBody] ChangeRoleDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var role = await _context.Roles.FindAsync(dto.RoleId);
            if (role == null)
            {
                return BadRequest("Роль не найдена");
            }

            user.RoleId = dto.RoleId;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"Роль пользователя изменена на {role.Name}" });
        }

        // DELETE: api/users/{id}
        [HttpDelete]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Пользователь заблокирован" });
        }

        // PUT: api/users/{id}/activate
        [HttpPut]
        [Route("{id:int}/activate")]
        public async Task<IHttpActionResult> ActivateUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = true;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Пользователь активирован" });
        }
    }

    public class UpdateUserDto
    {
        public string FullName { get; set; }
        public string Department { get; set; }
    }

    public class ChangeRoleDto
    {
        public int RoleId { get; set; }
    }
}