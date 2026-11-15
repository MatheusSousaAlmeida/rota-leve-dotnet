param(
    [string]$BaseUrl = "http://localhost:5139"
)

$ErrorActionPreference = "Stop"
$BaseUrl = $BaseUrl.TrimEnd('/')
$email = "teste.jwt.$([guid]::NewGuid().ToString('N').Substring(0,10))@rotaleve.local"
$password = "Teste@123456"

Write-Host "1/6 Cadastro publico..."
$usuario = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/usuarios" -ContentType "application/json" -Body (@{
    nome = "Admin de Teste"
    email = $email
    senha = $password
    perfil = "admin"
} | ConvertTo-Json)

Write-Host "2/6 Login..."
$login = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/login" -ContentType "application/json" -Body (@{
    email = $email
    senha = $password
} | ConvertTo-Json)
if ([string]::IsNullOrWhiteSpace($login.accessToken)) { throw "Login nao retornou accessToken" }
$auth = @{ Authorization = "Bearer $($login.accessToken)" }

Write-Host "3/6 Testando GET sem JWT (401)..."
try {
    $semJwt = Invoke-WebRequest -Method Get -Uri "$BaseUrl/api/usuarios" -SkipHttpErrorCheck
    if ([int]$semJwt.StatusCode -ne 401) { throw "Esperado 401, recebido $($semJwt.StatusCode)" }
} catch { throw "Falha ao verificar 401: $_" }

Write-Host "4/6 Testando GET com JWT..."
$usuarios = Invoke-RestMethod -Method Get -Uri "$BaseUrl/api/usuarios" -Headers $auth
Write-Host "  GET usuarios autorizado."

Write-Host "5/6 Health live com JWT..."
$live = Invoke-RestMethod -Method Get -Uri "$BaseUrl/health/live" -Headers $auth
if ($live.status -ne "Healthy") { throw "Health live inesperado: $($live.status)" }

Write-Host "6/6 Health ready com JWT (Oracle)..."
$ready = Invoke-RestMethod -Method Get -Uri "$BaseUrl/health/ready" -Headers $auth
if ($ready.status -ne "Healthy") { throw "Oracle indisponivel: $($ready.status)" }

Write-Host "OK - cadastro, login JWT, autorizacao, 401 e health Oracle." -ForegroundColor Green
