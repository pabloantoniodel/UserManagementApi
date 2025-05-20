// Dtos/UserUpdateDto.cs
using System.ComponentModel.DataAnnotations;
using UserManagementApi.Models; // Para Role

namespace UserManagementApi.Dtos
{
    public class UserUpdateDto
    {
        // El username generalmente no se actualiza o requiere lógica especial
        // Por simplicidad, lo omitimos aquí.

        [EmailAddress(ErrorMessage = "Formato de email inválido.")]
        [MaxLength(150)]
        public string? Email { get; set; } // Nullable para permitir actualización parcial

        public bool? PrivacyPolicyAccepted { get; set; }

        public Role? Role { get; set; }

        [MinLength(8, ErrorMessage = "La nueva contraseña debe tener al menos 8 caracteres.")]
        public string? NewPassword { get; set; } // Para cambiar la contraseña
        public int? CompanyId { get; set; } // Permitir actualizar la empresa
         public bool RemoveCompany { get; set; } = false; // Flag para desasociar empresa
    }
}