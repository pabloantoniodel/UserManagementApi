// Controllers/UsersController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.Models;
using UserManagementApi.Dtos;
using UserManagementApi.Services; // Para IEmailService
using BCrypt.Net;
using System.Security.Cryptography; // Para generar tokens
using System.Text; // Para Encoding

namespace UserManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsersController> _logger;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration; // Para obtener la URL base

        public UsersController(
            ApplicationDbContext context,
            ILogger<UsersController> logger,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _configuration = configuration;
        }

        // GET: api/Users (sin cambios)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserReadDto>>> GetUsers()
        {
            _logger.LogInformation("Obteniendo todos los usuarios");
            var users = await _context.Users
                .Select(u => new UserReadDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    PrivacyPolicyAccepted = u.PrivacyPolicyAccepted,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    CompanyId = u.CompanyId,
                    CompanyName = u.Company != null ? u.Company.Name : null // Nombre de la empresa
                    // No incluir PasswordHash, SetPasswordToken, etc.
                })
                .ToListAsync();
            return Ok(users);
        }

        // GET: api/Users/5 (sin cambios)
        [HttpGet("{id}")]
        public async Task<ActionResult<UserReadDto>> GetUser(int id)
        {
            _logger.LogInformation($"Obteniendo usuario con ID: {id}");
            var user = await _context.Users.FindAsync(id);
            user = await _context.Users.Include(u => u.Company).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                _logger.LogWarning($"Usuario con ID: {id} no encontrado.");
                return NotFound(new { message = $"Usuario con ID {id} no encontrado." });
            }
            var userDto = new UserReadDto { /* ... mapeo ... */ }; // Igual que antes
            return Ok(userDto);
        }

        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<UserReadDto>> CreateUser(UserCreateDto userCreateDto)
        {
            _logger.LogInformation($"Intentando crear usuario: {userCreateDto.Username}");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await _context.Users.AnyAsync(u => u.Username == userCreateDto.Username))
            {
                return Conflict(new { message = $"El nombre de usuario '{userCreateDto.Username}' ya está en uso." });
            }
            if (await _context.Users.AnyAsync(u => u.Email == userCreateDto.Email))
            {
                return Conflict(new { message = $"El email '{userCreateDto.Email}' ya está en uso." });
            }
            // Validaciones de  Empresa // solo los Administradores pueden crear empresas 
            if (userCreateDto.Role == Role.Usuario || userCreateDto.Role == Role.Superusuario)
            {
                if (!userCreateDto.CompanyId.HasValue)
                {
                    ModelState.AddModelError(nameof(userCreateDto.CompanyId), "Se requiere una empresa para roles Usuario y Superusuario.");
                    return BadRequest(ModelState);
                }
                if (!await _context.Companies.AnyAsync(c => c.Id == userCreateDto.CompanyId.Value))
                {
                    ModelState.AddModelError(nameof(userCreateDto.CompanyId), $"La empresa con ID {userCreateDto.CompanyId.Value} no existe.");
                    return BadRequest(ModelState);
                }
            }
            else if (userCreateDto.Role == Role.Administrador)
            {
                // Los administradores pueden o no tener empresa. Si se provee, debe existir.
                if (userCreateDto.CompanyId.HasValue && !await _context.Companies.AnyAsync(c => c.Id == userCreateDto.CompanyId.Value)) {
                    ModelState.AddModelError(nameof(userCreateDto.CompanyId), $"La empresa con ID {userCreateDto.CompanyId.Value} no existe.");
                    return BadRequest(ModelState);
                }
                // Si es admin y no se provee CompanyId, está bien.
            }



            var user = new User
            {
                Username = userCreateDto.Username,
                Email = userCreateDto.Email,
                // PasswordHash se deja null por ahora
                PrivacyPolicyAccepted = userCreateDto.PrivacyPolicyAccepted,
                Role = userCreateDto.Role,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SetPasswordToken = GenerateSecureToken(),
                SetPasswordTokenExpiry = DateTime.UtcNow.AddHours(24), // Token válido por 24 horas
                CompanyId = userCreateDto.CompanyId // Asignar CompanyId
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Usuario '{user.Username}' creado con ID: {user.Id}, EmpresaID: {user.CompanyId}.");

                // Cargar explícitamente la empresa para devolver el CompanyName
                if (user.CompanyId.HasValue)
                {
                    await _context.Entry(user).Reference(u => u.Company).LoadAsync();
                }
            // Enviar email para establecer contraseña
            try
            {
                string requestScheme = HttpContext.Request.Scheme; // http o https
                string requestHost = HttpContext.Request.Host.Value; // localhost:7001
                string callbackUrlBase = $"{requestScheme}://{requestHost}";

                await _emailService.SendSetPasswordEmailAsync(user.Email, user.Username, user.SetPasswordToken, callbackUrlBase);
                _logger.LogInformation($"Email para establecer contraseña enviado a {user.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al enviar email de establecimiento de contraseña a {user.Email}");
                // Considera qué hacer aquí. ¿Revertir la creación del usuario? ¿Marcar para reintento?
                // Por ahora, el usuario se crea pero no recibe el email.
            }


            var userReadDto = new UserReadDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                PrivacyPolicyAccepted = user.PrivacyPolicyAccepted,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                CompanyId = user.CompanyId,
                CompanyName = user.Company?.Name
            };
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userReadDto);
        }

        // NUEVO Endpoint para que el usuario establezca su contraseña
        [HttpPost("set-password")]
        public async Task<IActionResult> SetPassword(SetPasswordDto setPasswordDto)
        {
            _logger.LogInformation($"Intentando establecer contraseña con token: {setPasswordDto.Token?.Substring(0, Math.Min(10, setPasswordDto.Token?.Length ?? 0))}...");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.SetPasswordToken == setPasswordDto.Token);

            if (user == null)
            {
                _logger.LogWarning("Token de establecimiento de contraseña no válido o no encontrado.");
                return BadRequest(new { message = "Token no válido o expirado." });
            }

            if (user.SetPasswordTokenExpiry < DateTime.UtcNow)
            {
                _logger.LogWarning($"Token para usuario {user.Username} ha expirado.");
                user.SetPasswordToken = null; // Limpiar token expirado
                user.SetPasswordTokenExpiry = null;
                await _context.SaveChangesAsync();
                return BadRequest(new { message = "El token ha expirado. Por favor, solicita uno nuevo." }); // O redirige a "reenviar email"
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(setPasswordDto.NewPassword);
            user.SetPasswordToken = null; // Importante: invalidar el token después de usarlo
            user.SetPasswordTokenExpiry = null;
            user.IsEmailVerified = true; // Marcar que ya estableció contraseña/verificó email
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Contraseña establecida correctamente para el usuario: {user.Username}");

            return Ok(new { message = "Contraseña establecida correctamente." });
        }


        // PUT: api/Users/5 (sin cambios significativos en lógica central, pero NewPassword es más relevante aquí para admin)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserUpdateDto userUpdateDto)
        {
            // ... (lógica existente) ...
            // El campo NewPassword en UserUpdateDto podría ser usado por un admin para cambiar la contraseña directamente.
            // Si el usuario quiere cambiar su propia contraseña, necesitaría un endpoint diferente
            // que requiera la contraseña actual o un flujo de "olvidé mi contraseña".

             var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                _logger.LogWarning($"Usuario con ID: {id} no encontrado para actualizar.");
                return NotFound(new { message = $"Usuario con ID {id} no encontrado." });
            }

            // Actualizar campos si se proporcionan en el DTO
            bool changed = false;
            // Actualizar Rol
            Role targetRole = userUpdateDto.Role ?? user.Role; // Rol final del usuario
             // Actualizar CompanyId
            if (userUpdateDto.RemoveCompany) // Si el flag es true, quitar la empresa
            {
                if (targetRole == Role.Usuario || targetRole == Role.Superusuario)
                {
                    return BadRequest(new { message = "No se puede quitar la empresa para roles Usuario y Superusuario. Asigna una nueva o cambia el rol." });
                }
                user.CompanyId = null;
                changed = true;
            }
            else if (userUpdateDto.CompanyId.HasValue)
            {
                if (!await _context.Companies.AnyAsync(c => c.Id == userUpdateDto.CompanyId.Value))
                {
                    return BadRequest(new { message = $"La empresa con ID {userUpdateDto.CompanyId.Value} no existe." });
                }
                if (user.CompanyId != userUpdateDto.CompanyId.Value)
                {
                    user.CompanyId = userUpdateDto.CompanyId.Value;
                    changed = true;
                }
            }

    // Validar CompanyId después de actualizar el rol, si el rol cambió.
    if (userUpdateDto.Role.HasValue && user.Role != userUpdateDto.Role.Value)
    {
        user.Role = userUpdateDto.Role.Value; // Actualiza el rol
        changed = true;
        // Re-validar CompanyId basado en el NUEVO rol
        if ((user.Role == Role.Usuario || user.Role == Role.Superusuario) && !user.CompanyId.HasValue)
        {
            return BadRequest(new { message = $"Se requiere una empresa para el rol {user.Role}." });
        }
    }
            if (!string.IsNullOrWhiteSpace(userUpdateDto.Email) && user.Email != userUpdateDto.Email)
            {
                if (await _context.Users.AnyAsync(u => u.Email == userUpdateDto.Email && u.Id != id))
                {
                    _logger.LogWarning($"Intento de actualizar a email existente: {userUpdateDto.Email}");
                    return Conflict(new { message = $"El email '{userUpdateDto.Email}' ya está en uso por otro usuario." });
                }
                user.Email = userUpdateDto.Email;
                changed = true;
            }

            if (userUpdateDto.PrivacyPolicyAccepted.HasValue && user.PrivacyPolicyAccepted != userUpdateDto.PrivacyPolicyAccepted.Value)
            {
                user.PrivacyPolicyAccepted = userUpdateDto.PrivacyPolicyAccepted.Value;
                changed = true;
            }

            if (userUpdateDto.Role.HasValue && user.Role != userUpdateDto.Role.Value)
            {
                user.Role = userUpdateDto.Role.Value;
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(userUpdateDto.NewPassword))
            {
                // Este es un cambio de contraseña forzado, por ejemplo por un admin.
                // O si es un usuario autenticado, se podría requerir la contraseña actual.
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(userUpdateDto.NewPassword);
                user.SetPasswordToken = null; // Invalidar cualquier token pendiente si se fuerza contraseña
                user.SetPasswordTokenExpiry = null;
                changed = true;
            }

            if (changed)
            {
                user.UpdatedAt = DateTime.UtcNow;
                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Usuario con ID: {id} actualizado correctamente.");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(id))
                    {
                        _logger.LogWarning($"Conflicto de concurrencia, usuario con ID: {id} no existe.");
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError("Error de concurrencia al actualizar usuario.");
                        throw;
                    }
                }
            }
            return NoContent();
        }

        // DELETE: api/Users/5 (sin cambios)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
             // ... (lógica existente) ...
             var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                _logger.LogWarning($"Usuario con ID: {id} no encontrado para eliminar.");
                return NotFound(new { message = $"Usuario con ID {id} no encontrado." });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Usuario con ID: {id} eliminado correctamente.");
            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }

        // Helper para generar tokens seguros
        private string GenerateSecureToken(int length = 32)
        {
            using (var randomNumberGenerator = RandomNumberGenerator.Create())
            {
                var randomBytes = new byte[length];
                randomNumberGenerator.GetBytes(randomBytes);
                return Convert.ToBase64String(randomBytes)
                    .Replace('+', '-') // URL-safe
                    .Replace('/', '_') // URL-safe
                    .TrimEnd('=');     // Quitar padding
            }
        }

        // Opcional: Endpoint simple para simular la página a la que lleva el email
        // En una app real, esto sería una página en tu frontend
        [HttpGet("set-password-page")]
        public IActionResult SetPasswordPage([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Token es requerido.");
            }
            // Simplemente devuelve una página HTML básica que podría tener un formulario
            // que haga un POST a /api/users/set-password
            var htmlContent = $@"
            <!DOCTYPE html>
            <html>
            <head><title>Establecer Contraseña</title></head>
            <body>
                <h1>Establecer tu Contraseña</h1>
                <p>Token recibido: {token}</p>
                <p>Usa este token en un cliente API (como Postman) para enviar una petición POST a <code>/api/users/set-password</code> con el token y tu nueva contraseña.</p>
                <form id=""setPasswordForm"">
                    <input type=""hidden"" name=""token"" value=""{token}"" />
                    <label for=""newPassword"">Nueva Contraseña:</label>
                    <input type=""password"" id=""newPassword"" name=""newPassword"" required minlength=""8"" /><br/><br/>
                    <button type=""submit"">Establecer Contraseña</button>
                </form>
                <div id=""message""></div>
                <script>
                    document.getElementById('setPasswordForm').addEventListener('submit', async function(event) {{
                        event.preventDefault();
                        const tokenVal = this.elements['token'].value;
                        const passwordVal = this.elements['newPassword'].value;
                        const messageDiv = document.getElementById('message');
                        messageDiv.textContent = 'Procesando...';

                        try {{
                            const response = await fetch('/api/users/set-password', {{
                                method: 'POST',
                                headers: {{ 'Content-Type': 'application/json' }},
                                body: JSON.stringify({{ token: tokenVal, newPassword: passwordVal }})
                            }});
                            const result = await response.json();
                            if (response.ok) {{
                                messageDiv.textContent = 'Éxito: ' + (result.message || 'Contraseña establecida.');
                                messageDiv.style.color = 'green';
                            }} else {{
                                messageDiv.textContent = 'Error: ' + (result.message || response.statusText || 'Error desconocido.');
                                messageDiv.style.color = 'red';
                            }}
                        }} catch (error) {{
                            messageDiv.textContent = 'Error de red: ' + error.message;
                            messageDiv.style.color = 'red';
                        }}
                    }});
                </script>
            </body>
            </html>";
            return Content(htmlContent, "text/html");
        }
    }
}