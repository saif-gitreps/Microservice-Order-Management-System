using Microsoft.AspNetCore.Identity;
using UserService.Interfaces;
using UserService.Model;

namespace UserService.Repository
{
    public class UserRepository: IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public UserRepository(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<ApplicationUser?> FindByEmailAsync(string email)
        { 
            return await _userManager.FindByEmailAsync(email);
        }
        public async Task<ApplicationUser?> FindByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }
        public async Task<IdentityResult> CreateAsync(ApplicationUser user, string password)
        {
            return await _userManager.CreateAsync(user, password);
        }
        public async Task<IdentityResult> UpdateAsync(ApplicationUser user)
        {

            return await _userManager.UpdateAsync(user);
        }
        public async Task<bool> CheckPasswordAsync(ApplicationUser user, string password)
        {
            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
            return result.Succeeded;
        }
    }
}
