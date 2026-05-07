using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using Microsoft.IdentityModel.Tokens;
using SZR_Production_API.Models;

namespace SZR_Production_API.Services
{
    public interface IAuthService
    {
        Task<TokenResponseDto> LoginAsync(LoginDto loginDto);
        Task<UserInfoDto> RegisterAsync(RegisterDto registerDto);
        Task<TokenResponseDto> RefreshTokenAsync(string accessToken, string refreshToken);
        Task<bool> LogoutAsync(int userId);
        Task<UserInfoDto> GetCurrentUserAsync(int userId);
    }

    public class AuthService : IAuthService
    {
        private readonly SZR_ProductionEntities2 _context;

        public AuthService()
        {
            _context = new SZR_ProductionEntities2();
        }

        public async Task<TokenResponseDto> LoginAsync(LoginDto loginDto)
        {
            Users user = await _context.Users.FirstOrDefaultAsync(u => u.Login == loginDto.Login);

            if (user == null)
            {
                throw new UnauthorizedAccessException("Неверный логин или пароль");
            }

            string passwordHash = ComputeSha256Hash(loginDto.Password);
            if (user.PasswordHash != passwordHash)
            {
                throw new UnauthorizedAccessException("Неверный логин или пароль");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Пользователь заблокирован");
            }

            Roles role = await _context.Roles.FindAsync(user.RoleId);
            string roleName = role != null ? role.Name : "User";

            string accessToken = GenerateAccessToken(user, roleName);
            string refreshToken = GenerateRefreshToken();

            TokenResponseDto response = new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.Now.AddMinutes(60),
                User = new UserInfoDto
                {
                    Id = user.Id,
                    Login = user.Login,
                    FullName = user.FullName,
                    Role = roleName,
                    Department = user.Department
                }
            };

            return response;
        }

        public async Task<UserInfoDto> RegisterAsync(RegisterDto registerDto)
        {
            Users existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Login == registerDto.Login);

            if (existingUser != null)
            {
                throw new InvalidOperationException("Пользователь с таким логином уже существует");
            }

            Users user = new Users
            {
                Login = registerDto.Login,
                PasswordHash = ComputeSha256Hash(registerDto.Password),
                FullName = registerDto.FullName,
                RoleId = registerDto.RoleId,
                Department = registerDto.Department,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            Roles role = await _context.Roles.FindAsync(user.RoleId);
            string roleName = role != null ? role.Name : "User";

            UserInfoDto response = new UserInfoDto
            {
                Id = user.Id,
                Login = user.Login,
                FullName = user.FullName,
                Role = roleName,
                Department = user.Department
            };

            return response;
        }

        public async Task<TokenResponseDto> RefreshTokenAsync(string accessToken, string refreshToken)
        {
            ClaimsPrincipal principal = GetPrincipalFromExpiredToken(accessToken);
            Claim userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            int userId = int.Parse(userIdClaim != null ? userIdClaim.Value : "0");

            Users user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Недействительный refresh token");
            }

            Roles role = await _context.Roles.FindAsync(user.RoleId);
            string roleName = role != null ? role.Name : "User";

            string newAccessToken = GenerateAccessToken(user, roleName);
            string newRefreshToken = GenerateRefreshToken();

            TokenResponseDto response = new TokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.Now.AddMinutes(60),
                User = new UserInfoDto
                {
                    Id = user.Id,
                    Login = user.Login,
                    FullName = user.FullName,
                    Role = roleName,
                    Department = user.Department
                }
            };

            return response;
        }

        public async Task<bool> LogoutAsync(int userId)
        {
            Users user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                return true;
            }
            return false;
        }

        public async Task<UserInfoDto> GetCurrentUserAsync(int userId)
        {
            Users user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            Roles role = await _context.Roles.FindAsync(user.RoleId);
            string roleName = role != null ? role.Name : "User";

            UserInfoDto response = new UserInfoDto
            {
                Id = user.Id,
                Login = user.Login,
                FullName = user.FullName,
                Role = roleName,
                Department = user.Department
            };

            return response;
        }

        private string GenerateAccessToken(Users user, string role)
        {
            string secretKey = "super-secret-key-for-szr-production-2024-min-32-characters";
            byte[] key = Encoding.ASCII.GetBytes(secretKey);

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

            Claim[] claims = new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Login),
                new Claim(ClaimTypes.Role, role),
                new Claim("FullName", user.FullName)
            };

            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddMinutes(60),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            byte[] randomNumber = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }
            return Convert.ToBase64String(randomNumber);
        }

        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                return Convert.ToBase64String(bytes);
            }
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            string secretKey = "super-secret-key-for-szr-production-2024-min-32-characters";
            byte[] key = Encoding.ASCII.GetBytes(secretKey);

            TokenValidationParameters tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = false
            };

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            ClaimsPrincipal principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            JwtSecurityToken jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }
    }

    // DTO классы
    public class LoginDto
    {
        public string Login { get; set; }
        public string Password { get; set; }

        public LoginDto()
        {
            Login = "";
            Password = "";
        }
    }

    public class RegisterDto
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public int RoleId { get; set; }
        public string Department { get; set; }

        public RegisterDto()
        {
            Login = "";
            Password = "";
            FullName = "";
            Department = "";
            RoleId = 2;
        }
    }

    public class TokenResponseDto
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public UserInfoDto User { get; set; }

        public TokenResponseDto()
        {
            AccessToken = "";
            RefreshToken = "";
            User = new UserInfoDto();
        }
    }

    public class UserInfoDto
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string Department { get; set; }

        public UserInfoDto()
        {
            Login = "";
            FullName = "";
            Role = "";
            Department = "";
        }
    }
}