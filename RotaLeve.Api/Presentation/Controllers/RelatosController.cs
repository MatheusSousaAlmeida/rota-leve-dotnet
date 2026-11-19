using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Application.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace RotaLeve.Api.Presentation.Controllers;

[ApiController]
[Route("api/relatos")]
[Authorize(Roles = "cidadao,admin")]
public class RelatosController(IRelatoService service) : ControllerBase
{
    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool Admin => User.IsInRole("admin");

    [HttpGet]
    [EnableRateLimiting("listagens")]
    [SwaggerOperation(
        Summary = "Listar relatos colaborativos",
        Description = "Retorna relatos em páginas, do mais recente para o mais antigo, com status e confirmações. Parâmetros: pagina (padrão 1, máximo 1000000) e tamanhoPagina (padrão 10, máximo 100).")]
    [SwaggerResponse(StatusCodes.Status200OK, "Relatos retornados com sucesso", typeof(PaginacaoResponseDto<RelatoResponseDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Parâmetros de paginação inválidos")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Nenhum relato cadastrado")]
    [SwaggerResponse(StatusCodes.Status429TooManyRequests, "Limite de consultas da listagem excedido")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token JWT ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Usuário sem permissão para acessar o recurso")]
    public async Task<IActionResult> Get([FromQuery] PaginacaoRequestDto paginacao)
    {
        var resultado = await service.ListarPaginadoAsync(paginacao.Pagina, paginacao.TamanhoPagina);
        return resultado.Dados.Count == 0 ? NoContent() : Ok(resultado);
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(
        Summary = "Obter relato por ID",
        Description = "Retorna os dados detalhados de um relato específico.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Relato encontrado", typeof(RelatoResponseDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token JWT ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Usuário sem permissão para acessar o recurso")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Relato não encontrado")]
    public async Task<IActionResult> Get(int id)
    {
        var r = await service.ObterAsync(id);
        return r.IsSuccess
            ? Ok(r.Value)
            : NotFound(new { erro = r.Error });
    }

    [HttpPost]
    [EnableRateLimiting("relatos")]
    [SwaggerOperation(
        Summary = "Criar relato",
        Description = "Cria um relato com Idempotency-Key obrigatório. Primeira execução: 201. Mesma chave e conteúdo: 200 e relato existente. Mesma chave e dados diferentes: 409. Rate Limiting protege contra spam.")]
    [SwaggerResponse(StatusCodes.Status201Created, "Relato criado com sucesso", typeof(RelatoResponseDto))]
    [SwaggerResponse(StatusCodes.Status200OK, "Requisição repetida: relato existente", typeof(RelatoResponseDto))]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Chave reutilizada com conteúdo diferente")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos ou localidade inexistente/inativo")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token JWT ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Usuário sem permissão para criar relatos")]
    [SwaggerResponse(StatusCodes.Status429TooManyRequests, "Limite de criação de relatos excedido")]
    public async Task<IActionResult> Post(RelatoRequestDto dto, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        if (!RotaLeve.Api.Application.Services.IdempotenciaRelato.ChaveValida(idempotencyKey))
            return BadRequest(new { erro = "Header Idempotency-Key obrigatório (1 a 200 caracteres ASCII visíveis)." });
        var r = await service.CriarAsync(UsuarioId, dto, idempotencyKey!);
        return r.IsSuccess
            ? StatusCode(r.StatusCode, r.Value)
            : StatusCode(r.StatusCode, new { erro = r.Error });
    }

    [HttpPut("{id:int}")]
    [SwaggerOperation(
        Summary = "Atualizar relato",
        Description = "Permite ao cidadão atualizar apenas um relato próprio ainda em status Novo. O admin pode atualizar qualquer relato.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Relato atualizado com sucesso", typeof(RelatoResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos ou localidade inexistente/inativo")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token JWT ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Cidadão tentando alterar relato de outro usuário")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Relato não encontrado")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Relato não pode ser alterado no estado atual")]
    public async Task<IActionResult> Put(int id, RelatoRequestDto dto)
    {
        var r = await service.AtualizarAsync(id, UsuarioId, Admin, dto);
        return r.IsSuccess
            ? Ok(r.Value)
            : StatusCode(r.StatusCode, new { erro = r.Error });
    }

    [HttpDelete("{id:int}")]
    [SwaggerOperation(
        Summary = "Excluir relato",
        Description = "Permite ao cidadão excluir apenas um relato próprio ainda em status Novo. O admin pode excluir qualquer relato.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Relato excluído com sucesso")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token JWT ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Cidadão tentando excluir relato de outro usuário")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Relato não encontrado")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Relato não pode ser excluído no estado atual")]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await service.ExcluirAsync(id, UsuarioId, Admin);
        return r.IsSuccess
            ? Ok(new { mensagem = "Relato excluído com sucesso.", relatoId = id })
            : StatusCode(r.StatusCode, new { erro = r.Error });
    }

    [HttpPost("{id:int}/confirmacoes")]
    [EnableRateLimiting("confirmacoes")]
    [SwaggerOperation(
        Summary = "Confirmar relato",
        Description = "Registra que o usuário autenticado confirma que o problema relatado ainda existe. O autor não pode confirmar o próprio relato e cada usuário pode confirmar um relato apenas uma vez.")]
    [SwaggerResponse(StatusCodes.Status201Created, "Confirmação registrada com sucesso", typeof(ConfirmacaoResponseDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token JWT ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Usuário sem permissão para confirmar relatos")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Relato não encontrado")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Confirmação inválida, duplicada ou relato indisponível para confirmação")]
    [SwaggerResponse(StatusCodes.Status429TooManyRequests, "Limite de confirmações excedido")]
    public async Task<IActionResult> Confirmar(int id)
    {
        var r = await service.ConfirmarAsync(id, UsuarioId);
        return r.IsSuccess
            ? StatusCode(r.StatusCode, r.Value)
            : StatusCode(r.StatusCode, new { erro = r.Error });
    }

    [HttpDelete("{id:int}/confirmacoes/minha")]
    [SwaggerOperation(
        Summary = "Remover minha confirmação",
        Description = "Remove a confirmação do usuário autenticado para o relato informado. Caso não existam mais confirmações, o relato retorna para o status Novo, conforme a regra de negócio.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Confirmação removida com sucesso")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token JWT ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Usuário sem permissão para acessar o recurso")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Relato ou confirmação não encontrados")]
    public async Task<IActionResult> RemoverConfirmacao(int id)
    {
        var r = await service.RemoverConfirmacaoAsync(id, UsuarioId);
        return r.IsSuccess
            ? Ok(new { mensagem = "Confirmação removida com sucesso.", relatoId = id })
            : StatusCode(r.StatusCode, new { erro = r.Error });
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(
        Summary = "Alterar status do relato",
        Description = "Permite ao admin alterar o status de um relato. Relatos resolvidos não podem ser reabertos; caso o problema retorne, deve ser criado um novo relato.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Status do relato atualizado com sucesso", typeof(RelatoResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Status informado é inválido")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token JWT ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Apenas administradores podem alterar o status do relato")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Relato não encontrado")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Relato resolvido não pode ser reaberto")]
    public async Task<IActionResult> AlterarStatus(int id, AlterarStatusRelatoDto dto)
    {
        var r = await service.AlterarStatusAsync(id, dto.Status);
        return r.IsSuccess
            ? Ok(r.Value)
            : StatusCode(r.StatusCode, new { erro = r.Error });
    }
}
