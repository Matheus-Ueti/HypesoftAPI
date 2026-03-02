$adminToken = (Invoke-RestMethod `
    -Uri "http://localhost:8080/realms/master/protocol/openid-connect/token" `
    -Method Post `
    -Body @{ client_id="admin-cli"; username="admin"; password="admin"; grant_type="password" } `
    -ContentType "application/x-www-form-urlencoded").access_token

$headers = @{ Authorization = "Bearer $adminToken"; "Content-Type" = "application/json" }


Write-Host "=== Required Actions do Realm ==="
$actions = Invoke-RestMethod -Uri "http://localhost:8080/admin/realms/shopsense/authentication/required-actions" -Method Get -Headers $headers
$actions | ForEach-Object { Write-Host "$($_.alias) - enabled:$($_.enabled) default:$($_.defaultAction)" }

Write-Host "`nDesabilitando VERIFY_PROFILE..."
$vpAction = @{ alias="VERIFY_PROFILE"; name="Verify Profile"; providerId="VERIFY_PROFILE"; enabled=$false; defaultAction=$false; priority=90; config=@{} } | ConvertTo-Json -Depth 5
try {
    Invoke-RestMethod -Uri "http://localhost:8080/admin/realms/shopsense/authentication/required-actions/VERIFY_PROFILE" -Method Put -Headers $headers -Body $vpAction
    Write-Host "VERIFY_PROFILE desabilitado!"
} catch { Write-Host "Aviso: $_" }

# Buscar usuário
$user = (Invoke-RestMethod -Uri "http://localhost:8080/admin/realms/shopsense/users?username=testuser" -Method Get -Headers $headers)[0]
Write-Host "User ID: $($user.id)"
Write-Host "Required actions: $($user.requiredActions -join ', ')"
Write-Host "Email verified: $($user.emailVerified)"

# Atualizar usuário removendo required actions
$updateBody = @{
    id = $user.id
    username = "testuser"
    email = "test@shopsense.com"
    enabled = $true
    emailVerified = $true
    requiredActions = @()
} | ConvertTo-Json -Depth 5

Invoke-RestMethod -Uri "http://localhost:8080/admin/realms/shopsense/users/$($user.id)" -Method Put -Headers $headers -Body $updateBody
Write-Host "Usuário atualizado!"

# Redefinir senha explicitamente
$passwordBody = @{ type="password"; value="Test@123"; temporary=$false } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8080/admin/realms/shopsense/users/$($user.id)/reset-password" -Method Put -Headers $headers -Body $passwordBody
Write-Host "Senha redefinida!"

# Testar login
Write-Host "`nTestando login..."
try {
    $loginResponse = Invoke-RestMethod `
        -Uri "http://localhost:8080/realms/shopsense/protocol/openid-connect/token" `
        -Method Post `
        -Body @{ client_id="shopsense-frontend"; username="testuser"; password="Test@123"; grant_type="password" } `
        -ContentType "application/x-www-form-urlencoded"
    Write-Host "LOGIN OK! Token expira em: $($loginResponse.expires_in)s"
    Write-Host "ACCESS TOKEN:"
    Write-Host $loginResponse.access_token
} catch {
    Write-Host "Erro no login: $_"
}
