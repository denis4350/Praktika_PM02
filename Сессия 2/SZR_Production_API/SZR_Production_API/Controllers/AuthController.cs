using System;
using System.Threading.Tasks;
using System.Net;
using System.Web.Http;
using SZR_Production_API.Services;

namespace SZR_Production_API.Controllers
{
    [RoutePrefix("api/auth")]
    public class AuthController : ApiController
    {
        private readonly AuthService _authService;

        public AuthController()
        {
            _authService = new AuthService();
        }

        [HttpPost]
        [Route("login")]
        public async Task<IHttpActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                TokenResponseDto result = await _authService.LoginAsync(loginDto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Content(HttpStatusCode.Unauthorized, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("register")]
        public async Task<IHttpActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                UserInfoDto result = await _authService.RegisterAsync(registerDto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("refresh")]
        public async Task<IHttpActionResult> Refresh([FromBody] RefreshTokenDto refreshDto)
        {
            try
            {
                TokenResponseDto result = await _authService.RefreshTokenAsync(refreshDto.AccessToken, refreshDto.RefreshToken);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Content(HttpStatusCode.Unauthorized, new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost]
        [Route("logout")]
        public async Task<IHttpActionResult> Logout()
        {
            int userId = GetCurrentUserId();
            if (userId > 0)
            {
                await _authService.LogoutAsync(userId);
            }
            return Ok(new { message = "Выход выполнен успешно" });
        }

        [Authorize]
        [HttpGet]
        [Route("me")]
        public async Task<IHttpActionResult> GetCurrentUser()
        {
            try
            {
                int userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Unauthorized();
                }

                UserInfoDto user = await _authService.GetCurrentUserAsync(userId);
                if (user == null)
                {
                    return NotFound();
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        private int GetCurrentUserId()
        {
            try
            {
                var identity = User.Identity as System.Security.Claims.ClaimsIdentity;
                if (identity == null)
                {
                    System.Diagnostics.Debug.WriteLine("Identity is null");
                    return 0;
                }

                var userIdClaim = identity.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    System.Diagnostics.Debug.WriteLine("UserId claim not found");
                    return 0;
                }

                if (int.TryParse(userIdClaim.Value, out int userId))
                {
                    return userId;
                }

                return 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting user ID: {ex.Message}");
                return 0;
            }
        }
    }

    public class RefreshTokenDto
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }

        public RefreshTokenDto()
        {
            AccessToken = "";
            RefreshToken = "";
        }
    }
}