# Rota Leve API (.NET 8 Minimal API) - build multi-stage
# Contexto de build: raiz do repositorio (docker build -f dockerfiles/Dockerfile .)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY RotaLeve.Api/RotaLeve.Api.csproj RotaLeve.Api/
RUN dotnet restore RotaLeve.Api/RotaLeve.Api.csproj
COPY RotaLeve.Api/ RotaLeve.Api/
RUN dotnet publish RotaLeve.Api/RotaLeve.Api.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
# usuario sem privilegio de root (imagem oficial ja traz o usuario app)
USER app
ENTRYPOINT ["dotnet", "RotaLeve.Api.dll"]
