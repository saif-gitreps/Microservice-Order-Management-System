using Microsoft.AspNetCore.Identity;
using UserService.Model;

namespace UserService.Interfaces
{
    public interface IUserRepository
    {
        Task<ApplicationUser?> FindByEmailAsync(string email);
        Task<ApplicationUser?> FindByIdAsync(string userId);
        Task<IdentityResult> CreateAsync(ApplicationUser user, string password);
        Task<IdentityResult> UpdateAsync(ApplicationUser user);
        Task<bool> CheckPasswordAsync(ApplicationUser user, string password);
    }
}
