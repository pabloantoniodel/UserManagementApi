// Dtos/SetPasswordDto.cs
using System.ComponentModel.DataAnnotations;

namespace UserManagementApi.Dtos
{
    public class SetPasswordDto
    {
        [Required(ErrorMessage = "El token es obligatorio.")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}