using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using SZR_Production_API.DTO;
using SZR_Production_API.Models;
using SZR_Production_API.Services;

namespace SZR_Production_API.Controllers
{
    [RoutePrefix("api/auth")]
    public class AuthController : ApiController
    {
        private readonly IAuthService _authService;

        public AuthController()
        {
            _authService = new AuthService();
        }

        // POST: api/auth/login
        [HttpPost]
        [Route("login")]
        [AllowAnonymous]
        public async Task<IHttpActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Некорректные данные запроса",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                if (loginDto == null)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Тело запроса пустое"
                    });
                }

                if (string.IsNullOrWhiteSpace(loginDto.Login))
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Логин обязателен"
                    });
                }

                if (string.IsNullOrWhiteSpace(loginDto.Password))
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Пароль обязателен"
                    });
                }

                var result = await _authService.LoginAsync(loginDto);

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "Аутентификация успешна"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Content(HttpStatusCode.Unauthorized, new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = "Ошибка входа: " + ex.Message
                });
            }
        }

        // POST: api/auth/refresh
        [HttpPost]
        [Route("refresh")]
        [AllowAnonymous]
        public async Task<IHttpActionResult> Refresh([FromBody] RefreshTokenDto refreshDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Некорректные данные запроса",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                if (refreshDto == null)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Тело запроса пустое"
                    });
                }

                if (string.IsNullOrWhiteSpace(refreshDto.AccessToken))
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "AccessToken обязателен"
                    });
                }

                if (string.IsNullOrWhiteSpace(refreshDto.RefreshToken))
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "RefreshToken обязателен"
                    });
                }

                var result = await _authService.RefreshTokenAsync(
                    refreshDto.AccessToken,
                    refreshDto.RefreshToken
                );

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "Токен обновлён"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Content(HttpStatusCode.Unauthorized, new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = "Ошибка обновления токена: " + ex.Message
                });
            }
        }

        // POST: api/auth/logout
        [HttpPost]
        [Route("logout")]
        [Authorize]
        public async Task<IHttpActionResult> Logout([FromBody] LogoutDto logoutDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Некорректные данные запроса",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                if (logoutDto == null || string.IsNullOrWhiteSpace(logoutDto.RefreshToken))
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "RefreshToken обязателен"
                    });
                }

                int userId = GetCurrentUserId();

                bool success = await _authService.LogoutAsync(
                    userId,
                    logoutDto.RefreshToken
                );

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        loggedOut = success
                    },
                    message = success
                        ? "Выход выполнен"
                        : "Refresh token не найден"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Content(HttpStatusCode.Unauthorized, new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = "Ошибка выхода: " + ex.Message
                });
            }
        }
        // POST: api/auth/register
        // POST: api/auth/register
        [HttpPost]
        [Route("register")]
        [AllowAnonymous]
        public async Task<IHttpActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Некорректные данные: " + string.Join("; ", ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage))
                    });
                }

                var result = await _authService.RegisterAsync(registerDto);

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "Регистрация успешна"
                });
            }
            catch (InvalidOperationException ex)
            {
                return Content(HttpStatusCode.Conflict, new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Content(HttpStatusCode.BadRequest, new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = "Ошибка регистрации: " + ex.Message });
            }
        }
        [HttpPost]
        [Route("change-password")]
        [Authorize]
        public async Task<IHttpActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Content(HttpStatusCode.BadRequest, ApiResponse<object>.Fail(
                        "Некорректные данные: " + string.Join("; ", ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage))));
                }

                int userId = GetCurrentUserId();

                bool success = await _authService.ChangePasswordAsync(userId, dto.OldPassword, dto.NewPassword);

                return Ok(ApiResponse<object>.Ok(null, "Пароль успешно изменён"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    ApiResponse<object>.Fail("Ошибка смены пароля: " + ex.Message));
            }
        }

        // GET: api/auth/me
        [HttpGet]
        [Route("me")]
        [Authorize]
        public async Task<IHttpActionResult> GetCurrentUser()
        {
            try
            {
                int userId = GetCurrentUserId();

                var user = await _authService.GetCurrentUserAsync(userId);

                if (user == null)
                {
                    return Content(HttpStatusCode.NotFound, new
                    {
                        success = false,
                        message = "Пользователь не найден"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = user,
                    message = "Данные текущего пользователя получены"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Content(HttpStatusCode.Unauthorized, new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = "Ошибка получения пользователя: " + ex.Message
                });
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

            if (userIdClaim == null)
            {
                throw new UnauthorizedAccessException("В токене отсутствует идентификатор пользователя");
            }

            if (!int.TryParse(userIdClaim.Value, out int userId))
            {
                throw new UnauthorizedAccessException("Некорректный идентификатор пользователя в токене");
            }

            return userId;
        }
    }

}