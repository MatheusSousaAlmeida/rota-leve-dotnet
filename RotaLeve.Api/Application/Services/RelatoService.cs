using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Application.Interfaces;
using RotaLeve.Api.Domain.Entities;
using RotaLeve.Api.Domain.Enums;
using RotaLeve.Api.Domain.Interfaces;
using RotaLeve.Api.Domain.Models;

namespace RotaLeve.Api.Application.Services;

public class RelatoService(IRelatoRepository relatos, IConfirmacaoRepository confirmacoes, ILocalidadeRepository pontos) : IRelatoService
{
    private static readonly TimeSpan Expiracao = TimeSpan.FromHours(72);

    public async Task<PaginacaoResponseDto<RelatoResponseDto>> ListarPaginadoAsync(int pagina, int tamanhoPagina)
    {
        await relatos.ExpirarAntigosAsync(DateTime.UtcNow.Subtract(Expiracao));
        var (itens, total) = await relatos.ListarPaginadoAsync(pagina, tamanhoPagina);
        return new PaginacaoResponseDto<RelatoResponseDto>(
            pagina, tamanhoPagina, total, (int)Math.Ceiling(total / (double)tamanhoPagina),
            itens.Select(ToDto).ToList());
    }

    public async Task<List<RelatoResponseDto>> ListarAsync()
    {
        await relatos.ExpirarAntigosAsync(DateTime.UtcNow.Subtract(Expiracao));
        return (await relatos.ListarAsync()).Select(ToDto).ToList();
    }

    public async Task<OperationResult<RelatoResponseDto>> ObterAsync(int id)
    {
        await relatos.ExpirarAntigosAsync(DateTime.UtcNow.Subtract(Expiracao));
        var e = await relatos.ObterAsync(id); return e is null ? OperationResult<RelatoResponseDto>.Failure("Relato não encontrado.", 404) : OperationResult<RelatoResponseDto>.Success(ToDto(e));
    }

    public async Task<OperationResult<RelatoResponseDto>> CriarAsync(int usuarioId, RelatoRequestDto dto, string chaveIdempotencia)
    {
        if (!IdempotenciaRelato.ChaveValida(chaveIdempotencia))
            return OperationResult<RelatoResponseDto>.Failure("Header Idempotency-Key obrigatório (1 a 200 caracteres ASCII visíveis).", 400);

        var hashChave = IdempotenciaRelato.GerarHashChave(usuarioId, chaveIdempotencia);
        var hashRequisicao = IdempotenciaRelato.GerarHashRequisicao(dto);
        var existente = await relatos.ObterPorChaveIdempotenciaAsync(hashChave);
        if (existente is not null) return ResponderRepeticao(existente, hashRequisicao);

        var localidade = await pontos.ObterAsync(dto.LocalidadeId);
        if (localidade is null || !localidade.Ativo)
            return OperationResult<RelatoResponseDto>.Failure("Localidade não encontrada ou inativa.", 400);

        var entity = new Relato
        {
            IdUsuario = usuarioId, IdLocalidade = dto.LocalidadeId, Tipo = dto.Tipo,
            Descricao = dto.Descricao.Trim(), Latitude = dto.Latitude, Longitude = dto.Longitude,
            Status = StatusRelato.Novo, ChaveIdempotencia = hashChave, HashRequisicao = hashRequisicao
        };
        var (salvo, criado) = await relatos.AdicionarIdempotenteAsync(entity);
        return criado
            ? OperationResult<RelatoResponseDto>.Success(ToDto(salvo), 201)
            : ResponderRepeticao(salvo, hashRequisicao);
    }

    private static OperationResult<RelatoResponseDto> ResponderRepeticao(Relato existente, string hashRequisicao) =>
        !string.Equals(existente.HashRequisicao, hashRequisicao, StringComparison.Ordinal)
            ? OperationResult<RelatoResponseDto>.Failure("Idempotency-Key já utilizada com dados diferentes.", 409)
            : OperationResult<RelatoResponseDto>.Success(ToDto(existente), 200);

    public async Task<OperationResult<RelatoResponseDto>> AtualizarAsync(int id, int usuarioId, bool admin, RelatoRequestDto dto)
    {
        var e = await relatos.ObterAsync(id, true); if (e is null) return OperationResult<RelatoResponseDto>.Failure("Relato não encontrado.", 404);
        if (!admin && e.IdUsuario != usuarioId) return OperationResult<RelatoResponseDto>.Failure("Você só pode alterar seus próprios relatos.", 403);
        if (!admin && e.Status != StatusRelato.Novo) return OperationResult<RelatoResponseDto>.Failure("Relatos com confirmações ou finalizados não podem ser alterados pelo cidadão.", 409);
        var ponto = await pontos.ObterAsync(dto.LocalidadeId); if (ponto is null || !ponto.Ativo) return OperationResult<RelatoResponseDto>.Failure("Localidade não encontrado ou inativo.", 400);
        e.IdLocalidade = dto.LocalidadeId; e.Tipo = dto.Tipo; e.Descricao = dto.Descricao.Trim(); e.Latitude = dto.Latitude; e.Longitude = dto.Longitude;
        await relatos.AtualizarAsync(e); return OperationResult<RelatoResponseDto>.Success(ToDto(e));
    }

    public async Task<OperationResult<bool>> ExcluirAsync(int id, int usuarioId, bool admin)
    {
        var e = await relatos.ObterAsync(id); if (e is null) return OperationResult<bool>.Failure("Relato não encontrado.", 404);
        if (!admin && e.IdUsuario != usuarioId) return OperationResult<bool>.Failure("Você só pode excluir seus próprios relatos.", 403);
        if (!admin && e.Status != StatusRelato.Novo) return OperationResult<bool>.Failure("Relatos com confirmações ou finalizados não podem ser excluídos pelo cidadão.", 409);
        await relatos.ExcluirAsync(id); return OperationResult<bool>.Success(true, 200);
    }

    public async Task<OperationResult<ConfirmacaoResponseDto>> ConfirmarAsync(int relatoId, int usuarioId)
    {
        var relato = await relatos.ObterAsync(relatoId, true); if (relato is null) return OperationResult<ConfirmacaoResponseDto>.Failure("Relato não encontrado.", 404);
        if (relato.IdUsuario == usuarioId) return OperationResult<ConfirmacaoResponseDto>.Failure("O autor não pode confirmar o próprio relato.", 409);
        if (relato.Status is StatusRelato.Resolvido or StatusRelato.Expirado) return OperationResult<ConfirmacaoResponseDto>.Failure("Relato resolvido ou expirado não aceita confirmações.", 409);
        if (await confirmacoes.ExisteAsync(relatoId, usuarioId)) return OperationResult<ConfirmacaoResponseDto>.Failure("Este usuário já confirmou o relato.", 409);
        var conf = await confirmacoes.AdicionarAsync(new ConfirmacaoRelato { IdRelato = relatoId, IdUsuario = usuarioId, DataConfirmacao = DateTime.UtcNow });
        relato.Status = StatusRelato.Ativo; relato.DataUltimaConfirmacao = conf.DataConfirmacao; await relatos.AtualizarAsync(relato);
        return OperationResult<ConfirmacaoResponseDto>.Success(new ConfirmacaoResponseDto(conf.IdConfirmacao, conf.IdRelato, conf.IdUsuario, conf.DataConfirmacao), 201);
    }

    public async Task<OperationResult<bool>> RemoverConfirmacaoAsync(int relatoId, int usuarioId)
    {
        var relato = await relatos.ObterAsync(relatoId, true); if (relato is null) return OperationResult<bool>.Failure("Relato não encontrado.", 404);
        if (!await confirmacoes.RemoverAsync(relatoId, usuarioId)) return OperationResult<bool>.Failure("Confirmação não encontrada.", 404);
        if (await confirmacoes.ContarAsync(relatoId) == 0 && relato.Status == StatusRelato.Ativo) { relato.Status = StatusRelato.Novo; relato.DataUltimaConfirmacao = null; await relatos.AtualizarAsync(relato); }
        return OperationResult<bool>.Success(true, 200);
    }

    public async Task<OperationResult<RelatoResponseDto>> AlterarStatusAsync(int relatoId, StatusRelato status)
    {
        var relato = await relatos.ObterAsync(relatoId, true); if (relato is null) return OperationResult<RelatoResponseDto>.Failure("Relato não encontrado.", 404);
        if (relato.Status == StatusRelato.Resolvido && status != StatusRelato.Resolvido) return OperationResult<RelatoResponseDto>.Failure("Relato resolvido não é reaberto. Crie um novo relato caso o problema retorne.", 409);
        relato.Status = status; relato.DataResolucao = status == StatusRelato.Resolvido ? DateTime.UtcNow : null; await relatos.AtualizarAsync(relato);
        return OperationResult<RelatoResponseDto>.Success(ToDto(relato));
    }

    private static RelatoResponseDto ToDto(Relato e) => new(e.IdRelato, e.IdUsuario, e.IdLocalidade, e.Tipo, e.Descricao, e.Latitude, e.Longitude, e.Status, e.Confirmacoes?.Count ?? 0, e.DataCriacao, e.DataUltimaConfirmacao, e.DataResolucao);
}
