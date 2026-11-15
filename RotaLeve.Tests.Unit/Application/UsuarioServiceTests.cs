using Moq;
using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Application.Services;
using RotaLeve.Api.Domain.Entities;
using RotaLeve.Api.Domain.Enums;
using RotaLeve.Api.Domain.Interfaces;
using RotaLeve.Api.Infrastructure.Security;

namespace RotaLeve.Tests.Unit.Application;

public class UsuarioServiceTests
{
    private readonly Mock<IUsuarioRepository> _repository = new();
    private readonly Mock<IPasswordHasher> _hasher = new();

    [Fact]
    [Trait("Service", "Usuario")]
    public async Task Listar_UsuariosExistentes_DeveRetornarTodos()
    {
        // Arrange
        _repository.Setup(x => x.ListarAsync()).ReturnsAsync([
            CriarUsuario(1, "Cidadao", "cidadao@teste.com", TipoUsuario.Cidadao),
            CriarUsuario(2, "Admin", "admin@teste.com", TipoUsuario.Admin)
        ]);
        var service = CriarService();

        // Act
        var resultado = (await service.ListarAsync()).ToList();

        // Assert
        Assert.Equal(2, resultado.Count);
        Assert.Equal("cidadao", resultado[0].Perfil);
        Assert.Equal("admin", resultado[1].Perfil);
    }

    [Fact]
    [Trait("Service", "Usuario")]
    public async Task Obter_UsuarioInexistente_DeveRetornarNotFound()
    {
        // Arrange
        _repository.Setup(x => x.ObterPorIdAsync(999)).ReturnsAsync((Usuario?)null);
        var service = CriarService();

        // Act
        var resultado = await service.ObterAsync(999);

        // Assert
        Assert.False(resultado.IsSuccess);
        Assert.Equal(404, resultado.StatusCode);
    }

    [Fact]
    [Trait("Service", "Usuario")]
    public async Task Criar_EmailDisponivel_DeveCriarCidadaoComSenhaHash()
    {
        // Arrange
        var dto = new UsuarioCreateRequestDto
        {
            Nome = " Maria Silva ",
            Email = " MARIA@EXEMPLO.COM ",
            Senha = "Senha@123",
            Perfil = "cidadao"
        };
        _repository.Setup(x => x.ObterPorEmailAsync("maria@exemplo.com")).ReturnsAsync((Usuario?)null);
        _hasher.Setup(x => x.Hash("Senha@123")).Returns("HASH-GERADO");
        _repository.Setup(x => x.AdicionarAsync(It.IsAny<Usuario>()))
            .ReturnsAsync((Usuario u) => { u.IdUsuario = 10; return u; });
        var service = CriarService();

        // Act
        var resultado = await service.CriarAsync(dto);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal(201, resultado.StatusCode);
        Assert.Equal("maria@exemplo.com", resultado.Value!.Email);
        Assert.Equal("cidadao", resultado.Value.Perfil);
        _repository.Verify(x => x.AdicionarAsync(It.Is<Usuario>(u =>
            u.Nome == "Maria Silva" &&
            u.Email == "maria@exemplo.com" &&
            u.SenhaHash == "HASH-GERADO" &&
            u.TipoUsuario == TipoUsuario.Cidadao)), Times.Once);
    }

    [Fact]
    [Trait("Service", "Usuario")]
    public async Task Criar_PerfilAdmin_DevePersistirAdmin()
    {
        // Arrange
        var dto = new UsuarioCreateRequestDto
        {
            Nome = "Admin Demo",
            Email = "admin@exemplo.com",
            Senha = "Senha@123",
            Perfil = "admin"
        };
        _repository.Setup(x => x.ObterPorEmailAsync(dto.Email)).ReturnsAsync((Usuario?)null);
        _hasher.Setup(x => x.Hash(dto.Senha)).Returns("HASH");
        _repository.Setup(x => x.AdicionarAsync(It.IsAny<Usuario>()))
            .ReturnsAsync((Usuario u) => { u.IdUsuario = 20; return u; });
        var service = CriarService();

        // Act
        var resultado = await service.CriarAsync(dto);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal("admin", resultado.Value!.Perfil);
        _repository.Verify(x => x.AdicionarAsync(It.Is<Usuario>(u => u.TipoUsuario == TipoUsuario.Admin)), Times.Once);
    }

    [Fact]
    [Trait("Service", "Usuario")]
    public async Task Criar_PerfilPadrao_DevePersistirCidadao()
    {
        // Arrange
        var dto = new UsuarioCreateRequestDto
        {
            Nome = "Usuario Padrao",
            Email = "padrao@exemplo.com",
            Senha = "Senha@123"
        };
        _repository.Setup(x => x.ObterPorEmailAsync(dto.Email)).ReturnsAsync((Usuario?)null);
        _hasher.Setup(x => x.Hash(dto.Senha)).Returns("HASH");
        _repository.Setup(x => x.AdicionarAsync(It.IsAny<Usuario>()))
            .ReturnsAsync((Usuario u) => { u.IdUsuario = 1; return u; });
        var service = CriarService();

        // Act
        var resultado = await service.CriarAsync(dto);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal("cidadao", resultado.Value!.Perfil);
        _repository.Verify(x => x.AdicionarAsync(It.Is<Usuario>(u => u.TipoUsuario == TipoUsuario.Cidadao)), Times.Once);
    }

    [Fact]
    [Trait("Service", "Usuario")]
    public async Task Criar_EmailDuplicado_DeveRetornarConflict()
    {
        // Arrange
        var dto = new UsuarioCreateRequestDto
        {
            Nome = "Maria",
            Email = "maria@exemplo.com",
            Senha = "Senha@123"
        };
        _repository.Setup(x => x.ObterPorEmailAsync(dto.Email)).ReturnsAsync(CriarUsuario(1, "Maria", dto.Email, TipoUsuario.Cidadao));
        var service = CriarService();

        // Act
        var resultado = await service.CriarAsync(dto);

        // Assert
        Assert.False(resultado.IsSuccess);
        Assert.Equal(409, resultado.StatusCode);
        _repository.Verify(x => x.AdicionarAsync(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    [Trait("Service", "Usuario")]
    public async Task Atualizar_UsuarioExistente_DeveAlterarDadosPerfilStatusESenha()
    {
        // Arrange
        var usuario = CriarUsuario(1, "Nome Antigo", "antigo@exemplo.com", TipoUsuario.Cidadao);
        var dto = new UsuarioUpdateRequestDto
        {
            Nome = "Nome Novo",
            Email = "novo@exemplo.com",
            Senha = "NovaSenha@123",
            Perfil = "admin",
            Ativo = false
        };
        _repository.Setup(x => x.ObterPorIdAsync(1)).ReturnsAsync(usuario);
        _repository.Setup(x => x.ObterPorEmailAsync("novo@exemplo.com")).ReturnsAsync((Usuario?)null);
        _hasher.Setup(x => x.Hash(dto.Senha)).Returns("NOVO-HASH");
        var service = CriarService();

        // Act
        var resultado = await service.AtualizarAsync(1, dto);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal("Nome Novo", usuario.Nome);
        Assert.Equal("novo@exemplo.com", usuario.Email);
        Assert.Equal(TipoUsuario.Admin, usuario.TipoUsuario);
        Assert.False(usuario.Ativo);
        Assert.Equal("NOVO-HASH", usuario.SenhaHash);
        _repository.Verify(x => x.AtualizarAsync(usuario), Times.Once);
    }

    [Fact]
    [Trait("Service", "Usuario")]
    public async Task Atualizar_SemNovaSenha_DeveManterHashAtual()
    {
        // Arrange
        var usuario = CriarUsuario(1, "Maria", "maria@exemplo.com", TipoUsuario.Cidadao);
        usuario.SenhaHash = "HASH-ATUAL";
        var dto = new UsuarioUpdateRequestDto
        {
            Nome = "Maria Atualizada",
            Email = "maria@exemplo.com",
            Senha = null,
            Perfil = "cidadao",
            Ativo = true
        };
        _repository.Setup(x => x.ObterPorIdAsync(1)).ReturnsAsync(usuario);
        _repository.Setup(x => x.ObterPorEmailAsync(dto.Email)).ReturnsAsync(usuario);
        var service = CriarService();

        // Act
        var resultado = await service.AtualizarAsync(1, dto);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal("HASH-ATUAL", usuario.SenhaHash);
        _hasher.Verify(x => x.Hash(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    [Trait("Service", "Usuario")]
    public async Task Atualizar_EmailDeOutroUsuario_DeveRetornarConflict()
    {
        // Arrange
        var usuario = CriarUsuario(1, "Maria", "maria@exemplo.com", TipoUsuario.Cidadao);
        var outro = CriarUsuario(2, "Joao", "joao@exemplo.com", TipoUsuario.Cidadao);
        var dto = new UsuarioUpdateRequestDto
        {
            Nome = "Maria",
            Email = "joao@exemplo.com",
            Perfil = "cidadao",
            Ativo = true
        };
        _repository.Setup(x => x.ObterPorIdAsync(1)).ReturnsAsync(usuario);
        _repository.Setup(x => x.ObterPorEmailAsync(dto.Email)).ReturnsAsync(outro);
        var service = CriarService();

        // Act
        var resultado = await service.AtualizarAsync(1, dto);

        // Assert
        Assert.False(resultado.IsSuccess);
        Assert.Equal(409, resultado.StatusCode);
        _repository.Verify(x => x.AtualizarAsync(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    [Trait("Service", "Usuario")]
    public async Task Excluir_UsuarioExistente_DeveRealizarExclusaoLogica()
    {
        // Arrange
        var usuario = CriarUsuario(1, "Maria", "maria@exemplo.com", TipoUsuario.Cidadao);
        _repository.Setup(x => x.ObterPorIdAsync(1)).ReturnsAsync(usuario);
        var service = CriarService();

        // Act
        var resultado = await service.ExcluirAsync(1);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal(204, resultado.StatusCode);
        Assert.False(usuario.Ativo);
        _repository.Verify(x => x.AtualizarAsync(usuario), Times.Once);
    }

    private UsuarioService CriarService() => new(_repository.Object, _hasher.Object);

    private static Usuario CriarUsuario(int id, string nome, string email, TipoUsuario tipo) => new()
    {
        IdUsuario = id,
        Nome = nome,
        Email = email,
        SenhaHash = "HASH",
        TipoUsuario = tipo,
        Ativo = true,
        DataCriacao = DateTime.UtcNow
    };
}
