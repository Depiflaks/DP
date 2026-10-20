using System.ComponentModel.DataAnnotations;

namespace Valuator.Authentication;

public sealed class Credentials
{
    [Required(ErrorMessage = "Введите логин.")]
    [RegularExpression(@"[a-zA-Z0-9_-]{3,64}", ErrorMessage = "Логин: 3–64 латинских буквы, цифры, _ или -.")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Пароль: от 8 до 128 символов.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
