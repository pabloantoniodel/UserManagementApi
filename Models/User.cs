// Models/User.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementApi.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        // La contraseña ahora puede ser null hasta que el usuario la establezca
        public string? PasswordHash { get; set; }

        [Required]
        public bool PrivacyPolicyAccepted { get; set; }

        [Required]
        public Role Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Nuevos campos para el establecimiento de contraseña
        public string? SetPasswordToken { get; set; }
        public DateTime? SetPasswordTokenExpiry { get; set; }
        // NUEVO/AJUSTADO: Para el flujo de "olvidé mi contraseña"
        public string? ResetPasswordToken { get; set; }
        public DateTime? ResetPasswordTokenExpiry { get; set; }

        public bool IsEmailVerified { get; set; } = false; // Opcional: para marcar si ya estableció contraseña
        // --- RELACIÓN CON EMPRESA ---
        public int? CompanyId { get; set; } // Nullable para permitir Admins sin empresa
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; } // Propiedad de navegación
    }
}