using UserService.Dtos;
using UserService.Model;

namespace UserService.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResultDto> RegisterAsync(string email, string password, string? firstName, string? lastName);
        Task<AuthResultDto> LoginAsync(string email, string password);
        Task<ApplicationUser?> GetUserByIdAsync(string userId);
    }
}
