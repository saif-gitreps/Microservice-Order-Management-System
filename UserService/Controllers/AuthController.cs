using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;
using UserService.Dtos;
using UserService.Interfaces;

namespace UserService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

       
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
          
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterAsync(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName);

            if (!result.Success)
            {
             
                return BadRequest(new { errors = result.Errors });
            }

            return Ok(new
            {
                token = result.Token,
                userId = result.UserId,
                message = "User registered successfully"
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.LoginAsync(request.Email, request.Password);

            if (!result.Success)
            {
           
                return Unauthorized(new { message = "Invalid email or password" });
            }

            return Ok(new
            {
                token = result.Token,
                userId = result.UserId,
                message = "Login successful"
            });
        }


        [HttpGet("me")]
        [Authorize] 
        public async Task<IActionResult> GetCurrentUser()
        {
            
            var userId = User.Identity?.Name;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                id = user.Id,
                email = user.Email,
                userName = user.UserName,
                firstName = user.FirstName,
                lastName = user.LastName
            });
        }
    }

}
