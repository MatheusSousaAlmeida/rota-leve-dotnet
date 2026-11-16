using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Application.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace RotaLeve.Api.Presentation.Controllers;

[ApiController]
[Route("api/localidades")]
[Authorize(Roles = "cidadao,admin")]
public class LocalidadesController(ILocalidadeService service) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting("listagens")]
    [SwaggerOperation(
        Summary = "Listar localidades ativas",
        Description = "Retorna todos os localidades ativas cadastrados na plataforma Rota Leve.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Localidades retornadas com sucesso", typeof(IEnumerable<LocalidadeResponseDto>))]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Nenhuma localidade ativa cadastrada")]
    [SwaggerResponse(StatusCodes.Status429TooManyRequests, "Limite de consultas da listagem excedido")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token JWT ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Usuário sem permissão para acessar o recurso")]
    public async Task<IActionResult> Get()
    {
        var itens = await service.ListarAsync();
        return !itens.Any() ? NoContent() : Ok(itens);
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(
        Summary = "Obter localidade por ID",
        Description = "Retorna os dados de uma localidade específico a partir do identificador informado.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Localidade encontrada", typeof(LocalidadeResponseDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token JWT ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Usuário sem permissão para acessar o recurso")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Localidade não encontrada")]
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
        Summary = "Cadastrar localidade",
        Description = "Cadastra uma nova localidade de acessibilidade. Operação restrita ao perfil admin.")]
    [SwaggerResponse(StatusCodes.Status201Created, "Localidade cadastrada com sucesso", typeof(LocalidadeResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados da localidade inválidos")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token JWT ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Apenas administradores podem cadastrar localidades")]
    public async Task<IActionResult> Post(LocalidadeRequestDto dto)
    {
        var r = await service.CriarAsync(dto);
        return StatusCode(r.StatusCode, r.Value);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(
        Summary = "Atualizar localidade",
        Description = "Atualiza os dados de uma localidade existente. Operação restrita ao perfil admin.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Localidade atualizada com sucesso", typeof(LocalidadeResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados da localidade inválidos")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token JWT ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Apenas administradores podem atualizar localidades")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Localidade não encontrada")]
    public async Task<IActionResult> Put(int id, LocalidadeRequestDto dto)
    {
        var r = await service.AtualizarAsync(id, dto);
        return r.IsSuccess
            ? Ok(r.Value)
            : NotFound(new { erro = r.Error });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(
        Summary = "Desativar localidade",
        Description = "Realiza a exclusão lógica/desativação da localidade informado. Operação restrita ao perfil admin.")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Localidade desativada com sucesso")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token JWT ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Apenas administradores podem desativar localidades")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Localidade não encontrada")]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await service.ExcluirAsync(id);
        return r.IsSuccess
            ? NoContent()
            : NotFound(new { erro = r.Error });
    }
}
