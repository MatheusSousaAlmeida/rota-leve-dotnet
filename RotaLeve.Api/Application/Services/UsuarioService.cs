using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Application.Interfaces;
using RotaLeve.Api.Domain.Entities;
using RotaLeve.Api.Domain.Enums;
using RotaLeve.Api.Domain.Interfaces;
using RotaLeve.Api.Domain.Models;
using RotaLeve.Api.Infrastructure.Security;

namespace RotaLeve.Api.Application.Services;

public class UsuarioService(IUsuarioRepository usuarios, IPasswordHasher hasher) : IUsuarioService
{
    public async Task<IEnumerable<UsuarioDetalheResponseDto>> ListarAsync()
        => (await usuarios.ListarAsync()).Select(ToDto);

    public async Task<OperationResult<UsuarioDetalheResponseDto>> ObterAsync(int id)
    {
        var usuario = await usuarios.ObterPorIdAsync(id);
        return usuario is null
            ? OperationResult<UsuarioDetalheResponseDto>.Failure("Usuário não encontrado.", 404)
            : OperationResult<UsuarioDetalheResponseDto>.Success(ToDto(usuario));
    }

    public async Task<OperationResult<UsuarioDetalheResponseDto>> CriarAsync(UsuarioCreateRequestDto dto)
    {
        var emailNormalizado = dto.Email.Trim().ToLowerInvariant();
        if (await usuarios.ObterPorEmailAsync(emailNormalizado) is not null)
            return OperationResult<UsuarioDetalheResponseDto>.Failure("E-mail já cadastrado.", 409);

        var usuario = new Usuario
        {
            Nome = dto.Nome.Trim(),
            Email = emailNormalizado,
            SenhaHash = hasher.Hash(dto.Senha),
            TipoUsuario = ParsePerfil(dto.Perfil),
            Ativo = true
        };

        usuario = await usuarios.AdicionarAsync(usuario);
        return OperationResult<UsuarioDetalheResponseDto>.Success(ToDto(usuario), 201);
    }

    public async Task<OperationResult<UsuarioDetalheResponseDto>> AtualizarAsync(int id, UsuarioUpdateRequestDto dto)
    {
        var usuario = await usuarios.ObterPorIdAsync(id);
        if (usuario is null)
            return OperationResult<UsuarioDetalheResponseDto>.Failure("Usuário não encontrado.", 404);

        var emailNormalizado = dto.Email.Trim().ToLowerInvariant();
        var outroComEmail = await usuarios.ObterPorEmailAsync(emailNormalizado);
        if (outroComEmail is not null && outroComEmail.IdUsuario != id)
            return OperationResult<UsuarioDetalheResponseDto>.Failure("E-mail já cadastrado para outro usuário.", 409);

        usuario.Nome = dto.Nome.Trim();
        usuario.Email = emailNormalizado;
        usuario.TipoUsuario = ParsePerfil(dto.Perfil);
        usuario.Ativo = dto.Ativo;

        if (!string.IsNullOrWhiteSpace(dto.Senha))
            usuario.SenhaHash = hasher.Hash(dto.Senha);

        await usuarios.AtualizarAsync(usuario);
        return OperationResult<UsuarioDetalheResponseDto>.Success(ToDto(usuario));
    }

    public async Task<OperationResult<bool>> ExcluirAsync(int id)
    {
        var usuario = await usuarios.ObterPorIdAsync(id);
        if (usuario is null)
            return OperationResult<bool>.Failure("Usuário não encontrado.", 404);

        usuario.Ativo = false;
        await usuarios.AtualizarAsync(usuario);
        return OperationResult<bool>.Success(true, 204);
    }

    private static TipoUsuario ParsePerfil(string perfil)
        => perfil.Trim().Equals("admin", StringComparison.OrdinalIgnoreCase)
            ? TipoUsuario.Admin
            : TipoUsuario.Cidadao;

    private static UsuarioDetalheResponseDto ToDto(Usuario usuario)
        => new(
            usuario.IdUsuario,
            usuario.Nome,
            usuario.Email,
            usuario.TipoUsuario == TipoUsuario.Admin ? "admin" : "cidadao",
            usuario.Ativo,
            usuario.DataCriacao);
}
