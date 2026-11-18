using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using RotaLeve.Api.Application.Interfaces;
using RotaLeve.Tests.Integration.Configuration;

namespace RotaLeve.Tests.Integration.Fixtures;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IAuthService> AuthServiceMock { get; } = new();
    public Mock<ILocalidadeService> LocalidadeServiceMock { get; } = new();
    public Mock<IRelatoService> RelatoServiceMock { get; } = new();
    public Mock<IUsuarioService> UsuarioServiceMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Secretkey"] = JwtTestConfiguration.GetSecretKey()
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(IAuthService));
            services.RemoveAll(typeof(ILocalidadeService));
            services.RemoveAll(typeof(IRelatoService));
            services.RemoveAll(typeof(IUsuarioService));

            services.AddSingleton(AuthServiceMock.Object);
            services.AddSingleton(LocalidadeServiceMock.Object);
            services.AddSingleton(RelatoServiceMock.Object);
            services.AddSingleton(UsuarioServiceMock.Object);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                options.DefaultChallengeScheme = TestAuthHandler.Scheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });
        });
    }
}

[CollectionDefinition("API Integration")]
public class ApiIntegrationCollection : ICollectionFixture<CustomWebApplicationFactory>
{
}
