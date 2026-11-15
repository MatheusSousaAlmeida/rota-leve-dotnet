using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Application.Interfaces;
using RotaLeve.Api.Domain.Entities;
using RotaLeve.Api.Domain.Enums;
using RotaLeve.Api.Domain.Interfaces;
using RotaLeve.Api.Domain.Models;
using RotaLeve.Api.Infrastructure.Security;

namespace RotaLeve.Api.Application.Services;

public class AuthService(IUsuarioRepository usuarios, IPasswordHasher hasher, IJwtTokenService tokenService) : IAuthService
{
    public async Task<OperationResult<AuthResponseDto>> LoginAsync(LoginRequestDto dto)
    {
        var emailNormalizado = dto.Email.Trim().ToLowerInvariant();
        var usuario = await usuarios.ObterPorEmailAsync(emailNormalizado);

        if (usuario is null || !usuario.Ativo || !hasher.Verify(dto.Senha, usuario.SenhaHash))
            return OperationResult<AuthResponseDto>.Failure("E-mail ou senha inválidos.", 401);

        var (token, expires) = tokenService.GerarToken(usuario);
        return OperationResult<AuthResponseDto>.Success(new AuthResponseDto(token, expires, ToDto(usuario)));
    }

    private static UsuarioResponseDto ToDto(Usuario u) =>
        new(u.IdUsuario, u.Nome, u.Email, u.TipoUsuario == TipoUsuario.Admin ? "admin" : "cidadao");
}
