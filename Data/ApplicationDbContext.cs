// Data/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Models;

namespace UserManagementApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }    //Usuarios
        public DbSet<Company> Companies { get; set; } // Empresas
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Asegurar que Username y Email sean únicos
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Configurar la conversión del enum Role a string en la base de datos
            // Esto es bueno para la legibilidad de la BD y si el enum cambia de orden.
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>(); // Almacenará "Usuario", "Superusuario", "Administrador"

                modelBuilder.Entity<Company>(entity =>
    {
        // entity.HasIndex(c => c.CompanyCode).IsUnique(); // <--- ELIMINAR ESTA LÍNEA
        // Puedes añadir un índice para Name si quieres que sea único o para búsquedas rápidas
        entity.HasIndex(c => c.Name).IsUnique(); // Opcional: si el nombre de la empresa debe ser único
    }); 
            /*
            // Opcional: Seed de datos iniciales
            modelBuilder.Entity<User>().HasData(
                 new User {
                     Id = 1, // Hay que especificar el ID para el seed
                     Username = "pabloantoniodel@gmail.com",
                     Email = "pabloantoniodel@gmail.com",
                     PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pppp54321!"), // ¡Hashear!
                     PrivacyPolicyAccepted = true,
                     Role = Role.Administrador,
                     CreatedAt = DateTime.UtcNow,
                     UpdatedAt = DateTime.UtcNow
                 }
             );*/
        }
    }
}