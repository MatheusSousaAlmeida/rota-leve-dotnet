using RotaLeve.Api.Infrastructure.Security;

namespace RotaLeve.Tests.Unit.Security;

public class PasswordHasherTests
{
    [Fact]
    [Trait("Security", "Password")]
    public void HashEVerify_SenhaCorreta_DeveValidarComSucesso()
    {
        // Arrange
        var hasher = new PasswordHasher();
        const string senha = "Senha@123";

        // Act
        var hash = hasher.Hash(senha);
        var senhaCorreta = hasher.Verify(senha, hash);
        var senhaIncorreta = hasher.Verify("outra", hash);

        // Assert
        Assert.NotEqual(senha, hash);
        Assert.True(senhaCorreta);
        Assert.False(senhaIncorreta);
    }

    [Fact]
    [Trait("Security", "Password")]
    public void Hash_MesmaSenhaDuasVezes_DeveGerarHashesDiferentes()
    {
        // Arrange
        var hasher = new PasswordHasher();
        const string senha = "Senha@123";

        // Act
        var primeiro = hasher.Hash(senha);
        var segundo = hasher.Hash(senha);

        // Assert
        Assert.NotEqual(primeiro, segundo);
        Assert.True(hasher.Verify(senha, primeiro));
        Assert.True(hasher.Verify(senha, segundo));
    }


}
