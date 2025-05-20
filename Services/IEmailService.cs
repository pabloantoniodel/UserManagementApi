// Services/IEmailService.cs
namespace UserManagementApi.Services
{
    public interface IEmailService
    {
        Task SendSetPasswordEmailAsync(string toEmail, string username, string token, string callbackUrlBase);
        Task SendResetPasswordEmailAsync(string toEmail, string username, string token, string callbackUrlBase);
    }
 }
