using RotaLeve.Api.Domain.Entities;
namespace RotaLeve.Api.Domain.Interfaces;
public interface IConfirmacaoRepository
{
    Task<bool> ExisteAsync(int relatoId, int usuarioId);
    Task<int> ContarAsync(int relatoId);
    Task<ConfirmacaoRelato> AdicionarAsync(ConfirmacaoRelato entity);
    Task<ConfirmacaoRelato?> ObterAsync(int relatoId, int usuarioId);
    Task<bool> RemoverAsync(int relatoId, int usuarioId);
}
