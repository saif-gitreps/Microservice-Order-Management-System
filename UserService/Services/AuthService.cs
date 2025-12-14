using UserService.Dtos;
using UserService.Interfaces;
using UserService.Model;

namespace UserService.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        public AuthService(IUserRepository userRepository, ITokenService tokenService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
        }

        public async Task<AuthResultDto> RegisterAsync(string email, string password, string? firstName, string? lastName)
        { 
            var existingUser = await _userRepository.FindByEmailAsync(email);
            if (existingUser != null)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Errors = new List<string> { "User with this email already exists." }
                };
            }

            var user = new ApplicationUser
            {
                Email = email,
                UserName = email,
                FirstName = firstName,
                LastName = lastName,
                CreatedAt = DateTime.UtcNow
            };
   
            var result = await _userRepository.CreateAsync(user, password);

            if (!result.Succeeded)
            { 
                return new AuthResultDto
                {
                    Success = false,
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }

            var token = _tokenService.GenerateToken(user);

            return new AuthResultDto
            {
                Success = true,
                Token = token,
                UserId = user.Id
            };
        }

        public async Task<AuthResultDto> LoginAsync(string email, string password)
        {
            var user = await _userRepository.FindByEmailAsync(email);
            if (user == null)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Errors = new List<string> { "Invalid email or password." }
                };
            }

            var passwordValid = await _userRepository.CheckPasswordAsync(user, password);
            if (!passwordValid)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Errors = new List<string> { "Invalid email or password." }
                };
            }

            var token = _tokenService.GenerateToken(user);

            return new AuthResultDto
            {
                Success = true,
                Token = token,
                UserId = user.Id
            };
        }


        public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
        {
            return await _userRepository.FindByIdAsync(userId);
        }
    }
}
