// Services/ConsoleEmailService.cs
namespace UserManagementApi.Services
{
    public class ConsoleEmailService : IEmailService
    {
        private readonly ILogger<ConsoleEmailService> _logger;
        private readonly IConfiguration _configuration;

        public ConsoleEmailService(ILogger<ConsoleEmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public Task SendSetPasswordEmailAsync(string toEmail, string username, string token, string callbackUrlBase)
        {
            // En una aplicación real, usarías una librería como MailKit o un servicio como SendGrid.
            // Por ahora, solo lo mostraremos en la consola.

            // El callbackUrlBase podría venir de appsettings.json si tienes un frontend
            // string frontendSetPasswordUrl = _configuration["AppSettings:FrontendSetPasswordUrl"];
            // string setPasswordLink = $"{frontendSetPasswordUrl}?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(toEmail)}";
            // Para esta demo, construiremos un link directo a un endpoint (aunque no es ideal para UX)

            string setPasswordLink = $"{callbackUrlBase}/api/users/set-password-page?token={Uri.EscapeDataString(token)}"; // Este es un link de ejemplo

            _logger.LogInformation("---- SIMULANDO ENVÍO DE EMAIL ----");
            _logger.LogInformation($"Para: {toEmail}");
            _logger.LogInformation($"Asunto: Establece tu contraseña en Megasolución");
            _logger.LogInformation($"Hola {username},");
            _logger.LogInformation($"Gracias por registrarte. Por favor, establece tu contraseña haciendo clic en el siguiente enlace:");
            _logger.LogInformation(setPasswordLink);
            _logger.LogInformation($"Este enlace expirará en 24 horas.");
            _logger.LogInformation("------------------------------------");

            return Task.CompletedTask;
        }
        // ... (método existente) ...

        //Metodo de resstablecer contraseña
        public Task SendResetPasswordEmailAsync(string toEmail, string username, string token, string callbackUrlBase)
        {
            // El callbackUrlBase debería apuntar a tu frontend Angular
            // ej: http://localhost:4200/reset-password
            string resetPasswordLink = $"{callbackUrlBase}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(toEmail)}";

            _logger.LogInformation("---- SIMULANDO ENVÍO DE EMAIL (RESETEAR CONTRASEÑA) ----");
            _logger.LogInformation($"Para: {toEmail}");
            _logger.LogInformation($"Asunto: Restablece tu contraseña en Megasolución");
            _logger.LogInformation($"Hola {username},");
            _logger.LogInformation($"Hemos recibido una solicitud para restablecer tu contraseña. Si no has sido tú, ignora este email.");
            _logger.LogInformation($"Para restablecer tu contraseña, haz clic en el siguiente enlace:");
            _logger.LogInformation(resetPasswordLink);
            _logger.LogInformation($"Este enlace expirará en 1 hora.");
            _logger.LogInformation("------------------------------------");

            return Task.CompletedTask;
        }
    }
}