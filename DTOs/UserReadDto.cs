// Dtos/UserReadDto.cs
using UserManagementApi.Models; // Para Role

namespace UserManagementApi.Dtos
{
    public class UserReadDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool PrivacyPolicyAccepted { get; set; }
        public Role Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? CompanyId { get; set; }
        public string? CompanyName { get; set; } // Para mostrar el nombre de la empresa
    }
}