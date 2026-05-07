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
    [RoutePrefix("api/notifications")]
    public class NotificationsController : ApiController
    {
        private readonly SZR_ProductionEntities2 _context;

        public NotificationsController()
        {
            _context = new SZR_ProductionEntities2();
        }

        // GET: api/notifications
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetNotifications(int page = 1, int pageSize = 20)
        {
            int userId = GetCurrentUserId();

            var totalCount = await _context.Notifications
                .Where(n => n.UserId == userId)
                .CountAsync();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new
                {
                    n.Id,
                    n.Type,
                    n.Title,
                    n.Message,
                    n.IsRead,
                    n.CreatedAt
                })
                .ToListAsync();

            var unreadCount = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();

            return Ok(new
            {
                success = true,
                data = notifications,
                unreadCount = unreadCount,
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        // GET: api/notifications/unread
        [HttpGet]
        [Route("unread")]
        public async Task<IHttpActionResult> GetUnreadNotifications()
        {
            int userId = GetCurrentUserId();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    n.Id,
                    n.Type,
                    n.Title,
                    n.Message,
                    n.CreatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, data = notifications });
        }

        // PUT: api/notifications/{id}/read
        [HttpPut]
        [Route("{id:int}/read")]
        public async Task<IHttpActionResult> MarkAsRead(int id)
        {
            try
            {
                int userId = GetCurrentUserId();
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

                if (notification == null)
                {
                    return Content(HttpStatusCode.NotFound, new { success = false, message = "Уведомление не найдено" });
                }

                notification.IsRead = true;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Уведомление отмечено как прочитанное" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // PUT: api/notifications/read-all
        [HttpPut]
        [Route("read-all")]
        public async Task<IHttpActionResult> MarkAllAsRead()
        {
            int userId = GetCurrentUserId();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Все уведомления отмечены как прочитанные" });
        }

        // DELETE: api/notifications/{id}
        [HttpDelete]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> DeleteNotification(int id)
        {
            int userId = GetCurrentUserId();
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
            {
                return NotFound();
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Уведомление удалено" });
        }

        // POST: api/notifications/create (для системы - создание уведомления)
        [HttpPost]
        [Route("create")]
        [Authorize(Roles = "Администратор")]
        public async Task<IHttpActionResult> CreateNotification([FromBody] CreateNotificationDto dto)
        {
            var notification = new Notifications
            {
                UserId = dto.UserId,
                Type = dto.Type,
                Title = dto.Title,
                Message = dto.Message,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Уведомление создано" });
        }

        private int GetCurrentUserId()
        {
            var identity = User.Identity as System.Security.Claims.ClaimsIdentity;
            var userIdClaim = identity?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }
    }

    public class CreateNotificationDto
    {
        public int UserId { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
    }
}