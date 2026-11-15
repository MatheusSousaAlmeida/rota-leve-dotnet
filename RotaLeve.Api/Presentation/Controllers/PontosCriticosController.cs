using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Application.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace RotaLeve.Api.Presentation.Controllers;

[ApiController]
[Route("api/pontos-criticos")]
[Authorize(Roles = "cidadao,admin")]
public class PontosCriticosController(IPontoCriticoService service) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(
        Summary = "Listar pontos críticos ativos",
        Description = "Retorna todos os pontos críticos ativos cadastrados na plataforma Rota Leve.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Pontos críticos retornados com sucesso", typeof(IEnumerable<PontoCriticoResponseDto>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token JWT ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Usuário sem permissão para acessar o recurso")]
    public async Task<IActionResult> Get()
        => Ok(await service.ListarAsync());

    [HttpGet("{id:int}")]
    [SwaggerOperation(
        Summary = "Obter ponto crítico por ID",
        Description = "Retorna os dados de um ponto crítico específico a partir do identificador informado.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Ponto crítico encontrado", typeof(PontoCriticoResponseDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token JWT ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Usuário sem permissão para acessar o recurso")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Ponto crítico não encontrado")]
    public async Task<IActionResult> Get(int id)
    {
        var r = await service.ObterAsync(id);
        return r.IsSuccess
            ? Ok(r.Value)
            : NotFound(new { erro = r.Error });
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(
        Summary = "Cadastrar ponto crítico",
        Description = "Cadastra um novo ponto crítico de acessibilidade. Operação restrita ao perfil admin.")]
    [SwaggerResponse(StatusCodes.Status201Created, "Ponto crítico cadastrado com sucesso", typeof(PontoCriticoResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados do ponto crítico inválidos")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token JWT ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Apenas administradores podem cadastrar pontos críticos")]
    public async Task<IActionResult> Post(PontoCriticoRequestDto dto)
    {
        var r = await service.CriarAsync(dto);
        return StatusCode(r.StatusCode, r.Value);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(
        Summary = "Atualizar ponto crítico",
        Description = "Atualiza os dados de um ponto crítico existente. Operação restrita ao perfil admin.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Ponto crítico atualizado com sucesso", typeof(PontoCriticoResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados do ponto crítico inválidos")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token JWT ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Apenas administradores podem atualizar pontos críticos")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Ponto crítico não encontrado")]
    public async Task<IActionResult> Put(int id, PontoCriticoRequestDto dto)
    {
        var r = await service.AtualizarAsync(id, dto);
        return r.IsSuccess
            ? Ok(r.Value)
            : NotFound(new { erro = r.Error });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(
        Summary = "Desativar ponto crítico",
        Description = "Realiza a exclusão lógica/desativação do ponto crítico informado. Operação restrita ao perfil admin.")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Ponto crítico desativado com sucesso")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token JWT ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Apenas administradores podem desativar pontos críticos")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Ponto crítico não encontrado")]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await service.ExcluirAsync(id);
        return r.IsSuccess
            ? NoContent()
            : NotFound(new { erro = r.Error });
    }
}
