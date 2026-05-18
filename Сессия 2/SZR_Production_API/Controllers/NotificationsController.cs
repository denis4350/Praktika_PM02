using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using SZR_Production_API.Models;

namespace SZR_Production_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/notifications")]
    public class NotificationsController : ApiController
    {
        private readonly SZR_ProductionEntities2 _context;

        public NotificationsController()
        {
            _context = new SZR_ProductionEntities2();
        }

        // GET: api/notifications?page=1&pageSize=20&isRead=false
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetNotifications(
            int page = 1,
            int pageSize = 20,
            bool? isRead = null)
        {
            try
            {
                var validation = ValidatePagination(page, pageSize);
                if (validation != null)
                    return validation;

                int userId = GetCurrentUserId();

                var query = _context.Notifications
                    .Where(n => n.UserId == userId);

                if (isRead.HasValue)
                {
                    query = query.Where(n => n.IsRead == isRead.Value);
                }

                int totalCount = await query.CountAsync();

                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new
                    {
                        id = n.Id,
                        type = n.Type,
                        title = n.Title,
                        message = n.Message,
                        isRead = n.IsRead,
                        createdAt = n.CreatedAt,
                        metadata = n.Metadata
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = notifications,
                    pagination = BuildPagination(page, pageSize, totalCount),
                    message = "Список уведомлений получен"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения уведомлений: " + ex.Message);
            }
        }

        // GET: api/notifications/unread?limit=20
        [HttpGet]
        [Route("unread")]
        public async Task<IHttpActionResult> GetUnreadNotifications(int limit = 20)
        {
            try
            {
                if (limit < 1 || limit > 100)
                {
                    return BadRequestMessage("limit должен быть от 1 до 100");
                }

                int userId = GetCurrentUserId();

                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(limit)
                    .Select(n => new
                    {
                        id = n.Id,
                        type = n.Type,
                        title = n.Title,
                        message = n.Message,
                        createdAt = n.CreatedAt,
                        metadata = n.Metadata
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = notifications,
                    pagination = (object)null,
                    message = "Непрочитанные уведомления получены"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения непрочитанных уведомлений: " + ex.Message);
            }
        }

        // GET: api/notifications/unread-count
        [HttpGet]
        [Route("unread-count")]
        public async Task<IHttpActionResult> GetUnreadCount()
        {
            try
            {
                int userId = GetCurrentUserId();

                int count = await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        unreadCount = count
                    },
                    message = "Количество непрочитанных уведомлений получено"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка получения количества уведомлений: " + ex.Message);
            }
        }

        // PUT: api/notifications/5/read
        [HttpPut]
        [Route("{id:int}/read")]
        public async Task<IHttpActionResult> MarkAsRead(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequestMessage("Некорректный идентификатор уведомления");
                }

                int userId = GetCurrentUserId();

                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

                if (notification == null)
                {
                    return NotFoundMessage("Уведомление не найдено");
                }

                notification.IsRead = true;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = notification.Id,
                        isRead = notification.IsRead
                    },
                    message = "Уведомление отмечено как прочитанное"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка отметки уведомления: " + ex.Message);
            }
        }

        // PUT: api/notifications/read-all
        [HttpPut]
        [Route("read-all")]
        public async Task<IHttpActionResult> MarkAllAsRead()
        {
            try
            {
                int userId = GetCurrentUserId();

                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                }

                int changedCount = notifications.Count;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        changedCount
                    },
                    message = "Все уведомления отмечены как прочитанные"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка при массовом прочтении уведомлений: " + ex.Message);
            }
        }

        // DELETE: api/notifications/5
        [HttpDelete]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> DeleteNotification(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequestMessage("Некорректный идентификатор уведомления");
                }

                int userId = GetCurrentUserId();

                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

                if (notification == null)
                {
                    return NotFoundMessage("Уведомление не найдено");
                }

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id
                    },
                    message = "Уведомление удалено"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedMessage(ex.Message);
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка удаления уведомления: " + ex.Message);
            }
        }

        // POST: api/notifications/create
        [HttpPost]
        [Route("create")]
        [Authorize(Roles = "Администратор")]
        public async Task<IHttpActionResult> CreateNotification([FromBody] CreateNotificationDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequestMessage("Тело запроса пустое");
                }

                if (dto.UserId <= 0)
                {
                    return BadRequestMessage("Некорректный идентификатор пользователя");
                }

                if (string.IsNullOrWhiteSpace(dto.Title))
                {
                    return BadRequestMessage("Заголовок уведомления обязателен");
                }

                if (string.IsNullOrWhiteSpace(dto.Message))
                {
                    return BadRequestMessage("Текст уведомления обязателен");
                }

                bool userExists = await _context.Users
                    .AnyAsync(u => u.Id == dto.UserId && u.IsActive);

                if (!userExists)
                {
                    return NotFoundMessage("Активный пользователь не найден");
                }

                var notification = new Notifications
                {
                    UserId = dto.UserId,
                    Type = string.IsNullOrWhiteSpace(dto.Type) ? "Системное" : dto.Type.Trim(),
                    Title = dto.Title.Trim(),
                    Message = dto.Message.Trim(),
                    IsRead = false,
                    CreatedAt = DateTime.Now,
                    Metadata = dto.Metadata
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = notification.Id,
                        userId = notification.UserId,
                        type = notification.Type,
                        title = notification.Title,
                        isRead = notification.IsRead,
                        createdAt = notification.CreatedAt
                    },
                    message = "Уведомление создано"
                });
            }
            catch (Exception ex)
            {
                return ServerError("Ошибка создания уведомления: " + ex.Message);
            }
        }

        private int GetCurrentUserId()
        {
            var identity = User?.Identity as ClaimsIdentity;

            if (identity == null || !identity.IsAuthenticated)
            {
                throw new UnauthorizedAccessException("Пользователь не авторизован");
            }

            var userIdClaim = identity.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                throw new UnauthorizedAccessException("Не удалось определить ID пользователя");
            }

            return userId;
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
            return Content(HttpStatusCode.BadRequest, ApiResponse<object>.Fail(message));
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
            {
                _context.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    public class CreateNotificationDto
    {
        public int UserId { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Metadata { get; set; }
    }
}