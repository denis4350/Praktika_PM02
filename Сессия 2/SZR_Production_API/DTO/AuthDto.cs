using System;
using System.ComponentModel.DataAnnotations;

namespace SZR_Production_API.DTO
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Логин обязателен")]
        [StringLength(50, ErrorMessage = "Логин не должен превышать 50 символов")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(100, MinimumLength = 4, ErrorMessage = "Пароль должен быть от 4 до 100 символов")]
        public string Password { get; set; }
    }

    public class RefreshTokenDto
    {
        [Required(ErrorMessage = "AccessToken обязателен")]
        public string AccessToken { get; set; }

        [Required(ErrorMessage = "RefreshToken обязателен")]
        public string RefreshToken { get; set; }
    }

    public class LogoutDto
    {
        [Required(ErrorMessage = "RefreshToken обязателен")]
        public string RefreshToken { get; set; }
    }

    public class TokenResponseDto
    {
        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public DateTime ExpiresAt { get; set; }

        public UserInfoDto User { get; set; }
    }

    public class UserInfoDto
    {
        public int Id { get; set; }

        public string Login { get; set; }

        public string FullName { get; set; }

        public string Role { get; set; }

        public int RoleId { get; set; }

        public string Department { get; set; }

        public bool IsActive { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Старый пароль обязателен")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "Новый пароль обязателен")]
        [StringLength(100, MinimumLength = 4, ErrorMessage = "Новый пароль должен быть от 4 до 100 символов")]
        public string NewPassword { get; set; }
    }
}