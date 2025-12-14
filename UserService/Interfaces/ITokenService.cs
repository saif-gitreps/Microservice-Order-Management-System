using UserService.Model;

namespace UserService.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(ApplicationUser user);
    }
}
