[CmdletBinding()]
param(
    [string]$BaseUrl = 'https://localhost:7288'
)

$ErrorActionPreference = 'Stop'

if (-not ('TrustAllCertsPolicy' -as [type])) {
    Add-Type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAllCertsPolicy : ICertificatePolicy {
    public bool CheckValidationResult(ServicePoint sp, X509Certificate cert, WebRequest req, int problem) { return true; }
}
"@
}
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

Write-Host "1) Izleyici girisi..." -ForegroundColor Cyan
$login = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/login" `
    -ContentType 'application/json; charset=utf-8' `
    -Body (@{ email='izleyici@wms.local'; password='Viewer@123' } | ConvertTo-Json -Compress)
if (-not $login.success) { throw "Giris basarisiz: $($login.message)" }
$tok = $login.data.accessToken
Write-Host "   OK - rol: $($login.data.role)" -ForegroundColor Green
$hdr = @{ Authorization = "Bearer $tok" }

Write-Host "`n2) Okuma testleri (200 beklenir)..." -ForegroundColor Cyan
$readEndpoints = @(
    '/api/dashboard/summary',
    '/api/products?pageNumber=1&pageSize=5',
    '/api/orders?pageNumber=1&pageSize=5',
    '/api/customers?pageNumber=1&pageSize=5',
    '/api/warehouses?pageNumber=1&pageSize=5',
    '/api/stock/balances?pageNumber=1&pageSize=5',
    '/api/suppliers?pageNumber=1&pageSize=5',
    '/api/purchase-requisitions?pageNumber=1&pageSize=5'
)
foreach ($p in $readEndpoints) {
    try {
        $r = Invoke-WebRequest -Method Get -Uri "$BaseUrl$p" -Headers $hdr -UseBasicParsing
        Write-Host "   GET $p -> $($r.StatusCode)" -ForegroundColor Green
    } catch {
        $code = try { $_.Exception.Response.StatusCode.value__ } catch { 'n/a' }
        Write-Host "   GET $p -> $code" -ForegroundColor Red
    }
}

Write-Host "`n3) Yazma testleri (403 / 405 beklenir)..." -ForegroundColor Cyan
$writeTests = @(
    @{ method='POST';   path='/api/products';            body=@{ name='X';sku='X';categoryId=[Guid]::Empty } },
    @{ method='POST';   path='/api/orders';              body=@{ items=@() } },
    @{ method='POST';   path='/api/customers';           body=@{ name='X' } },
    @{ method='POST';   path='/api/warehouses';          body=@{ name='X';city='X';address='X';isActive=$true } },
    @{ method='POST';   path='/api/stock/entry';         body=@{ productId=[Guid]::Empty;locationId=[Guid]::Empty;quantity=1 } },
    @{ method='DELETE'; path='/api/customers/00000000-0000-0000-0000-000000000000'; body=$null }
)
foreach ($t in $writeTests) {
    try {
        $params = @{ Method=$t.method; Uri="$BaseUrl$($t.path)"; Headers=$hdr; UseBasicParsing=$true }
        if ($t.body) { $params.Body = ($t.body | ConvertTo-Json -Compress); $params.ContentType = 'application/json' }
        $r = Invoke-WebRequest @params
        Write-Host "   $($t.method) $($t.path) -> $($r.StatusCode) (BEKLENMEDIK)" -ForegroundColor Yellow
    } catch {
        $code = try { $_.Exception.Response.StatusCode.value__ } catch { 'n/a' }
        $sym = if ($code -eq 403 -or $code -eq 401) { 'Green' } else { 'Yellow' }
        Write-Host "   $($t.method) $($t.path) -> $code" -ForegroundColor $sym
    }
}
