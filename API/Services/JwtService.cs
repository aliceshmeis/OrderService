using Microsoft.IdentityModel.Tokens;
using OrderService.Domain.DTOs;
using OrderService.Domain.Models;
using OrderService.Persistence.UnitOfWork;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Npgsql;
using Dapper;


namespace OrderService.API.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<JwtService> _logger;

        public JwtService(IConfiguration configuration, IUnitOfWork unitOfWork, ILogger<JwtService> logger)
        {
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public string GenerateToken(UserDto user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"]);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("id", user.Id.ToString()), // Add this for consistency with inventory service
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("role", user.IsAdmin ? "Admin" : "User")
            };

            Console.WriteLine($"GENERATING JWT - UserID: {user.Id}, Username: {user.Username}, IsAdmin: {user.IsAdmin}, Role: {(user.IsAdmin ? "Admin" : "User")}");

            _logger.LogInformation("JWT Claims - UserID: {Id}, Role: {Role}, IsAdmin: {IsAdmin}",
                user.Id, user.IsAdmin ? "Admin" : "User", user.IsAdmin);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<LoginResponseDto?> AuthenticateAsync(LoginDto loginDto)
        {
            try
            {
                _logger.LogInformation("Attempting to authenticate user: {Username}", loginDto.Username);
                using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));

                // Keep original table name "login"
                var user = await connection.QuerySingleOrDefaultAsync<dynamic>(
                    "SELECT id, username, email, password_hash, is_admin FROM \"order\".\"login\" WHERE username = @username AND is_active = true AND is_deleted = false",
                    new { username = loginDto.Username });

                if (user == null)
                {
                    _logger.LogWarning("User not found: {Username}", loginDto.Username);
                    return null;
                }

                // ADD THESE DEBUG LINES
                _logger.LogInformation("Found user: {Username}", (string)user.username);
                _logger.LogInformation("Stored hash: {Hash}", (string)user.password_hash);
                _logger.LogInformation("Input password: {Password}", loginDto.Password);
                _logger.LogInformation("Hash length: {Length}", ((string)user.password_hash)?.Length ?? 0);

                // Verify password using BCrypt
                bool isValidPassword = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.password_hash);

                _logger.LogInformation("BCrypt verification result: {IsValid}", isValidPassword);

                if (!isValidPassword)
                {
                    _logger.LogWarning("Invalid password for user: {Username}", loginDto.Username);
                    return null;
                }

                _logger.LogInformation("User authenticated successfully: {Username}", loginDto.Username);

                var userDto = new UserDto
                {
                    Id = user.id,
                    Username = user.username,
                    Email = user.email,
                    IsAdmin = user.is_admin
                };

                _logger.LogInformation("UserDto created - ID: {Id}, Username: {Username}, IsAdmin: {IsAdmin}",
                    userDto.Id, userDto.Username, userDto.IsAdmin);

                var token = GenerateToken(userDto);
                return new LoginResponseDto
                {
                    Token = token,
                    User = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during authentication for user: {Username}", loginDto.Username);
                return null;
            }
        }

        public async Task<BaseResponse<object>> SignUpAsync(SignUpDto signUpDto)
        {
            try
            {
                _logger.LogInformation("Attempting to register user: {Username}", signUpDto.Username);

                _logger.LogInformation("Starting password hashing...");
                // Hash the password
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(signUpDto.Password);
                _logger.LogInformation("Password hashed successfully, hash length: {Length}", hashedPassword.Length);

                _logger.LogInformation("Calling CreateUserInDatabase with username: {Username}, email: {Email}",
                    signUpDto.Username, signUpDto.Email);

                // Call stored procedure to create user
                var result = await CreateUserInDatabase(signUpDto.Username, signUpDto.Email, hashedPassword);

                _logger.LogInformation("CreateUserInDatabase completed with ErrorCode: {ErrorCode}", result.ErrorCode);

                if (result.ErrorCode != 0)
                {
                    _logger.LogWarning("Signup failed - ErrorCode: {ErrorCode}, Message: {Message}",
                        result.ErrorCode, result.Message);
                    return result; // Return the error as-is
                }

                _logger.LogInformation("User registered successfully: {Username}", signUpDto.Username);
                return BaseResponse<object>.Success(new
                {
                    Username = signUpDto.Username,
                    Email = signUpDto.Email
                }, "User created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during user signup for: {Username} - Message: {Message}, StackTrace: {StackTrace}",
                    signUpDto.Username, ex.Message, ex.StackTrace);
                return BaseResponse<object>.Error("An error occurred during signup", 500);
            }
        }

        // Helper method to create user in database using stored procedure
        // Helper method to create user in database using stored procedure
        private async Task<BaseResponse<object>> CreateUserInDatabase(string username, string email, string passwordHash)
        {
            try
            {
                _logger.LogInformation("CreateUserInDatabase starting for username: {Username}, email: {Email}", username, email);
                _logger.LogInformation("Password hash length: {Length}", passwordHash?.Length ?? 0);

                using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));

                _logger.LogInformation("Database connection created, about to call stored procedure");

                var result = await connection.QuerySingleAsync<dynamic>(
                    "SELECT * FROM \"order\".sp_create_user(@p_username, @p_email, @p_password_hash)",
                    new
                    {
                        p_username = username,
                        p_email = email,
                        p_password_hash = passwordHash
                    });

                _logger.LogInformation("Stored procedure executed successfully");

                int errorCode = (int)result.errorcode;
                _logger.LogInformation("Stored procedure returned ErrorCode: {ErrorCode}", errorCode);

                if (errorCode == 0)
                {
                    _logger.LogInformation("User created successfully in database");
                    return BaseResponse<object>.Success("User created successfully");
                }
                else if (errorCode == 409)
                {
                    _logger.LogWarning("User creation failed - username or email already exists");
                    return BaseResponse<object>.Error("Username or email already exists", 409);
                }
                else
                {
                    _logger.LogWarning("User creation failed with error code: {ErrorCode}", errorCode);
                    return BaseResponse<object>.Error("Failed to create user", errorCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in CreateUserInDatabase for username {Username}: {Message}", username, ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                return BaseResponse<object>.Error("Database error occurred", 500);
            }
        }
    }
}