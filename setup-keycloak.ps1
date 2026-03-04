Write-Host "Aguardando Keycloak inicializar..."
$ready = $false
for ($i = 0; $i -lt 20; $i++) {
    try {
        $null = Invoke-RestMethod -Uri "http://localhost:8080/realms/master" -Method Get -ErrorAction Stop
        $ready = $true; break
    } catch { Start-Sleep -Seconds 3 }
}
if (-not $ready) { Write-Error "Keycloak nao respondeu"; exit 1 }

# Token de admin
$adminToken = (Invoke-RestMethod `
    -Uri "http://localhost:8080/realms/master/protocol/openid-connect/token" `
    -Method Post `
    -Body @{ client_id="admin-cli"; username="admin"; password="admin"; grant_type="password" } `
    -ContentType "application/x-www-form-urlencoded").access_token

$headers = @{ Authorization = "Bearer $adminToken"; "Content-Type" = "application/json" }

# 1. Criar realm shopsense (ignora erro se ja existe)
Write-Host "Criando realm shopsense..."
$realmBody = @{
    realm = "shopsense"
    enabled = $true
    registrationAllowed = $false
} | ConvertTo-Json
try { Invoke-RestMethod -Uri "http://localhost:8080/admin/realms" -Method Post -Headers $headers -Body $realmBody } catch {}

# 2. Criar client shopsense-frontend
Write-Host "Criando client shopsense-frontend..."
$clientBody = @{
    clientId = "shopsense-frontend"
    enabled = $true
    publicClient = $true
    directAccessGrantsEnabled = $true
    redirectUris = @("http://localhost:5173/*")
    webOrigins = @("http://localhost:5173")
} | ConvertTo-Json
try { Invoke-RestMethod -Uri "http://localhost:8080/admin/realms/shopsense/clients" -Method Post -Headers $headers -Body $clientBody } catch {}

# 2a. Adicionar audience mapper no client
$clientId = (Invoke-RestMethod -Uri "http://localhost:8080/admin/realms/shopsense/clients?clientId=shopsense-frontend" -Method Get -Headers $headers)[0].id
$mapperBody = @{
    name = "audience-mapper"
    protocol = "openid-connect"
    protocolMapper = "oidc-audience-mapper"
    config = @{
        "included.client.audience" = "shopsense-frontend"
        "access.token.claim" = "true"
    }
} | ConvertTo-Json -Depth 5
try { Invoke-RestMethod -Uri "http://localhost:8080/admin/realms/shopsense/clients/$clientId/protocol-mappers/models" -Method Post -Headers $headers -Body $mapperBody } catch {}

# 3. Desabilitar required actions problemáticos
Write-Host "Desabilitando required actions..."
foreach ($action in @("VERIFY_PROFILE", "UPDATE_PROFILE", "UPDATE_PASSWORD", "VERIFY_EMAIL")) {
    $body = @{ alias=$action; enabled=$false; defaultAction=$false } | ConvertTo-Json
    try { Invoke-RestMethod -Uri "http://localhost:8080/admin/realms/shopsense/authentication/required-actions/$action" -Method Put -Headers $headers -Body $body } catch {}
}

# 4. Criar usuário testuser
Write-Host "Criando usuario testuser..."
$userBody = @{
    username = "testuser"
    email = "test@shopsense.com"
    enabled = $true
    emailVerified = $true
    requiredActions = @()
    credentials = @(@{ type="password"; value="Test@123"; temporary=$false })
} | ConvertTo-Json -Depth 5
try {
    Invoke-RestMethod -Uri "http://localhost:8080/admin/realms/shopsense/users" -Method Post -Headers $headers -Body $userBody
    Write-Host "Usuario criado!"
} catch {
    Write-Host "Usuario ja existe, atualizando senha..."
    $userId = (Invoke-RestMethod -Uri "http://localhost:8080/admin/realms/shopsense/users?username=testuser" -Method Get -Headers $headers)[0].id
    $pwBody = @{ type="password"; value="Test@123"; temporary=$false } | ConvertTo-Json
    Invoke-RestMethod -Uri "http://localhost:8080/admin/realms/shopsense/users/$userId/reset-password" -Method Put -Headers $headers -Body $pwBody
}

# 5. Testar login
Write-Host "`nTestando login..."
try {
    $loginResponse = Invoke-RestMethod `
        -Uri "http://localhost:8080/realms/shopsense/protocol/openid-connect/token" `
        -Method Post `
        -Body @{ client_id="shopsense-frontend"; username="testuser"; password="Test@123"; grant_type="password" } `
        -ContentType "application/x-www-form-urlencoded"
    Write-Host "LOGIN OK! Token expira em: $($loginResponse.expires_in)s"
} catch {
    Write-Host "Erro no login: $_"
}
