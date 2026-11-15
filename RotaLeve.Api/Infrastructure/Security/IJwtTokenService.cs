using RotaLeve.Api.Domain.Entities;
namespace RotaLeve.Api.Infrastructure.Security;
public interface IJwtTokenService { (string Token, DateTime ExpiresAtUtc) GerarToken(Usuario usuario); }
