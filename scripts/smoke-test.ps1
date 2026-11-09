param(
    [string]$IdentityUrl = 'https://localhost:7001',
    [string]$ApiUrl = 'https://localhost:7039',
    [Parameter(Mandatory = $true)][string]$AdminPassword,
    [Parameter(Mandatory = $true)][string]$ManagerPassword
)

$ErrorActionPreference = 'Stop'

function Get-AccessToken([string]$UserName, [string]$Password) {
    $body = @{ userName = $UserName; password = $Password } | ConvertTo-Json
    (Invoke-RestMethod -Method Post -Uri "$IdentityUrl/api/auth/login" -ContentType 'application/json' -Body $body).accessToken
}

function Invoke-Api([string]$Method, [string]$Path, [string]$Token = $null, $Body = $null) {
    $headers = @{}
    if ($Token) { $headers['Authorization'] = "Bearer $Token" }
    $request = @{ Method = $Method; Uri = "$ApiUrl$Path"; Headers = $headers; UseBasicParsing = $true }
    if ($null -ne $Body) {
        $request['ContentType'] = 'application/json'
        $request['Body'] = $Body | ConvertTo-Json
    }
    try {
        $response = Invoke-WebRequest @request
        $content = if ($response.Content) { $response.Content | ConvertFrom-Json } else { $null }
        return [pscustomobject]@{ Status = [int]$response.StatusCode; Body = $content; ContentType = "$($response.Headers['Content-Type'])" }
    }
    catch {
        $errorRecord = $_
        if ($null -eq $errorRecord.Exception.Response) { throw }
        $errorResponse = $errorRecord.Exception.Response
        $contentType = $null
        if ($errorResponse -is [System.Net.WebResponse]) {
            $contentType = $errorResponse.ContentType
        }
        elseif ($errorResponse.Content -and $errorResponse.Content.Headers.ContentType) {
            $contentType = $errorResponse.Content.Headers.ContentType.ToString()
        }
        $text = $null
        if ($errorRecord.ErrorDetails -and $errorRecord.ErrorDetails.Message) {
            $text = $errorRecord.ErrorDetails.Message
        }
        elseif ($errorRecord.Exception.Response -is [System.Net.WebResponse]) {
            $stream = $errorRecord.Exception.Response.GetResponseStream()
            if ($stream.CanSeek) { $stream.Position = 0 }
            $text = (New-Object System.IO.StreamReader($stream)).ReadToEnd()
        }
        $content = $null
        if ($text) {
            try { $content = $text | ConvertFrom-Json } catch { $content = $null }
        }
        return [pscustomobject]@{ Status = [int]$errorResponse.StatusCode; Body = $content; ContentType = $contentType }
    }
}

function Assert-Status($Result, [int]$Expected, [string]$Step) {
    if ($Result.Status -ne $Expected) { throw "FALHOU: $Step (esperado $Expected, obtido $($Result.Status))" }
    Write-Host "OK  $Step -> $Expected"
}

function Assert-True([bool]$Condition, [string]$Step) {
    if (-not $Condition) { throw "FALHOU: $Step" }
    Write-Host "OK  $Step"
}

function Assert-ProblemContentType($Result, [string]$Step) {
    Assert-True ("$($Result.ContentType)".StartsWith('application/problem+json')) "$Step com Content-Type application/problem+json (obtido '$($Result.ContentType)')"
}

function Assert-Problem($Result, [int]$ExpectedStatus, [string]$ExpectedCode, [string]$Step) {
    Assert-Status $Result $ExpectedStatus $Step
    Assert-ProblemContentType $Result $Step
    Assert-True ($Result.Body.code -eq $ExpectedCode -and $Result.Body.traceId) "$Step com code $ExpectedCode e traceId"
}

$page1 = Invoke-Api GET '/api/products'
Assert-Status $page1 200 'GET /api/products'
Assert-True ($page1.Body.items.Count -eq 5 -and $page1.Body.hasMore -and $page1.Body.nextCursor -eq 100005) 'primeira página com 5 produtos e cursor 100005'

$page2 = Invoke-Api GET "/api/products?lastItemId=$($page1.Body.nextCursor)"
Assert-Status $page2 200 'GET /api/products?lastItemId=100005'
Assert-True ($page2.Body.items.Count -ge 5 -and $page2.Body.items[0].id -eq 100006) 'segunda página começa em 100006'

Assert-Status (Invoke-Api GET '/api/products/100001') 200 'GET /api/products/100001'
Assert-Status (Invoke-Api GET '/api/products/99999') 400 'GET /api/products/99999 (id inválido)'
Assert-Problem (Invoke-Api GET '/api/products/999999') 404 'Product.NotFound' 'GET /api/products/999999 (inexistente)'
Assert-Problem (Invoke-Api GET '/api/nope') 404 'Route.NotFound' 'GET /api/nope (rota inexistente)'

$search = Invoke-Api GET '/api/products/search?name=mart'
Assert-Status $search 200 'GET /api/products/search?name=mart'
Assert-True ($search.Body.items[0].name -eq 'Martelo de Carpinteiro') 'pesquisa parcial encontra o Martelo'

$missingName = Invoke-Api GET '/api/products/search'
Assert-Status $missingName 400 'GET /api/products/search sem name'
Assert-True ($missingName.Body.code -eq 'Validation.Failed') 'erro de binding com code Validation.Failed'
Assert-ProblemContentType $missingName 'erro de binding'

$range = Invoke-Api GET '/api/products/stock-level?min=0&max=30'
Assert-Status $range 200 'GET /api/products/stock-level?min=0&max=30'
Assert-True (@($range.Body.items | ForEach-Object { $_.id }) -contains 100002) 'intervalo de stock inclui o Berbequim (15)'

$newProduct = @{ name = 'Produto de Teste'; description = 'Criado pelo smoke test'; price = 4.99; initialStock = 10 }
Assert-Problem (Invoke-Api POST '/api/products' $null $newProduct) 401 'Auth.Unauthorized' 'POST sem token'

$managerToken = Get-AccessToken 'Manager' $ManagerPassword
$adminToken = Get-AccessToken 'Admin' $AdminPassword

Assert-Problem (Invoke-Api POST '/api/products' $managerToken $newProduct) 403 'Auth.Forbidden' 'POST com Manager'
Assert-Status (Invoke-Api POST '/api/products' $adminToken @{ name = ''; price = 0; initialStock = -1 }) 400 'POST inválido'

$created = Invoke-Api POST '/api/products' $adminToken $newProduct
Assert-Status $created 201 'POST com Admin'
$id = $created.Body.id
Assert-True ($id -ge 100011 -and $id -le 999999 -and $created.Body.stockQuantity -eq 10) "id gerado ($id) com 6 dígitos e stock 10"

Assert-True ($null -eq $created.Body.PSObject.Properties['category']) 'resposta já não inclui category'

$update = @{ name = 'Produto de Teste Alterado'; description = $null; price = 5.49 }
Assert-Status (Invoke-Api PUT "/api/products/$id" $managerToken $update) 200 'PUT com Manager'

$added = Invoke-Api POST "/api/products/$id/add-to-stock/5" $managerToken
Assert-Status $added 200 'add-to-stock 5'
Assert-True ($added.Body.quantity -eq 15) 'stock passou a 15'

Assert-Status (Invoke-Api POST "/api/products/$id/decrement-stock/100" $managerToken) 422 'decrement-stock acima do disponível'
$decreased = Invoke-Api POST "/api/products/$id/decrement-stock/5" $managerToken
Assert-Status $decreased 200 'decrement-stock 5'
Assert-True ($decreased.Body.quantity -eq 10) 'stock voltou a 10'

Assert-Status (Invoke-Api DELETE "/api/products/$id" $managerToken) 403 'DELETE com Manager'
Assert-Status (Invoke-Api DELETE "/api/products/$id" $adminToken) 204 'DELETE com Admin (soft delete)'
Assert-Status (Invoke-Api GET "/api/products/$id") 404 'GET depois do DELETE'
Assert-Status (Invoke-Api DELETE "/api/products/$id" $adminToken) 404 'DELETE repetido'

Write-Host "`nSmoke test concluído com sucesso." -ForegroundColor Green
