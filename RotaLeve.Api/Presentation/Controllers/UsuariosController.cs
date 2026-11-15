using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Application.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace RotaLeve.Api.Presentation.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = "admin")]
public class UsuariosController(IUsuarioService service) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting("listagens")]
    [SwaggerOperation(
        Summary = "Listar usuários",
        Description = "Retorna todos os usuários cadastrados na plataforma, incluindo usuários ativos e inativos. Operação restrita ao perfil admin.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Usuários retornados com sucesso", typeof(IEnumerable<UsuarioDetalheResponseDto>))]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Nenhum usuário cadastrado")]
    [SwaggerResponse(StatusCodes.Status429TooManyRequests, "Limite de consultas da listagem excedido")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token JWT ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Apenas administradores podem consultar a lista de usuários")]
    public async Task<IActionResult> Get()
    {
        var itens = await service.ListarAsync();
        return !itens.Any() ? NoContent() : Ok(itens);
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(
        Summary = "Obter usuário por ID",
        Description = "Retorna os dados cadastrais de um usuário específico. A senha e o hash da senha nunca são retornados. Operação restrita ao perfil admin.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Usuário encontrado", typeof(UsuarioDetalheResponseDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token JWT ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Apenas administradores podem consultar usuários")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Usuário não encontrado")]
    public async Task<IActionResult> Get(int id)
    {
        var r = await service.ObterAsync(id);
        return r.IsSuccess ? Ok(r.Value) : NotFound(new { erro = r.Error });
    }

    [HttpPost]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Cadastrar usuário",
        Description = "Cadastra um novo usuário sem exigir autenticação JWT. O perfil padrão é cidadao e pode ser alterado para admin no corpo da requisição. A senha é armazenada somente após aplicação de hash.")]
    [SwaggerResponse(StatusCodes.Status201Created, "Usuário cadastrado com sucesso", typeof(UsuarioDetalheResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados de cadastro inválidos")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "E-mail já cadastrado")]
    public async Task<IActionResult> Post(UsuarioCreateRequestDto dto)
    {
        var r = await service.CriarAsync(dto);
        return r.IsSuccess
            ? StatusCode(r.StatusCode, r.Value)
            : StatusCode(r.StatusCode, new { erro = r.Error });
    }

    [HttpPut("{id:int}")]
    [SwaggerOperation(
        Summary = "Atualizar usuário",
        Description = "Atualiza nome, e-mail, perfil e status do usuário. A senha é opcional; quando enviada, um novo hash é gerado. Operação restrita ao perfil admin.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Usuário atualizado com sucesso", typeof(UsuarioDetalheResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados de atualização inválidos")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token JWT ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Apenas administradores podem atualizar usuários")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Usuário não encontrado")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "E-mail já utilizado por outro usuário")]
    public async Task<IActionResult> Put(int id, UsuarioUpdateRequestDto dto)
    {
        var r = await service.AtualizarAsync(id, dto);
        return r.IsSuccess
            ? Ok(r.Value)
            : StatusCode(r.StatusCode, new { erro = r.Error });
    }

    [HttpDelete("{id:int}")]
    [SwaggerOperation(
        Summary = "Desativar usuário",
        Description = "Realiza exclusão lógica do usuário, definindo Ativo=false. O histórico relacionado ao usuário é preservado. Operação restrita ao perfil admin.")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Usuário desativado com sucesso")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token JWT ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Apenas administradores podem desativar usuários")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Usuário não encontrado")]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await service.ExcluirAsync(id);
        return r.IsSuccess ? NoContent() : NotFound(new { erro = r.Error });
    }
}
