using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using RotaLeve.Api.Domain.Entities;
using RotaLeve.Api.Domain.Enums;

namespace RotaLeve.Api.Infrastructure.Security;

public class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public (string Token, DateTime ExpiresAtUtc) GerarToken(Usuario usuario)
    {
        var secret = configuration["Secretkey"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("Secretkey não configurada.");

        var expires = DateTime.UtcNow.AddHours(8);
        var role = usuario.TipoUsuario == TipoUsuario.Admin ? "admin" : "cidadao";
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, role)
        };
        var credentials = new SigningCredentials(JwtKeyHelper.Create(secret), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(claims: claims, expires: expires, signingCredentials: credentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
