using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Application.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace RotaLeve.Api.Presentation.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService service) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "Autenticar usuário e emitir JWT",
        Description = "Valida e-mail e senha do usuário e, em caso de sucesso, retorna um token JWT com o perfil de acesso do usuário autenticado.")]
    [SwaggerResponse(StatusCodes.Status200OK, "JWT emitido com sucesso", typeof(AuthResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados de autenticação inválidos")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "E-mail ou senha inválidos")]
    public async Task<IActionResult> Login(LoginRequestDto dto)
    {
        var r = await service.LoginAsync(dto);
        return r.IsSuccess
            ? Ok(r.Value)
            : StatusCode(r.StatusCode, new { erro = r.Error });
    }
}
