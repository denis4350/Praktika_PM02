using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
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

        // GET: api/roles?page=1&pageSize=20
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetRoles(int page = 1, int pageSize = 20)
        {
            try
            {
                var validation = ValidatePagination(page, pageSize);
                if (validation != null)
                    return validation;

                var query = _context.Roles.AsQueryable();

                int totalCount = await query.CountAsync();

                var roles = await query
                    .OrderBy(r => r.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        id = r.Id,
                        name = r.Name,
                        description = r.Description,
                        userCount = _context.Users.Count(u => u.RoleId == r.Id && u.IsActive)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = roles,
                    pagination = BuildPagination(page, pageSize, totalCount),
                    message = "Список ролей получен"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения ролей: " + ex.Message);
            }
        }

        // GET: api/roles/5
        [HttpGet]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> GetRole(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор роли");

                var role = await _context.Roles
                    .Where(r => r.Id == id)
                    .Select(r => new
                    {
                        id = r.Id,
                        name = r.Name,
                        description = r.Description,
                        userCount = _context.Users.Count(u => u.RoleId == r.Id && u.IsActive)
                    })
                    .FirstOrDefaultAsync();

                if (role == null)
                    return NotFoundMessage("Роль не найдена");

                return Ok(new
                {
                    success = true,
                    data = role,
                    message = "Роль получена"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения роли: " + ex.Message);
            }
        }

        // GET: api/roles/5/users?page=1&pageSize=20
        [HttpGet]
        [Route("{id:int}/users")]
        public async Task<IHttpActionResult> GetUsersByRole(
            int id,
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор роли");

                var validation = ValidatePagination(page, pageSize);
                if (validation != null)
                    return validation;

                var role = await _context.Roles
                    .Where(r => r.Id == id)
                    .Select(r => new
                    {
                        id = r.Id,
                        name = r.Name,
                        description = r.Description
                    })
                    .FirstOrDefaultAsync();

                if (role == null)
                    return NotFoundMessage("Роль не найдена");

                var query = _context.Users
                    .Where(u => u.RoleId == id && u.IsActive);

                int totalCount = await query.CountAsync();

                var users = await query
                    .OrderBy(u => u.FullName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new
                    {
                        id = u.Id,
                        login = u.Login,
                        fullName = u.FullName,
                        department = u.Department,
                        isActive = u.IsActive,
                        createdAt = u.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        role,
                        users
                    },
                    pagination = BuildPagination(page, pageSize, totalCount),
                    message = "Пользователи роли получены"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения пользователей роли: " + ex.Message);
            }
        }

        // GET: api/roles/list
        [HttpGet]
        [Route("list")]
        public async Task<IHttpActionResult> GetRolesList()
        {
            try
            {
                var roles = await _context.Roles
                    .OrderBy(r => r.Name)
                    .Select(r => new
                    {
                        id = r.Id,
                        name = r.Name
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = roles,
                    message = "Список ролей для выбора получен"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения списка ролей: " + ex.Message);
            }
        }

        private IHttpActionResult ValidatePagination(int page, int pageSize)
        {
            if (page < 1)
            {
                return BadRequestMessage("Номер страницы должен быть больше 0");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequestMessage("Размер страницы должен быть от 1 до 100");
            }

            return null;
        }

        private object BuildPagination(int page, int pageSize, int totalCount)
        {
            return new
            {
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }

        private IHttpActionResult BadRequestMessage(string message)
        {
            return Content(HttpStatusCode.BadRequest, ApiResponse<object>.Fail(message)); ;
        }

        private IHttpActionResult NotFoundMessage(string message)
        {
            return Content(HttpStatusCode.BadRequest, ApiResponse<object>.Fail(message));
        }

        private IHttpActionResult ServerError(string message)
        {
            return Content(HttpStatusCode.BadRequest, ApiResponse<object>.Fail(message));
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
}