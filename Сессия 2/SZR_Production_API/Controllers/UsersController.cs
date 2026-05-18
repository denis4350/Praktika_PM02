using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
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

        // GET: api/users?page=1&pageSize=20&search=&roleId=1&isActive=true
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetUsers(
            int page = 1,
            int pageSize = 20,
            string search = null,
            int? roleId = null,
            bool? isActive = null)
        {
            try
            {
                var validation = ValidatePagination(page, pageSize);
                if (validation != null)
                    return validation;

                var query = _context.Users
                    .Include(u => u.Roles)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string value = search.Trim();
                    query = query.Where(u =>
                        u.Login.Contains(value) ||
                        u.FullName.Contains(value) ||
                        u.Department.Contains(value));
                }

                if (roleId.HasValue)
                {
                    query = query.Where(u => u.RoleId == roleId.Value);
                }

                if (isActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == isActive.Value);
                }

                int totalCount = await query.CountAsync();

                var users = await query
                    .OrderBy(u => u.Login)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new
                    {
                        id = u.Id,
                        login = u.Login,
                        fullName = u.FullName,
                        roleId = u.RoleId,
                        roleName = u.Roles != null ? u.Roles.Name : "",
                        department = u.Department,
                        isActive = u.IsActive,
                        createdAt = u.CreatedAt,
                        hasAvatar = u.Avatar != null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = users,
                    pagination = BuildPagination(page, pageSize, totalCount),
                    message = "Список пользователей получен"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения пользователей: " + ex.Message);
            }
        }

        // GET: api/users/5
        [HttpGet]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> GetUser(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор пользователя");

                var user = await _context.Users
                    .Include(u => u.Roles)
                    .Where(u => u.Id == id)
                    .Select(u => new
                    {
                        id = u.Id,
                        login = u.Login,
                        fullName = u.FullName,
                        roleId = u.RoleId,
                        roleName = u.Roles != null ? u.Roles.Name : "",
                        department = u.Department,
                        isActive = u.IsActive,
                        createdAt = u.CreatedAt,
                        hasAvatar = u.Avatar != null
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                    return NotFoundMessage("Пользователь не найден");

                return Ok(new
                {
                    success = true,
                    data = user,
                    message = "Пользователь получен"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения пользователя: " + ex.Message);
            }
        }

        // PUT: api/users/5
        [HttpPut]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор пользователя");

                if (dto == null)
                    return BadRequestMessage("Тело запроса пустое");

                var user = await _context.Users.FindAsync(id);

                if (user == null)
                    return NotFoundMessage("Пользователь не найден");

                string oldValue = "ФИО: " + user.FullName + "; Подразделение: " + user.Department;

                if (!string.IsNullOrWhiteSpace(dto.FullName))
                    user.FullName = dto.FullName.Trim();

                if (dto.Department != null)
                    user.Department = string.IsNullOrWhiteSpace(dto.Department) ? null : dto.Department.Trim();

                string newValue = "ФИО: " + user.FullName + "; Подразделение: " + user.Department;

                AddAudit(
                    GetCurrentUserId(),
                    "Изменение пользователя",
                    "User",
                    user.Id,
                    oldValue,
                    newValue
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = user.Id,
                        login = user.Login,
                        fullName = user.FullName,
                        department = user.Department
                    },
                    message = "Пользователь обновлён"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка обновления пользователя: " + ex.Message);
            }
        }

        // PUT: api/users/5/role
        [HttpPut]
        [Route("{id:int}/role")]
        public async Task<IHttpActionResult> ChangeUserRole(int id, [FromBody] ChangeRoleDto dto)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор пользователя");

                if (dto == null || dto.RoleId <= 0)
                    return BadRequestMessage("Не указан корректный ID роли");

                var user = await _context.Users.FindAsync(id);

                if (user == null)
                    return NotFoundMessage("Пользователь не найден");

                var role = await _context.Roles.FindAsync(dto.RoleId);

                if (role == null)
                    return NotFoundMessage("Роль не найдена");

                var oldRole = await _context.Roles.FindAsync(user.RoleId);
                string oldValue = oldRole != null ? oldRole.Name : user.RoleId.ToString();

                user.RoleId = dto.RoleId;

                AddAudit(
                    GetCurrentUserId(),
                    "Изменение роли пользователя",
                    "User",
                    user.Id,
                    oldValue,
                    role.Name
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = user.Id,
                        login = user.Login,
                        roleId = user.RoleId,
                        roleName = role.Name
                    },
                    message = "Роль пользователя изменена на " + role.Name
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка изменения роли: " + ex.Message);
            }
        }

        // DELETE: api/users/5
        [HttpDelete]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> DeleteUser(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор пользователя");

                int currentUserId = GetCurrentUserId();

                if (id == currentUserId)
                    return BadRequestMessage("Нельзя заблокировать собственную учётную запись");

                var user = await _context.Users.FindAsync(id);

                if (user == null)
                    return NotFoundMessage("Пользователь не найден");

                if (!user.IsActive)
                    return BadRequestMessage("Пользователь уже заблокирован");

                string oldValue = user.IsActive.ToString();

                user.IsActive = false;

                AddAudit(
                    currentUserId,
                    "Блокировка пользователя",
                    "User",
                    user.Id,
                    oldValue,
                    user.IsActive.ToString()
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = user.Id,
                        login = user.Login,
                        isActive = user.IsActive
                    },
                    message = "Пользователь заблокирован"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка блокировки пользователя: " + ex.Message);
            }
        }

        // PUT: api/users/5/activate
        [HttpPut]
        [Route("{id:int}/activate")]
        public async Task<IHttpActionResult> ActivateUser(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор пользователя");

                var user = await _context.Users.FindAsync(id);

                if (user == null)
                    return NotFoundMessage("Пользователь не найден");

                if (user.IsActive)
                    return BadRequestMessage("Пользователь уже активен");

                string oldValue = user.IsActive.ToString();

                user.IsActive = true;

                AddAudit(
                    GetCurrentUserId(),
                    "Активация пользователя",
                    "User",
                    user.Id,
                    oldValue,
                    user.IsActive.ToString()
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = user.Id,
                        login = user.Login,
                        isActive = user.IsActive
                    },
                    message = "Пользователь активирован"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка активации пользователя: " + ex.Message);
            }
        }

        // GET: api/users/5/avatar
        [HttpGet]
        [Route("{id:int}/avatar")]
        public async Task<HttpResponseMessage> GetAvatar(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Некорректный идентификатор пользователя"
                    });
                }

                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new
                    {
                        success = false,
                        message = "Пользователь не найден"
                    });
                }

                if (user.Avatar == null || user.Avatar.Length == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new
                    {
                        success = false,
                        message = "Аватар не найден"
                    });
                }

                var result = new HttpResponseMessage(HttpStatusCode.OK);
                result.Content = new ByteArrayContent(user.Avatar);
                result.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                return result;
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = "Ошибка получения аватара: " + ex.Message
                });
            }
        }

        // POST: api/users/5/avatar
        [HttpPost]
        [Route("{id:int}/avatar")]
        public async Task<IHttpActionResult> UploadAvatar(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequestMessage("Некорректный идентификатор пользователя");

                var user = await _context.Users.FindAsync(id);

                if (user == null)
                    return NotFoundMessage("Пользователь не найден");

                if (!Request.Content.IsMimeMultipartContent())
                    return BadRequestMessage("Неверный формат запроса. Требуется multipart/form-data");

                var provider = new MultipartMemoryStreamProvider();
                await Request.Content.ReadAsMultipartAsync(provider);

                if (provider.Contents.Count == 0)
                    return BadRequestMessage("Файл не найден");

                var file = provider.Contents[0];

                string contentType = file.Headers.ContentType != null
                    ? file.Headers.ContentType.MediaType
                    : null;

                if (contentType != "image/jpeg" && contentType != "image/png")
                    return BadRequestMessage("Допустимы только изображения JPEG или PNG");

                byte[] bytes = await file.ReadAsByteArrayAsync();

                if (bytes == null || bytes.Length == 0)
                    return BadRequestMessage("Файл пустой");

                if (bytes.Length > 5 * 1024 * 1024)
                    return BadRequestMessage("Файл слишком большой. Максимум 5MB");

                user.Avatar = bytes;

                AddAudit(
                    GetCurrentUserId(),
                    "Загрузка аватара пользователя",
                    "User",
                    user.Id,
                    null,
                    "Avatar uploaded, size: " + bytes.Length
                );

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = user.Id,
                        hasAvatar = true,
                        size = bytes.Length
                    },
                    message = "Аватар сохранён"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка загрузки аватара: " + ex.Message);
            }
        }

        private int GetCurrentUserId()
        {
            var identity = User?.Identity as ClaimsIdentity;

            if (identity == null || !identity.IsAuthenticated)
                throw new UnauthorizedAccessException("Пользователь не авторизован");

            var userIdClaim = identity.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                throw new UnauthorizedAccessException("Не удалось определить ID пользователя");

            return userId;
        }

        private string GetClientIp()
        {
            if (Request != null && Request.Properties.ContainsKey("MS_HttpContext"))
            {
                var context = Request.Properties["MS_HttpContext"] as System.Web.HttpContextWrapper;
                return context?.Request?.UserHostAddress ?? "0.0.0.0";
            }

            return "0.0.0.0";
        }

        private void AddAudit(
            int userId,
            string action,
            string entityType,
            int entityId,
            string oldValue,
            string newValue)
        {
            _context.AuditLogs.Add(new AuditLogs
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValue = oldValue,
                NewValue = newValue,
                CreatedAt = DateTime.Now,
                IpAddress = GetClientIp()
            });
        }

        private IHttpActionResult ValidatePagination(int page, int pageSize)
        {
            if (page < 1)
                return BadRequestMessage("Номер страницы должен быть больше 0");

            if (pageSize < 1 || pageSize > 100)
                return BadRequestMessage("Размер страницы должен быть от 1 до 100");

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
            return Content(HttpStatusCode.BadRequest, new { success = false, message });
        }

        private IHttpActionResult NotFoundMessage(string message)
        {
            return Content(HttpStatusCode.BadRequest, ApiResponse<object>.Fail(message));
        }

        private IHttpActionResult UnauthorizedMessage(string message)
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
                _context.Dispose();

            base.Dispose(disposing);
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