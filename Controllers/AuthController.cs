// Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.Models;
using UserManagementApi.Services; // Para IEmailService
using UserManagementApi.Dtos; // Para LoginDto y UserReadDto
using BCrypt.Net;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
// Para JWT (si lo implementas completamente)
// using Microsoft.IdentityModel.Tokens;
// using System.IdentityModel.Tokens.Jwt;
// using System.Security.Claims;
// using System.Text;

namespace UserManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration; // Para la clave secreta de JWT
        private readonly ILogger<AuthController> _logger;
        private IEmailService _emailService;

        public AuthController(ApplicationDbContext context, IConfiguration configuration, ILogger<AuthController> logger, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _emailService = emailService;
        }

        public class ForgotPasswordApiDto
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordApiDto forgotPasswordDto)
        {
            _logger.LogInformation($"Solicitud de reseteo de contraseña para email: {forgotPasswordDto.Email}");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == forgotPasswordDto.Email);

            if (user == null)
            {
                // NO reveles si el email existe o no por seguridad.
                _logger.LogWarning($"Intento de reseteo para email no existente (o no se informa): {forgotPasswordDto.Email}");
                return Ok(new { message = "Si tu email está registrado, recibirás un enlace para restablecer tu contraseña." });
            }

            user.ResetPasswordToken = GenerateSecureToken();
            user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(1); // Token válido por 1 hora
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Token de reseteo generado para usuario: {user.Username}");

            try
            {
                // Determinar la URL base del frontend. Esto es mejor configurarlo.
                // string frontendBaseUrl = _configuration["AppSettings:FrontendBaseUrl"] ?? "http://localhost:4200";
                string requestScheme = HttpContext.Request.Scheme;
                string requestHost = HttpContext.Request.Host.Value;
                // Para este flujo, el callback DEBE ser la URL del frontend Angular.
                // Asumimos que el frontend está en el mismo dominio/puerto o configurado.
                // ¡IMPORTANTE! En producción, configura explícitamente la URL base del frontend.
                string frontendBaseUrl = _configuration.GetValue<string>("AppSettings:FrontendUrl") ?? $"{requestScheme}://{requestHost.Replace(":" + HttpContext.Request.Host.Port, ":4200")}";
                // Si la API y el frontend corren en puertos diferentes en desarrollo, necesitas una forma de saber la URL del frontend.
                // Por ejemplo, desde appsettings.json: "AppSettings": { "FrontendUrl": "http://localhost:4200" }


                await _emailService.SendResetPasswordEmailAsync(user.Email, user.Username, user.ResetPasswordToken, frontendBaseUrl);
                _logger.LogInformation($"Email de reseteo de contraseña enviado a {user.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al enviar email de reseteo a {user.Email}");
                // No informar al usuario del error interno, pero el token ya está en la BD.
            }

            return Ok(new { message = "Si tu email está registrado, recibirás un enlace para restablecer tu contraseña." });
        }


        public class ResetPasswordApiDto
        {
            [Required]
            public string Token { get; set; } = string.Empty;

            [Required]
            [MinLength(8)]
            public string NewPassword { get; set; } = string.Empty;

            [Required]
            [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordApiDto resetPasswordDto)
        {
            _logger.LogInformation($"Intento de reseteo de contraseña con token (primeros 10 chars): {resetPasswordDto.Token?.Substring(0, Math.Min(10, resetPasswordDto.Token?.Length ?? 0))}...");

            if (!ModelState.IsValid) // Validará el CompareAttribute
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetPasswordToken == resetPasswordDto.Token);

            if (user == null)
            {
                _logger.LogWarning("Token de reseteo no válido o no encontrado.");
                return BadRequest(new { message = "Token no válido o expirado." });
            }

            if (user.ResetPasswordTokenExpiry < DateTime.UtcNow)
            {
                _logger.LogWarning($"Token de reseteo para {user.Username} ha expirado.");
                user.ResetPasswordToken = null;
                user.ResetPasswordTokenExpiry = null;
                await _context.SaveChangesAsync();
                return BadRequest(new { message = "El token ha expirado. Por favor, solicita uno nuevo." });
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(resetPasswordDto.NewPassword);
            user.ResetPasswordToken = null; // Invalidar token
            user.ResetPasswordTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;
            // Opcional: marcar IsEmailVerified = true si este flujo también verifica el email
            // user.IsEmailVerified = true;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Contraseña reseteada correctamente para: {user.Username}");

            return Ok(new { message = "Tu contraseña ha sido restablecida con éxito.", email = user.Email });
        }

        // Helper para generar tokens seguros (ya deberías tenerlo de la implementación anterior)
        private string GenerateSecureToken(int length = 32)
        {
            using (var randomNumberGenerator = RandomNumberGenerator.Create())
            {
                var randomBytes = new byte[length];
                randomNumberGenerator.GetBytes(randomBytes);
                return Convert.ToBase64String(randomBytes)
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .TrimEnd('=');
            }
        }
        public class LoginApiDto // DTO específico para la API
        {
            [Required]
            public string UsernameOrEmail { get; set; } = string.Empty;
            [Required]
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginApiDto loginDto)
        {
            _logger.LogInformation($"Intento de login para: {loginDto.UsernameOrEmail}");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginDto.UsernameOrEmail || u.Email == loginDto.UsernameOrEmail);

            if (user == null)
            {
                _logger.LogWarning($"Usuario no encontrado: {loginDto.UsernameOrEmail}");
                return Unauthorized(new { message = "Credenciales inválidas." });
            }

            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                _logger.LogWarning($"Usuario {loginDto.UsernameOrEmail} no tiene contraseña establecida.");
                // Podrías redirigir o indicar que necesita establecer contraseña primero.
                return Unauthorized(new { message = "Este usuario no ha establecido una contraseña. Por favor, revisa tu email." });
            }

            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                _logger.LogWarning($"Contraseña incorrecta para: {loginDto.UsernameOrEmail}");
                return Unauthorized(new { message = "Credenciales inválidas." });
            }

            // Si llegamos aquí, el login es exitoso
            _logger.LogInformation($"Login exitoso para: {user.Username}");

            // --- Generación de Token JWT (Simplificado, en una app real sería más robusto) ---
            // Por ahora, solo devolvemos un token simulado.
            // En una implementación real, usarías System.IdentityModel.Tokens.Jwt
            var token = GenerateJwtToken(user); // Implementa esta función si usas JWT real

            var userReadDto = new UserReadDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                PrivacyPolicyAccepted = user.PrivacyPolicyAccepted,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return Ok(new
            {
                message = "Login exitoso.",
                token = token, // Aquí iría el token JWT real
                user = userReadDto,
                canManageCompanies = (user.Role == Role.Administrador) // Para el frontend Determina si puede crear compañias
            });
        }

        // ----- EJEMPLO DE GENERACIÓN DE JWT (Necesitarías Microsoft.AspNetCore.Authentication.JwtBearer) -----
        private string GenerateJwtToken(User user)
        {
            // Si NO vas a implementar JWT real de inmediato, puedes devolver un string simple como placeholder:
            // return $"SIMULATED_JWT_FOR_{user.Username}_{Guid.NewGuid()}";

            // Para JWT real:
            // var jwtKey = _configuration["Jwt:Key"];
            // if (string.IsNullOrEmpty(jwtKey))
            // {
            //     _logger.LogError("JWT Key no está configurada en appsettings.json");
            //     throw new InvalidOperationException("JWT Key no está configurada.");
            // }
            // var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            // var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // var claims = new[]
            // {
            //     new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            //     new Claim(JwtRegisteredClaimNames.Email, user.Email),
            //     new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            //     new Claim(ClaimTypes.Role, user.Role.ToString()), // Importante para la autorización basada en roles
            //     new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            // };

            // var tokenDescriptor = new SecurityTokenDescriptor
            // {
            //     Subject = new ClaimsIdentity(claims),
            //     Expires = DateTime.UtcNow.AddHours(1), // O el tiempo que desees
            //     Issuer = _configuration["Jwt:Issuer"],
            //     Audience = _configuration["Jwt:Audience"],
            //     SigningCredentials = credentials
            // };

            // var tokenHandler = new JwtSecurityTokenHandler();
            // var token = tokenHandler.CreateToken(tokenDescriptor);
            // return tokenHandler.WriteToken(token);

            // Placeholder si no tienes JWT configurado completamente:
            return $"FAKE_JWT_TOKEN_FOR_{user.Id}_{Guid.NewGuid()}";
        }

    }
}