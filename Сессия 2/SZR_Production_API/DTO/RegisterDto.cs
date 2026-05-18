using System.ComponentModel.DataAnnotations;

public class RegisterDto
{
    [Required(ErrorMessage = "Логин обязателен")]
    [StringLength(50, ErrorMessage = "Логин не должен превышать 50 символов")]
    public string Login { get; set; }

    [Required(ErrorMessage = "Пароль обязателен")]
    [StringLength(100, MinimumLength = 4, ErrorMessage = "Пароль должен быть от 4 до 100 символов")]
    public string Password { get; set; }

    [Required(ErrorMessage = "ФИО обязательно")]
    [StringLength(150, ErrorMessage = "ФИО не должно превышать 150 символов")]
    public string FullName { get; set; }

    public string Department { get; set; }
}