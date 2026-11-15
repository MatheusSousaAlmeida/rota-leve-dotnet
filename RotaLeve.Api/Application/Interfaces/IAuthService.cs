using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Domain.Models;

namespace RotaLeve.Api.Application.Interfaces;

public interface IAuthService
{
    Task<OperationResult<AuthResponseDto>> LoginAsync(LoginRequestDto dto);
}
