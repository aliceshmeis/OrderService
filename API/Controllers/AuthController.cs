using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.API.Services; // Changed from Business.Services
using OrderService.Domain.DTOs;
using OrderService.Domain.Models;

namespace OrderService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IJwtService jwtService, ILogger<AuthController> logger)
        {
            _jwtService = jwtService;
            _logger = logger;
        }

        /// <summary>
        /// User login authentication
        /// </summary>
        /// <param name="loginDto">Login credentials</param>
        /// <returns>JWT token if authentication successful</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<LoginResponseDto>), 200)]
        [ProducesResponseType(typeof(BaseResponse), 400)]
        [ProducesResponseType(typeof(BaseResponse), 401)]
        public async Task<ActionResult<BaseResponse<LoginResponseDto>>> Login([FromBody] LoginDto loginDto)
        {
            _logger.LogInformation("Login attempt for user: {Username}", loginDto.Username);

            if (!ModelState.IsValid)
            {
                return BadRequest(BaseResponse.Error("Invalid login data"));
            }

            var result = await _jwtService.AuthenticateAsync(loginDto);

            if (result == null)
            {
                _logger.LogWarning("Failed login attempt for user: {Username}", loginDto.Username);
                return Unauthorized(BaseResponse.Error("Invalid username or password", 401));
            }

            _logger.LogInformation("Successful login for user: {Username}", loginDto.Username);
            return Ok(BaseResponse<LoginResponseDto>.Success(result, "Login successful"));
        }

        /// <summary>
        /// User registration/signup
        /// </summary>
        /// <param name="signUpDto">Registration data</param>
        /// <returns>Success message if registration completed</returns>
        [HttpPost("signup")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<object>), 201)]
        [ProducesResponseType(typeof(BaseResponse), 400)]
        [ProducesResponseType(typeof(BaseResponse), 409)]
        [ProducesResponseType(typeof(BaseResponse), 500)]
        public async Task<ActionResult<BaseResponse<object>>> SignUp([FromBody] SignUpDto signUpDto)
        {
            _logger.LogInformation("POST /api/auth/signup - User registration attempt for {Username}", signUpDto?.Username);

            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return BadRequest(BaseResponse.Error($"Validation failed: {errors}", 400));
            }

            var result = await _jwtService.SignUpAsync(signUpDto);

            if (result.ErrorCode == 409)
            {
                _logger.LogWarning("Signup failed - user already exists: {Username}", signUpDto.Username);
                return Conflict(result);
            }
            if (result.ErrorCode == 400)
                return BadRequest(result);
            if (result.ErrorCode != 0)
                return StatusCode(500, result);

            _logger.LogInformation("Successful registration for user: {Username}", signUpDto.Username);
            return CreatedAtAction(nameof(SignUp), result);
        }
    }
}