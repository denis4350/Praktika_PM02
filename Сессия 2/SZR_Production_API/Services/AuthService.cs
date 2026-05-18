using Microsoft.IdentityModel.Tokens;
using System;
using System.Configuration;
using System.Data.Entity;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SZR_Production_API.DTO;
using SZR_Production_API.Models;

namespace SZR_Production_API.Services
{
    public interface IAuthService
    {
        Task<TokenResponseDto> LoginAsync(LoginDto loginDto);
        Task<TokenResponseDto> RefreshTokenAsync(string accessToken, string refreshToken);
        Task<bool> LogoutAsync(int userId, string refreshToken);
        Task<UserInfoDto> GetCurrentUserAsync(int userId);
        Task<UserInfoDto> RegisterAsync(RegisterDto registerDto);
        Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    }

    public class AuthService : IAuthService, IDisposable
    {
        private readonly SZR_ProductionEntities2 _context;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly int _jwtExpiryMinutes;
        private readonly int _refreshTokenExpiryDays;


        public AuthService()
        {
            _context = new SZR_ProductionEntities2();

            _jwtSecret = ConfigurationManager.AppSettings["JwtSecret"];
            _jwtIssuer = ConfigurationManager.AppSettings["JwtIssuer"] ?? "SZR_Production_API";
            _jwtAudience = ConfigurationManager.AppSettings["JwtAudience"] ?? "SZR_Production_Client";

            _jwtExpiryMinutes = ReadIntSetting("JwtExpiryMinutes", 60);
            _refreshTokenExpiryDays = ReadIntSetting("RefreshTokenExpiryDays", 7);

            if (string.IsNullOrWhiteSpace(_jwtSecret) || _jwtSecret.Length < 32)
            {
                throw new InvalidOperationException("JwtSecret must be at least 32 characters long in Web.config");
            }
        }
        public async Task<UserInfoDto> RegisterAsync(RegisterDto registerDto)
        {
            if (registerDto == null)
                throw new ArgumentNullException(nameof(registerDto));

            if (string.IsNullOrWhiteSpace(registerDto.Login) ||
                string.IsNullOrWhiteSpace(registerDto.Password) ||
                string.IsNullOrWhiteSpace(registerDto.FullName))
            {
                throw new ArgumentException("Логин, пароль и ФИО обязательны");
            }

            string login = registerDto.Login.Trim();

            // Проверка на существование логина
            bool exists = await _context.Users.AnyAsync(u => u.Login == login);
            if (exists)
                throw new InvalidOperationException("Пользователь с таким логином уже существует");

            // Получаем роль "Технолог"
            var technologistRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Технолог");
            int roleId = technologistRole?.Id ?? 2; // если роль не найдена, fallback на ID 2

            string passwordHash = HashPassword(registerDto.Password);

            var user = new Users
            {
                Login = login,
                PasswordHash = passwordHash,
                FullName = registerDto.FullName.Trim(),
                RoleId = roleId,
                Department = registerDto.Department?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new UserInfoDto
            {
                Id = user.Id,
                Login = user.Login,
                FullName = user.FullName,
                Role = technologistRole?.Name ?? "Технолог",
                RoleId = roleId,
                Department = user.Department,
                IsActive = user.IsActive
            };
        }
        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new ArgumentException("Пользователь не найден");

            if (!VerifyPassword(oldPassword, user.PasswordHash))
                throw new UnauthorizedAccessException("Неверный текущий пароль");

            user.PasswordHash = HashPassword(newPassword);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<TokenResponseDto> LoginAsync(LoginDto loginDto)
        {
            if (loginDto == null)
                throw new UnauthorizedAccessException("Некорректные данные входа");

            if (string.IsNullOrWhiteSpace(loginDto.Login) || string.IsNullOrWhiteSpace(loginDto.Password))
                throw new UnauthorizedAccessException("Логин и пароль обязательны");

            string login = loginDto.Login.Trim();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Login == login);

            if (user == null)
                throw new UnauthorizedAccessException("Неверный логин или пароль");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Пользователь заблокирован");

            if (!VerifyPassword(loginDto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Неверный логин или пароль");

            var role = await _context.Roles.FindAsync(user.RoleId);
            string roleName = role != null ? role.Name : "User";

            string accessToken = GenerateAccessToken(user, roleName);
            string refreshToken = GenerateRefreshToken();
            string refreshTokenHash = HashRefreshToken(refreshToken);

            var refreshTokenEntity = new RefreshTokens
            {
                UserId = user.Id,
                Token = refreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            return BuildTokenResponse(user, roleName, accessToken, refreshToken);
        }

        public async Task<TokenResponseDto> RefreshTokenAsync(string accessToken, string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
                throw new UnauthorizedAccessException("Не указаны токены");

            ClaimsPrincipal principal = GetPrincipalFromExpiredToken(accessToken);

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                throw new UnauthorizedAccessException("Недействительный access token");

            string refreshTokenHash = HashRefreshToken(refreshToken);

            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt =>
                    rt.UserId == userId &&
                    rt.Token == refreshTokenHash &&
                    !rt.IsRevoked &&
                    rt.ExpiresAt > DateTime.UtcNow);

            if (storedToken == null)
                throw new UnauthorizedAccessException("Недействительный refresh token");

            var user = await _context.Users.FindAsync(userId);

            if (user == null || !user.IsActive)
                throw new UnauthorizedAccessException("Пользователь не найден или заблокирован");

            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;

            var role = await _context.Roles.FindAsync(user.RoleId);
            string roleName = role != null ? role.Name : "User";

            string newAccessToken = GenerateAccessToken(user, roleName);
            string newRefreshToken = GenerateRefreshToken();
            string newRefreshTokenHash = HashRefreshToken(newRefreshToken);

            _context.RefreshTokens.Add(new RefreshTokens
            {
                UserId = user.Id,
                Token = newRefreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return BuildTokenResponse(user, roleName, newAccessToken, newRefreshToken);
        }

        public async Task<bool> LogoutAsync(int userId, string refreshToken)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(refreshToken))
                return false;

            string refreshTokenHash = HashRefreshToken(refreshToken);

            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt =>
                    rt.UserId == userId &&
                    rt.Token == refreshTokenHash &&
                    !rt.IsRevoked);

            if (token == null)
                return false;

            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<UserInfoDto> GetCurrentUserAsync(int userId)
        {
            if (userId <= 0)
                return null;

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return null;

            var role = await _context.Roles.FindAsync(user.RoleId);

            return new UserInfoDto
            {
                Id = user.Id,
                Login = user.Login,
                FullName = user.FullName,
                Role = role != null ? role.Name : "User",
                RoleId = user.RoleId,
                Department = user.Department,
                IsActive = user.IsActive
            };
        }

        private TokenResponseDto BuildTokenResponse(
            Users user,
            string roleName,
            string accessToken,
            string refreshToken)
        {
            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtExpiryMinutes),
                User = new UserInfoDto
                {
                    Id = user.Id,
                    Login = user.Login,
                    FullName = user.FullName,
                    Role = roleName,
                    RoleId = user.RoleId,
                    Department = user.Department,
                    IsActive = user.IsActive
                }
            };
        }

        private string GenerateAccessToken(Users user, string role)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Login ?? ""),
                new Claim(ClaimTypes.Role, role ?? "User"),
                new Claim("login", user.Login ?? ""),
                new Claim("fullName", user.FullName ?? ""),
                new Claim("roleId", user.RoleId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _jwtIssuer,
                audience: _jwtAudience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(_jwtExpiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            byte[] randomNumber = new byte[64];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }

            return Convert.ToBase64String(randomNumber);
        }

        private string HashRefreshToken(string refreshToken)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(refreshToken);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private bool VerifyPassword(string plainPassword, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(plainPassword) || string.IsNullOrWhiteSpace(storedHash))
                return false;

            if (storedHash.Contains(":"))
            {
                return VerifyPbkdf2Password(plainPassword, storedHash);
            }

            return VerifyLegacySha256Password(plainPassword, storedHash);
        }

        private bool VerifyPbkdf2Password(string plainPassword, string storedHash)
        {
            try
            {
                string[] parts = storedHash.Split(':');

                if (parts.Length != 2)
                    return false;

                byte[] expectedHash = Convert.FromBase64String(parts[0]);
                byte[] salt = Convert.FromBase64String(parts[1]);

                using (var pbkdf2 = new Rfc2898DeriveBytes(
                    plainPassword,
                    salt,
                    10000,
                    HashAlgorithmName.SHA256))
                {
                    byte[] actualHash = pbkdf2.GetBytes(32);
                    return FixedTimeEquals(actualHash, expectedHash);
                }
            }
            catch
            {
                return false;
            }
        }

        private bool VerifyLegacySha256Password(string plainPassword, string storedHash)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(plainPassword));
                string computedHash = Convert.ToBase64String(hashBytes);
                return SlowEquals(computedHash, storedHash);
            }
        }

        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password is empty");

            byte[] salt = new byte[16];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                10000,
                HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(32);
                return Convert.ToBase64String(hash) + ":" + Convert.ToBase64String(salt);
            }
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidAudience = _jwtAudience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret)),
                ValidateLifetime = false,
                ClockSkew = TimeSpan.Zero
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);

            var jwtSecurityToken = securityToken as JwtSecurityToken;

            if (jwtSecurityToken == null ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }

        private int ReadIntSetting(string key, int defaultValue)
        {
            string value = ConfigurationManager.AppSettings[key];

            if (int.TryParse(value, out int result) && result > 0)
                return result;

            return defaultValue;
        }

        private bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
                return false;

            int diff = 0;

            for (int i = 0; i < left.Length; i++)
            {
                diff |= left[i] ^ right[i];
            }

            return diff == 0;
        }

        private bool SlowEquals(string left, string right)
        {
            if (left == null || right == null)
                return false;

            byte[] leftBytes = Encoding.UTF8.GetBytes(left);
            byte[] rightBytes = Encoding.UTF8.GetBytes(right);

            return FixedTimeEquals(leftBytes, rightBytes);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}