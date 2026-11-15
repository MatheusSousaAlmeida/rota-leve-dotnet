# Rota Leve API (.NET)

Backend colaborativo do projeto **Rota Leve**, Global Solution CitizenTech 2026/2. A API permite que cidadãos registrem e acompanhem barreiras de acessibilidade urbana, enquanto administradores gerenciam usuários, pontos críticos e a moderação dos relatos.


## Escopo desta API
- autenticação JWT;
- papéis `cidadao` e `admin` (admin também pode executar as ações de cidadão);
- CRUD administrativo de usuários;
- CRUD de relatos colaborativos;
- cadastro e gestão de pontos críticos;
- confirmação comunitária de relatos, com bloqueio de confirmação duplicada por usuário;
- expiração lógica após 72 horas sem confirmação;
- rate limiting contra spam;
- Swagger/OpenAPI;
- Health Checks da API e Oracle;
- testes unitários e de integração com xUnit.

> **Idempotência HTTP:** não foi implementada nesta primeira versão, pois será alinhada com o professor. A regra de negócio de confirmação única é garantida separadamente por validação e por `UNIQUE (ID_RELATO, ID_USUARIO)` no Oracle.

## Tecnologias
ASP.NET Core 8, Controllers REST, Entity Framework Core 8, Oracle, JWT, ASP.NET Core Rate Limiting, Swagger e xUnit.

## Regras principais
1. O cadastro em `POST /api/usuarios` é público e não exige JWT; o perfil padrão é `cidadao`, podendo ser enviado como `admin`.
2. Admin pode executar tudo que o cidadão executa e também gerenciar pontos críticos/status.
3. O autor não pode confirmar o próprio relato.
4. Cada usuário pode confirmar um relato apenas uma vez.
5. A primeira confirmação altera o relato de `Novo` para `Ativo`.
6. Relato `Resolvido` ou `Expirado` não aceita confirmação.
7. Relato resolvido não é reaberto; se o problema voltar, deve ser criado outro relato.
8. Relatos `Novo`/`Ativo` expiram após 72h sem nova confirmação.
9. Cidadão só altera/exclui o próprio relato enquanto ele estiver `Novo`; admin pode administrar qualquer relato.

## Rate limiting
- `POST /api/relatos`: 5 requisições a cada 10 minutos por usuário autenticado (fallback por IP).
- `POST /api/relatos/{id}/confirmacoes`: 20 requisições por minuto por usuário autenticado (fallback por IP).
- excesso retorna HTTP `429 Too Many Requests`.

## Executar
1. Preencha `User Id` e `Password` em `ConnectionStrings:OracleDbConnection`. A configuração JWT segue o padrão do projeto ClyvoVet e utiliza a chave `Secretkey`.
2. Execute a aplicação. As migrations são aplicadas automaticamente no startup e criam o schema quando necessário.
3. Execute:
   ```bash
   dotnet restore
   dotnet run --project RotaLeve.Api
   ```
4. Abra `/swagger`.

## Usuários e autenticação
O `AuthController` é responsável exclusivamente pela autenticação e emissão do JWT por meio de `POST /api/auth/login`.

A criação e manutenção de usuários fica concentrada no `UsuariosController`. O `POST /api/usuarios` é público e nunca exige JWT. O campo `perfil` pode ser `cidadao` ou `admin`, com `cidadao` como valor padrão. As operações de consulta, atualização e desativação continuam restritas ao perfil `admin`.

```json
{
  "nome": "Admin Demo",
  "email": "admin@rotaleve.com",
  "senha": "Senha@123",
  "perfil": "admin"
}
```

Após a criação, o usuário utiliza `/api/auth/login` para obter o JWT contendo `role=cidadao` ou `role=admin`, conforme o perfil cadastrado.

Não é necessário inserir usuários manualmente no banco. O endpoint público `POST /api/usuarios` permite criar tanto cidadãos quanto administradores; depois, o login retorna o JWT usado nos endpoints protegidos.

## Configuração
Os arquivos `appsettings.json` e `appsettings.Development.json` seguem o padrão do projeto ClyvoVet:
- `ConnectionStrings:OracleDbConnection` para o Oracle FIAP;
- `Secretkey` para assinatura JWT;
- `Logging` com os níveis padrão da aplicação.

Por segurança operacional, `User Id` e `Password` permanecem vazios no repositório e devem ser preenchidos localmente.

## Testes automatizados
Os testes seguem o mesmo padrão utilizado no projeto ClyvoVet:
- xUnit + Moq;
- organização explícita no padrão **AAA (Arrange, Act, Assert)**;
- uso de `[Trait]` para classificação;
- testes unitários com mocks dos repositórios/serviços dependentes;
- testes de integração com `WebApplicationFactory<Program>`, mocks dos serviços da camada Application e `TestAuthHandler`, sem criar um segundo `DbContext` InMemory.

A suíte cobre autenticação JWT, normalização de e-mail no login, hashing de senha no CRUD de usuários, geração de JWT, CRUD administrativo de usuários, regras de pontos críticos, criação/alteração/exclusão de relatos, confirmações, transições de status, expiração, validações HTTP, autorização cidadão/admin, Swagger, Health Check e respostas `429` dos rate limits.

Execute com:
```bash
dotnet test RotaLeve.sln
```

### Se estiver atualizando a partir da versão anterior
Antes de restaurar os pacotes, remova `bin` e `obj` dos três projetos para eliminar assemblies antigos do EF Core. Depois execute:
```bash
dotnet clean RotaLeve.sln
dotnet restore RotaLeve.sln
dotnet test RotaLeve.sln
```

Se o Visual Studio ainda mantiver pacotes antigos em cache, feche a IDE e execute também:
```bash
dotnet nuget locals all --clear
dotnet restore RotaLeve.sln
```

## Health Checks
- `GET /health/live`: processo da API.
- `GET /health/ready`: API + Oracle.

## Endpoints
### Auth
- `POST /api/auth/login`

### Usuários
- `GET /api/usuarios` (admin)
- `GET /api/usuarios/{id}` (admin)
- `POST /api/usuarios` (público, sem JWT)
- `PUT /api/usuarios/{id}` (admin)
- `DELETE /api/usuarios/{id}` (admin, exclusão lógica)

### Pontos críticos
- `GET /api/pontos-criticos`
- `GET /api/pontos-criticos/{id}`
- `POST /api/pontos-criticos` (admin)
- `PUT /api/pontos-criticos/{id}` (admin)
- `DELETE /api/pontos-criticos/{id}` (admin)

### Relatos
- `GET /api/relatos`
- `GET /api/relatos/{id}`
- `POST /api/relatos`
- `PUT /api/relatos/{id}`
- `DELETE /api/relatos/{id}`
- `POST /api/relatos/{id}/confirmacoes`
- `DELETE /api/relatos/{id}/confirmacoes/minha`
- `PATCH /api/relatos/{id}/status` (admin)

## Uso de IA generativa
IA generativa foi utilizada como apoio na estruturação, revisão e desenvolvimento do código. O grupo deve revisar, compreender e ser capaz de explicar todos os componentes apresentados.

## Logs estruturados

A API utiliza **Serilog** no mesmo padrão do projeto ClyvoVet. Na inicialização, é criada automaticamente a pasta `logs` no diretório de execução da aplicação.

Os arquivos seguem o padrão:

```text
logs/api-YYYYMMDD.log
```

Características:

- rotação diária (`RollingInterval.Day`);
- retenção dos últimos 7 arquivos de log;
- saída simultânea no console e em arquivo;
- `CorrelationId` por requisição, aceitando o header `X-Correlation-ID` ou gerando um automaticamente;
- `TraceId` associado à requisição;
- requisições HTTP 2xx/3xx registradas como `Information`;
- respostas 4xx registradas como `Warning`;
- respostas 5xx e exceções registradas como `Error`;
- exceções tratadas pelo `GlobalExceptionHandler` também são persistidas no arquivo de log.

Exemplo de arquivo gerado:

```text
RotaLeve.Api/bin/Debug/net8.0/logs/api-20261112.log
```

## Migrations automáticas

O projeto aplica automaticamente as migrations do Entity Framework Core durante a inicialização da API, seguindo o mesmo padrão do projeto ClyvoVet.

- `Database.Migrate()` é executado no startup.
- O processo é ignorado quando o ambiente é `Testing`, evitando acesso ao Oracle durante os testes de integração.
- Em caso de indisponibilidade temporária do Oracle, a aplicação realiza até **6 tentativas**, com intervalo de **20 segundos**.
- O resultado das tentativas é registrado pelo Serilog.
- A migration inicial está em `RotaLeve.Api/Infrastructure/Data/Migrations/20261113000100_InitialCreate.cs`.

Em um banco vazio, basta configurar `OracleDbConnection` e iniciar a API; o schema será criado automaticamente. Para futuras alterações no modelo, crie uma nova migration com `dotnet ef migrations add <NomeDaMigration>` e ela será aplicada automaticamente na próxima inicialização.


## Autenticação JWT no Swagger e publicação DevOps (v13)

**Somente** `POST /api/usuarios` e `POST /api/auth/login` são públicos. Todos os demais endpoints, inclusive `/health/live` e `/health/ready`, exigem um JWT válido. As funções administrativas exigem a role `admin`. O cadastro público aceita `admin` apenas para fins demonstrativos; **não é uma configuração segura para produção real**.

1. Configure `ConnectionStrings__OracleDbConnection` e `Secretkey` por variáveis de ambiente no destino (ou arquivos appsettings locais). Não publique credenciais reais.
2. Execute a API com o Oracle acessível. No startup o EF Core aplica migrations; se todas as tentativas falharem, a aplicação encerra com erro em vez de prosseguir sem tabelas. Use um schema vazio para a migration inicial.
3. No Swagger, execute `POST /api/usuarios` para criar uma conta `admin`, depois `POST /api/auth/login`.
4. Copie **somente** o valor `accessToken` retornado. Clique em **Authorize**, cole o JWT **sem** escrever `Bearer ` na frente e confirme. Swagger utiliza o esquema HTTP bearer e adiciona o prefixo sozinho.
5. Execute `GET /api/usuarios` ou `GET /health/live` com o token. A ausência de token retorna **401**; token de `cidadao` em rota exclusiva de admin retorna **403**.
6. Para testar fora do Swagger, envie o cabeçalho `Authorization: Bearer <accessToken>`.

**Observação de infraestrutura:** como os Health Checks requerem JWT nesta entrega, um serviço de monitoramento/orquestrador deve enviar um token válido para essas URLs.

**Logs de diagnóstico:** erros de assinatura, expiração ou formato do JWT são registrados sem expor o token, na pasta diária `logs`.

### Validação pré-entrega

```powershell
dotnet restore RotaLeve.sln
dotnet build RotaLeve.sln --no-restore
dotnet test RotaLeve.sln --no-build
```

O SDK .NET não estava disponível no ambiente de edição; executar esses comandos no Visual Studio / ambiente DevOps antes de entregar.

### Smoke test para DevOps (PowerShell 7)

Depois de configurar o Oracle e iniciar a API, execute:

```powershell
pwsh -File ./scripts/smoke-jwt.ps1 -BaseUrl http://localhost:5139
```

O script cria um administrador de teste (e-mail aleatório), realiza login, verifica 401 sem token, testa acesso autorizado e consulta os dois Health Checks, incluindo Oracle. Para a aplicação publicada, informe a URL base HTTPS no argumento `-BaseUrl`. O script não exibe o JWT nem a senha nos logs.
