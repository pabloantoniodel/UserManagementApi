// Dtos/UserCreateDto.cs
using System.ComponentModel.DataAnnotations;
using UserManagementApi.Models; // Para Role

namespace UserManagementApi.Dtos
{
    public class UserCreateDto
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de email inválido.")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        //[Required(ErrorMessage = "La contraseña es obligatoria.")]
        //[MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        //public string PasswordHash { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe aceptar la política de privacidad.")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "Debe aceptar la política de privacidad.")]
        public bool PrivacyPolicyAccepted { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio.")]
        public Role Role { get; set; } = Role.Usuario; // Default role
        public int? CompanyId { get; set; } // Nullable aquí, la lógica de validación estará en el controller
    }
}