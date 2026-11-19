using Moq;
using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Application.Services;
using RotaLeve.Api.Domain.Entities;
using RotaLeve.Api.Domain.Enums;
using RotaLeve.Api.Domain.Interfaces;

namespace RotaLeve.Tests.Unit.Application;

public class RelatoServiceTests
{
    private readonly Mock<IRelatoRepository> _relatos = new();
    private readonly Mock<IConfirmacaoRepository> _confirmacoes = new();
    private readonly Mock<ILocalidadeRepository> _pontos = new();

    [Fact]
    [Trait("Application", "Relatos")]
    public async Task Criar_ComPontoInexistente_DeveRetornar400()
    {
        // Arrange
        _pontos.Setup(x => x.ObterAsync(99)).ReturnsAsync((Localidade?)null);
        // Act
        var result = await CriarService().CriarAsync(1, DtoValido(99), "unit-erro-localidade");
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }



    [Fact]
    [Trait("Application", "Relatos")]
    public async Task Criar_DadosValidos_DeveCriarComoNovoERetornar201()
    {
        // Arrange
        _pontos.Setup(x => x.ObterAsync(2)).ReturnsAsync(new Localidade { IdLocalidade = 2, Ativo = true });
        _relatos.Setup(x => x.AdicionarIdempotenteAsync(It.IsAny<Relato>()))
            .ReturnsAsync((Relato r) => { r.IdRelato = 15; return (r, true); });
        var dto = DtoValido(2);
        dto.Descricao = "  Elevador principal indisponivel  ";

        // Act
        var result = await CriarService().CriarAsync(8, dto, "unit-criacao");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal(StatusRelato.Novo, result.Value!.Status);
        Assert.Equal("Elevador principal indisponivel", result.Value.Descricao);
        _relatos.Verify(x => x.AdicionarIdempotenteAsync(It.Is<Relato>(r => r.IdUsuario == 8 && r.Status == StatusRelato.Novo && r.ChaveIdempotencia != null)), Times.Once);
    }

    [Fact]
    [Trait("Application", "Relatos")]
    public async Task Atualizar_RelatoDeOutroCidadao_DeveRetornar403()
    {
        // Arrange
        _relatos.Setup(x => x.ObterAsync(10, true)).ReturnsAsync(new Relato { IdRelato = 10, IdUsuario = 1, Status = StatusRelato.Novo });
        // Act
        var result = await CriarService().AtualizarAsync(10, 2, false, DtoValido());
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    [Trait("Application", "Relatos")]
    public async Task Atualizar_RelatoAtivoComoCidadao_DeveRetornar409()
    {
        // Arrange
        _relatos.Setup(x => x.ObterAsync(10, true)).ReturnsAsync(new Relato { IdRelato = 10, IdUsuario = 2, Status = StatusRelato.Ativo });
        // Act
        var result = await CriarService().AtualizarAsync(10, 2, false, DtoValido());
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.StatusCode);
    }



    [Fact]
    [Trait("Application", "Relatos")]
    public async Task Excluir_RelatoDeOutroCidadao_DeveRetornar403()
    {
        // Arrange
        _relatos.Setup(x => x.ObterAsync(10, false)).ReturnsAsync(new Relato { IdRelato = 10, IdUsuario = 1, Status = StatusRelato.Novo });
        // Act
        var result = await CriarService().ExcluirAsync(10, 2, false);
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
    }





    [Fact]
    [Trait("Application", "Relatos")]
    public async Task Confirmar_ProprioRelato_DeveRetornar409()
    {
        // Arrange
        _relatos.Setup(x => x.ObterAsync(10, true)).ReturnsAsync(new Relato { IdRelato = 10, IdUsuario = 7, Status = StatusRelato.Novo });
        // Act
        var result = await CriarService().ConfirmarAsync(10, 7);
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.StatusCode);
    }



    [Fact]
    [Trait("Application", "Relatos")]
    public async Task Confirmar_DuasVezes_DeveRetornar409()
    {
        // Arrange
        _relatos.Setup(x => x.ObterAsync(10, true)).ReturnsAsync(new Relato { IdRelato = 10, IdUsuario = 1, Status = StatusRelato.Ativo });
        _confirmacoes.Setup(x => x.ExisteAsync(10, 7)).ReturnsAsync(true);
        // Act
        var result = await CriarService().ConfirmarAsync(10, 7);
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    [Trait("Application", "Relatos")]
    public async Task Confirmar_Valido_DeveAtivarRelatoEAtualizarData()
    {
        // Arrange
        var relato = new Relato { IdRelato = 10, IdUsuario = 1, Status = StatusRelato.Novo };
        _relatos.Setup(x => x.ObterAsync(10, true)).ReturnsAsync(relato);
        _confirmacoes.Setup(x => x.ExisteAsync(10, 7)).ReturnsAsync(false);
        _confirmacoes.Setup(x => x.AdicionarAsync(It.IsAny<ConfirmacaoRelato>()))
            .ReturnsAsync((ConfirmacaoRelato c) => { c.IdConfirmacao = 20; return c; });
        _relatos.Setup(x => x.AtualizarAsync(relato)).ReturnsAsync(relato);

        // Act
        var result = await CriarService().ConfirmarAsync(10, 7);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal(StatusRelato.Ativo, relato.Status);
        Assert.NotNull(relato.DataUltimaConfirmacao);
        Assert.Equal(7, result.Value!.UsuarioId);
    }





    [Fact]
    [Trait("Application", "Relatos")]
    public async Task AlterarStatus_ParaResolvido_DevePreencherDataResolucao()
    {
        // Arrange
        var relato = new Relato { IdRelato = 10, IdUsuario = 1, Status = StatusRelato.Ativo };
        _relatos.Setup(x => x.ObterAsync(10, true)).ReturnsAsync(relato);
        _relatos.Setup(x => x.AtualizarAsync(relato)).ReturnsAsync(relato);

        // Act
        var result = await CriarService().AlterarStatusAsync(10, StatusRelato.Resolvido);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(StatusRelato.Resolvido, relato.Status);
        Assert.NotNull(relato.DataResolucao);
    }



    [Fact]
    [Trait("Application", "Relatos")]
    public async Task Listar_DeveExecutarExpiracaoAntesDaConsulta()
    {
        // Arrange
        _relatos.Setup(x => x.ExpirarAntigosAsync(It.IsAny<DateTime>())).ReturnsAsync(2);
        _relatos.Setup(x => x.ListarAsync()).ReturnsAsync([]);

        // Act
        await CriarService().ListarAsync();

        // Assert
        _relatos.Verify(x => x.ExpirarAntigosAsync(It.Is<DateTime>(d => d < DateTime.UtcNow.AddHours(-71))), Times.Once);
        _relatos.Verify(x => x.ListarAsync(), Times.Once);
    }

    [Fact]
    [Trait("Application", "Idempotencia")]
    public async Task Criar_Repeticao_DeveReutilizarRelatoEOcorrendoMudancaRetornar409()
    {
        // Arrange
        var request = DtoValido(2);
        var chave = "solicitacao-123";
        var hashChave = IdempotenciaRelato.GerarHashChave(8, chave);
        var existente = new Relato
        {
            IdRelato = 37, IdUsuario = 8, IdLocalidade = 2,
            Tipo = request.Tipo, Descricao = request.Descricao,
            Latitude = request.Latitude, Longitude = request.Longitude,
            ChaveIdempotencia = hashChave,
            HashRequisicao = IdempotenciaRelato.GerarHashRequisicao(request)
        };
        _relatos.Setup(x => x.ObterPorChaveIdempotenciaAsync(hashChave))
            .ReturnsAsync(existente);

        // Act
        var repetido = await CriarService().CriarAsync(8, request, chave);
        var alterado = DtoValido(2);
        alterado.Descricao = "Descricao diferente para mesma chave";
        var conflito = await CriarService().CriarAsync(8, alterado, chave);

        // Assert
        Assert.True(repetido.IsSuccess);
        Assert.Equal(200, repetido.StatusCode);
        Assert.Equal(37, repetido.Value!.Id);
        Assert.False(conflito.IsSuccess);
        Assert.Equal(409, conflito.StatusCode);
        _relatos.Verify(x => x.AdicionarIdempotenteAsync(It.IsAny<Relato>()), Times.Never);
    }

    private RelatoService CriarService() => new(_relatos.Object, _confirmacoes.Object, _pontos.Object);

    private static RelatoRequestDto DtoValido(int pontoId = 1) => new()
    {
        LocalidadeId = pontoId,
        Tipo = TipoRelato.ElevadorIndisponivel,
        Descricao = "Elevador indisponivel para acesso",
        Latitude = -23.5505m,
        Longitude = -46.6333m
    };
}
