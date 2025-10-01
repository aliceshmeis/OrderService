using OrderService.Domain.DTOs;
using OrderService.Domain.Models;

namespace OrderService.API.Services
{
    public interface IJwtService
    {
        string GenerateToken(UserDto user);
        Task<LoginResponseDto?> AuthenticateAsync(LoginDto loginDto);
        Task<BaseResponse<object>> SignUpAsync(SignUpDto signUpDto);
        Task<BaseResponse<IEnumerable<UserListDto>>> GetAllUsersAsync(); // Add this line
    }
}