# Rota Leve API (.NET)

Backend colaborativo do projeto **Rota Leve**, Global Solution CitizenTech 2026/2. A API permite que cidadãos registrem e acompanhem barreiras de acessibilidade urbana, enquanto administradores gerenciam usuários, localidades e a moderação dos relatos.


## Escopo desta API
- autenticação JWT;
- papéis `cidadao` e `admin` (admin também pode executar as ações de cidadão);
- CRUD administrativo de usuários;
- CRUD de relatos colaborativos;
- cadastro e gestão de localidades;
- confirmação comunitária de relatos, com bloqueio de confirmação duplicada por usuário;
- expiração lógica após 72 horas sem confirmação;
- rate limiting contra spam;
- Swagger/OpenAPI com parâmetros de paginação interativos;
- Health Checks da API e Oracle;
- testes unitários e de integração com xUnit.

> **Idempotência HTTP:** implementada no `POST /api/relatos` com `Idempotency-Key` e unicidade no Oracle. A regra de confirmação única permanece separada por `UNIQUE (ID_RELATO, ID_USUARIO)`.

## Tecnologias
ASP.NET Core 8, Controllers REST, Entity Framework Core 8, Oracle, JWT, ASP.NET Core Rate Limiting, Swagger e xUnit.

## Regras principais
1. O cadastro em `POST /api/usuarios` é público e não exige JWT; o perfil padrão é `cidadao`, podendo ser enviado como `admin`.
2. Admin pode executar tudo que o cidadão executa e também gerenciar localidades/status.
3. O autor não pode confirmar o próprio relato.
4. Cada usuário pode confirmar um relato apenas uma vez.
5. A primeira confirmação altera o relato de `Novo` para `Ativo`.
6. Relato `Resolvido` ou `Expirado` não aceita confirmação.
7. Relato resolvido não é reaberto; se o problema voltar, deve ser criado outro relato.
8. Relatos `Novo`/`Ativo` expiram após 72h sem nova confirmação.
9. Cidadão só altera/exclui o próprio relato enquanto ele estiver `Novo`; admin pode administrar qualquer relato.

## Paginação nas listagens
Os endpoints `GET /api/usuarios` (admin), `GET /api/localidades` e `GET /api/relatos` aceitam os parâmetros opcionais **`pagina`** (padrão `1`, de `1` a `1000000`) e **`tamanhoPagina`** (padrão `10`, de `1` a `100`). Esses parâmetros aparecem e podem ser preenchidos na documentação Swagger UI.

Exemplo: `GET /api/localidades?pagina=2&tamanhoPagina=5`.

Quando a página contém registros, o retorno HTTP é `200 OK` com os metadados e os registros:

```json
{
  "pagina": 2,
  "tamanhoPagina": 5,
  "totalRegistros": 12,
  "totalPaginas": 3,
  "dados": [{ "idLocalidade": 12, "nome": "Praça Central" }]
}
```

O exemplo acima mostra somente o formato dos campos (a aplicação retorna itens em `dados` quando a resposta é 200). Se não houver registros na página solicitada, a resposta real é **204 No Content**, sem JSON. Parâmetros inválidos recebem **400 Bad Request** com o erro de validação. A paginação é executada no **Oracle**, com `COUNT`, `ORDER BY`, `SKIP` e `TAKE`, e não em listas carregadas inteiramente na memória. A ordenação é estável: usuários por ID ascendente, localidades por nome e ID, relatos por criação e ID descendentes.

A paginação não altera a autenticação JWT, rate limit, exclusões, idempotência nem as demais regras de negócio. Nenhuma migration adicional é necessária.

## Rate limiting
- `GET /api/usuarios`, `GET /api/relatos` e `GET /api/localidades`: **politica `listagens`**, 5 requisições a cada 20 segundos **por endereço IP**, compartilhadas entre as três listagens. A sexta chamada na mesma janela retorna HTTP `429`.
- `POST /api/relatos`: 5 requisições a cada 10 minutos por usuário autenticado (fallback por IP).
- `POST /api/relatos/{id}/confirmacoes`: 20 requisições por minuto por usuário autenticado (fallback por IP).
- excesso retorna HTTP `429 Too Many Requests`.

## Executar
1. Preencha `User Id` e `Password` em `ConnectionStrings:OracleDbConnection` e configure `Secretkey` no `RotaLeve.Api/appsettings.json` (ou por variável de ambiente). Utilize uma chave JWT própria e forte; o arquivo entregue não contém uma chave real.
2. Execute a aplicação. O Swagger inicia mesmo quando o Oracle estiver indisponível. As migrations são executadas automaticamente em segundo plano quando houver conexão.
3. Execute:
   ```bash
   dotnet restore
   dotnet run --project RotaLeve.Api
   ```
4. Abra `/swagger`.

## Usuários e autenticação
O `AuthController` é responsável exclusivamente pela autenticação e emissão do JWT por meio de `POST /api/auth/login`.

A criação e manutenção de usuários fica concentrada no `UsuariosController`. O `POST /api/usuarios` é público e nunca exige JWT. O campo `perfil` pode ser `cidadao` ou `admin`, com `cidadao` como valor padrão. As operações de consulta, atualização e exclusão definitiva continuam restritas ao perfil `admin`.

```json
{
  "nome": "Admin Demo",
  "email": "admin@rotaleve.com",
  "senha": "Senha@123",
  "perfil": "admin"
}
```

Após a criação, o usuário utiliza `/api/auth/login` para obter o JWT contendo `role=cidadao` ou `role=admin`, conforme o perfil cadastrado.

**Swagger Authorize:** o botão *Authorize* apenas armazena o JWT no navegador para enviá-lo no cabeçalho `Authorization: Bearer <token>`. O texto *Authorized* não valida o token. A validação ocorre quando uma rota protegida recebe a requisição: token ausente/inválido retorna `401`; token válido sem permissão retorna `403`. Para testar, faça login e chame `GET /api/relatos`: com lista vazia e JWT válido, a resposta esperada é `204 No Content`.

Não é necessário inserir usuários manualmente no banco. O endpoint público `POST /api/usuarios` permite criar tanto cidadãos quanto administradores; depois, o login retorna o JWT usado nos endpoints protegidos.

## Configuração
Os arquivos `appsettings.json` e `appsettings.Development.json` definem as configurações da aplicação:
- `ConnectionStrings:OracleDbConnection` para o Oracle FIAP;
- `Secretkey` para assinatura JWT;
- `Logging` com os níveis padrão da aplicação.

Por segurança operacional, `User Id`, `Password` e `Secretkey` permanecem vazios no repositório e devem ser preenchidos localmente. Não envie credenciais reais ao controle de versão.

## Testes automatizados
**JWT nos testes:** os dois projetos xUnit copiam automaticamente `RotaLeve.Api/appsettings.json` para seus diretórios de saída durante o build, com o nome `appsettings.RotaLeveApi.json`. A classe compartilhada `TestSupport/JwtTestConfiguration.cs` le a `Secretkey` desse arquivo e permite sobrescreve-la pela variavel de ambiente `Secretkey`. Assim, basta configurar a chave no projeto da API antes de executar `dotnet test`; nenhum valor de JWT fica duplicado nos testes. Se a chave estiver vazia, o teste falha com uma mensagem explicando como configura-la. O teste `Excluir_UsuarioExistente_DeveRemoverDefinitivamente` espera `200 OK`, conforme o endpoint DELETE.

Os testes utilizam:
- xUnit + Moq;
- organização explícita no padrão **AAA (Arrange, Act, Assert)**;
- uso de `[Trait]` para classificação;
- testes unitários com mocks dos repositórios/serviços dependentes;
- testes de integração com `WebApplicationFactory<Program>`, mocks dos serviços da camada Application e `TestAuthHandler`, sem criar um segundo `DbContext` InMemory.

A suíte cobre autenticação JWT, incluindo token inválido e uso do token emitido pelo login, normalização de e-mail no login, hashing de senha no CRUD de usuários, geração de JWT, CRUD administrativo de usuários, regras de localidades, criação/alteração/exclusão de relatos, confirmações, transições de status, expiração, validações HTTP, autorização cidadão/admin, Swagger, Health Check e respostas `429` dos rate limits.

Execute com:
```bash
dotnet test RotaLeve.sln
```

### Limpeza de artefatos de compilação
Em caso de inconsistência de assemblies do EF Core, remova `bin` e `obj` dos três projetos. Depois execute:
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
- `DELETE /api/usuarios/{id}` (admin, exclusão física e permanente)

O `DELETE` de usuário remove fisicamente o registro de `USUARIO`. Para respeitar as chaves estrangeiras Oracle, também exclui as confirmações feitas pelo usuário, seus relatos e as confirmações feitas por outras pessoas nesses relatos. A operação é transacional: em caso de falha, nenhuma exclusão é confirmada. Retorna `200 OK` com mensagem de confirmação quando bem-sucedida ou `404 Not Found` se o usuário não existir. **Essa exclusão é irreversível; não é uma desativação.**

### Localidades
- `GET /api/localidades`
- `GET /api/localidades/{id}`
- `POST /api/localidades` (admin)
- `PUT /api/localidades/{id}` (admin)
- `DELETE /api/localidades/{id}` (admin)

### Relatos
- `GET /api/relatos`
- `GET /api/relatos/{id}`
- `POST /api/relatos`
- `PUT /api/relatos/{id}`
- `DELETE /api/relatos/{id}`
- `POST /api/relatos/{id}/confirmacoes`
- `DELETE /api/relatos/{id}/confirmacoes/minha`

**Padrão DELETE:** as quatro operações de exclusão/remoção retornam `200 OK` com `mensagem` e o identificador correspondente em caso de sucesso; registros inexistentes retornam `404 Not Found`. O código `204 No Content` é utilizado nas listagens sem registros. A exclusão de usuário é permanente; a exclusão de localidade permanece lógica (desativação).
- `PATCH /api/relatos/{id}/status` (admin)

## Uso de IA generativa
IA generativa foi utilizada como apoio na estruturação, revisão e desenvolvimento do código. O grupo deve revisar, compreender e ser capaz de explicar todos os componentes apresentados.

## Logs estruturados

A API utiliza **Serilog**. Na inicialização, é criada automaticamente a pasta `logs` no diretório de execução da aplicação.

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

O projeto aplica automaticamente as migrations do Entity Framework Core em segundo plano, sem bloquear a inicialização HTTP da API.

- `Database.MigrateAsync()` é executado em um `BackgroundService`, após o início do host HTTP.
- O processo é ignorado quando o ambiente é `Testing`, evitando acesso ao Oracle durante os testes de integração.
- Em caso de indisponibilidade temporária do Oracle, a aplicação realiza até **6 tentativas**, com intervalo de **20 segundos**.
- O resultado das tentativas é registrado pelo Serilog.
- A migration inicial está em `RotaLeve.Api/Infrastructure/Data/Migrations/20261113000100_InitialCreate.cs`.

Em um banco vazio, configure `OracleDbConnection` e inicie a API; o schema será criado automaticamente quando o Oracle estiver acessível. Se a configuração estiver incorreta, Swagger e Health Checks continuarão disponíveis, mas as operações de dados poderão falhar até a correção e reinicialização. Para futuras alterações no modelo, crie uma nova migration com `dotnet ef migrations add <NomeDaMigration>` e ela será aplicada automaticamente na próxima inicialização.


## Autenticação JWT no Swagger e publicação DevOps

**Os endpoints públicos** são `POST /api/usuarios`, `POST /api/auth/login`, `GET /health/live` e `GET /health/ready`. Health Checks ficam públicos para diagnosticar a indisponibilidade do Oracle sem precisar de login. Todos os endpoints de negócio restantes exigem um JWT válido. As funções administrativas exigem a role `admin`. O cadastro público aceita `admin` apenas para fins demonstrativos; **não é uma configuração segura para produção real**.

1. Configure `ConnectionStrings__OracleDbConnection` e `Secretkey` por variáveis de ambiente no destino (ou arquivos appsettings locais). Não publique credenciais reais.
2. Inicie a API. O host HTTP sobe mesmo sem Oracle; as migrations são executadas em segundo plano, com até seis tentativas. Se o banco permanecer indisponível, a aplicação continua ativa para diagnóstico e os erros são registrados. Use um schema vazio para a migration inicial.
3. No Swagger, execute `POST /api/usuarios` para criar uma conta `admin`, depois `POST /api/auth/login`.
4. Copie **somente** o valor `accessToken` retornado. Clique em **Authorize**, cole o JWT **sem** escrever `Bearer ` na frente e confirme. Swagger utiliza o esquema HTTP bearer e adiciona o prefixo sozinho.
5. Execute `GET /api/usuarios` com o token. Esse endpoint retorna **401** sem JWT e **403** para um cidadão. Os Health Checks (`/health/live` e `/health/ready`) não exigem token e podem ser consultados mesmo sem acesso ao Oracle.
6. Para testar fora do Swagger, envie o cabeçalho `Authorization: Bearer <accessToken>`.

**Observação de infraestrutura:** os Health Checks não exigem JWT para permitir monitoramento externo. `/health/live` mostra o processo em execução (200); `/health/ready` verifica também a conexão Oracle e retorna 503 quando indisponível.

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


## Retornos das listagens

- `GET /api/usuarios`, `GET /api/relatos` e `GET /api/localidades`: retornam `200 OK` com dados ou `204 No Content` quando vazios.
- Consultas individuais por identificador inexistente retornam `404 Not Found`.
- Os cenários de listagem vazia possuem testes de integração seguindo AAA.

## Consulta de localidades no Oracle

A consulta filtra `ATIVO = 1` usando SQL Oracle explícito para contornar uma incompatibilidade de tradução de predicados booleanos `NUMBER(1)` no provider EF Core. Não é necessário alterar o schema nem as migrations. O filtro está implementado no repositório de localidades.

## Segurança OpenAPI/Swagger

A configuração de autenticação HTTP Bearer fica centralizada no `Program.cs`, usando `AddSecurityDefinition` e `AddSecurityRequirement` globais. Por ser global, o cadeado pode aparecer também no cadastro e no login; isso é apenas a documentação OpenAPI. Os atributos `[AllowAnonymous]` permitem acesso público ao cadastro, login e aos dois Health Checks. Os endpoints de negócio restantes exigem autenticação.

## Validação com Oracle

Execute `dotnet restore`, `dotnet build` e `dotnet test`, e valide também as consultas contra um Oracle real. Os testes de integração HTTP com mocks não executam consultas SQL no provider Oracle; portanto é importante validar `GET /api/localidades` no banco configurado.

## Diagnóstico quando o Oracle está indisponível

- Abra `http://localhost:5139/swagger` normalmente, mesmo sem conexão válida com Oracle.
- `GET /health/live` retorna HTTP 200 enquanto a API estiver em execução.
- `GET /health/ready` retorna HTTP 503 e mostra o check `oracle` como `Unhealthy` se o banco estiver inacessível ou a connection string estiver inválida.
- Migrations continuam automáticas: são tentadas em segundo plano até seis vezes, a cada vinte segundos, e geram logs de erro.
- Endpoints que dependem de dados não ficam automaticamente disponíveis se o banco estiver fora. Corrija as credenciais/conectividade e reinicie a aplicação para permitir novas tentativas de migration.
- Para evitar expor credenciais, o Health Check usa mensagens de erro genéricas; o diagnóstico detalhado fica nos logs do servidor.

## Localidades (nomenclatura)

`LOCALIDADE` representa um local cadastrado, com ou sem relatos. O atributo `ATIVO` indica cadastro habilitado e nao significa que ha um problema ocorrendo. Um `RELATO` referencia `ID_LOCALIDADE` e descreve uma ocorrencia no local.

Endpoints: `GET/POST /api/localidades`, `GET/PUT/DELETE /api/localidades/{id}`. Os contratos de relatos usam `localidadeId`.

### Migracao do Oracle

- **Schema novo:** `InitialCreate` cria diretamente `LOCALIDADE` e `RELATO.ID_LOCALIDADE`; a migration posterior verifica que o schema ja esta correto.
- **Schema existente (migration inicial ja registrada):** a migration de compatibilidade cria `LOCALIDADE` caso `PONTO_CRITICO` tenha sido excluida; ajusta `RELATO.ID_LOCALIDADE`, indice e chave estrangeira. Se a tabela antiga ainda existir, renomeia para preservar seus dados.
- **Relatos orfaos:** se `PONTO_CRITICO` foi excluida e existirem relatos apontando para localidades inexistentes, a nova FK nao pode ser aplicada ate que os dados sejam reconciliados. O projeto nao exclui relatos automaticamente.
- **Schema criado manualmente:** antes de usar migrations, reconciliar o historico `__EFMigrationsHistory` com o schema; nao executar o DDL manual junto com migrations em schema vazio.
- O arquivo `scripts/script-bd.sql` mostra o esquema final apenas como referencia e nao deve ser executado antes de iniciar a API com migrations automáticas.

**Atencao:** o caminho anterior `/api/pontos-criticos` foi substituido por `/api/localidades`; clientes precisam atualizar as URLs.


## Testes automatizados
**JWT nos testes:** os dois projetos xUnit copiam automaticamente `RotaLeve.Api/appsettings.json` para seus diretórios de saída durante o build, com o nome `appsettings.RotaLeveApi.json`. A classe compartilhada `TestSupport/JwtTestConfiguration.cs` le a `Secretkey` desse arquivo e permite sobrescreve-la pela variavel de ambiente `Secretkey`. Assim, basta configurar a chave no projeto da API antes de executar `dotnet test`; nenhum valor de JWT fica duplicado nos testes. Se a chave estiver vazia, o teste falha com uma mensagem explicando como configura-la. O teste `Excluir_UsuarioExistente_DeveRemoverDefinitivamente` espera `200 OK`, conforme o endpoint DELETE.


A suíte possui **46 casos de teste** (43 métodos `[Fact]` e três casos `[InlineData]` de um `[Theory]`), distribuídos entre testes unitários e testes de integração com xUnit. Os testes seguem o padrão AAA (`Arrange`, `Act`, `Assert`). Para executar: `dotnet test RotaLeve.sln`.

## Idempotência na criação de relatos

O endpoint `POST /api/relatos` exige o header `Idempotency-Key` (ASCII visível, até 200 caracteres).
O servidor calcula SHA-256 de `usuarioId:chave`, persistido em `RELATO.CHAVE_IDEMPOTENCIA` (VARCHAR2(64), índice UNIQUE), e SHA-256 do conteúdo normalizado em `RELATO.HASH_REQUISICAO`.

- Primeira criação com chave inédita: `201 Created`.
- Repetição com mesma chave, mesmo usuário e mesmo conteúdo: `200 OK`, mesmo relato, sem novo INSERT.
- Repetição com mesma chave e conteúdo diferente: `409 Conflict`.
- Header ausente ou inválido: `400 Bad Request`.
- Usuários diferentes podem reutilizar o mesmo texto de chave sem colisão.
- Uma violação de unicidade por duas criações simultâneas é tratada recuperando o registro já gravado.

A migration `20261116000100_AddRelatoIdempotencia` adiciona as colunas sem apagar relatos existentes.
Relatos antigos ficam com `CHAVE_IDEMPOTENCIA = NULL`, o que o índice UNIQUE do Oracle permite.
Para testes no Swagger, envie um valor novo de `Idempotency-Key` no POST de relato e repita exatamente a mesma chamada.
Se um relato for excluído, sua chave deixa de existir: o mecanismo preserva idempotência enquanto o registro permanece no banco.
