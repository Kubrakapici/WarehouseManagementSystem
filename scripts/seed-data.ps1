# Seed script: WMS API'sine bol miktarda gercekci ornek veri besler.
# Kullanim: powershell -ExecutionPolicy Bypass -File scripts\seed-data.ps1

[CmdletBinding()]
param(
    [string]$BaseUrl = 'https://localhost:7288',
    [string]$Email   = 'admin@wms.local',
    [string]$Pass    = 'Admin@123'
)

$ErrorActionPreference = 'Stop'

# --- Sertifika dogrulamasini bypass et (dev ortami) ----------------------
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

# --- Yardimci fonksiyonlar -----------------------------------------------
function Log([string]$msg) { Write-Host "[seed] $msg" -ForegroundColor Cyan }

function ApiRequest {
    param(
        [Parameter(Mandatory)] [string]$Method,
        [Parameter(Mandatory)] [string]$Path,
        [object]$Body = $null,
        [bool]$RequireSuccess = $true
    )

    $uri = "$BaseUrl$Path"
    $hdr = @{}
    if ($script:Token) { $hdr['Authorization'] = "Bearer $script:Token" }

    $params = @{
        Method  = $Method
        Uri     = $uri
        Headers = $hdr
    }
    if ($null -ne $Body) {
        $params['Body']        = ($Body | ConvertTo-Json -Depth 8 -Compress)
        $params['ContentType'] = 'application/json; charset=utf-8'
    }

    try {
        $res = Invoke-RestMethod @params
    } catch {
        $code = try { $_.Exception.Response.StatusCode.value__ } catch { 'n/a' }
        Write-Host "  ! API hata $Method $Path -> HTTP $code" -ForegroundColor Yellow
        $detail = $null
        try {
            $stream  = $_.Exception.Response.GetResponseStream()
            $reader  = New-Object System.IO.StreamReader($stream)
            $detail  = $reader.ReadToEnd()
            if ($detail) { Write-Host "    body: $detail" -ForegroundColor DarkGray }
        } catch {}
        if ($RequireSuccess) { throw }
        return $null
    }

    if ($res.success -eq $false -and $RequireSuccess) {
        throw "API basarisiz: $($res.message)"
    }
    return $res
}

function PickRandom { param($items, [int]$n = 1)
    $a = @($items)
    if ($a.Count -eq 0) { return @() }
    if ($n -ge $a.Count) { return $a }
    return ($a | Get-Random -Count $n)
}

# --- 1) Login -------------------------------------------------------------
Log "Giris yapiliyor: $Email"
$login = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/login" -ContentType 'application/json; charset=utf-8' -Body (@{ email = $Email; password = $Pass } | ConvertTo-Json -Compress)
if (-not $login.success) { throw "Giris basarisiz: $($login.message)" }
$script:Token = $login.data.accessToken
Log "Token alindi."

# --- 2) Mevcut durumu listele --------------------------------------------
$existingCategories = (ApiRequest -Method GET -Path '/api/categories').data
$existingWarehouses = (ApiRequest -Method GET -Path '/api/warehouses?pageNumber=1&pageSize=200').data.items
$existingSuppliers  = (ApiRequest -Method GET -Path '/api/suppliers?pageNumber=1&pageSize=200').data.items
$existingProducts   = (ApiRequest -Method GET -Path '/api/products?pageNumber=1&pageSize=500').data.items

Log "Mevcut: $($existingCategories.Count) kategori, $($existingWarehouses.Count) depo, $($existingSuppliers.Count) tedarikci, $($existingProducts.Count) urun"

# --- 3) Kategoriler -------------------------------------------------------
function FlattenCategories {
    param($nodes)
    $list = New-Object System.Collections.Generic.List[object]
    function Visit { param($n, $bag) $bag.Add($n); if ($n.childCategories) { foreach ($c in $n.childCategories) { Visit -n $c -bag $bag } } }
    foreach ($n in $nodes) { Visit -n $n -bag $list }
    return $list
}

function EnsureCategory([string]$name, $parentId) {
    $flat = FlattenCategories -nodes $existingCategories
    $hit = $flat | Where-Object { $_.name -eq $name -and $_.parentCategoryId -eq $parentId } | Select-Object -First 1
    if ($hit) { return $hit.id }

    $body = @{ name = $name; parentCategoryId = $parentId; description = $null }
    $res = ApiRequest -Method POST -Path '/api/categories' -Body $body
    if ($res -and $res.data) {
        $script:existingCategories = (ApiRequest -Method GET -Path '/api/categories').data
        return $res.data.id
    }
    return $null
}

Log "Kategoriler ekleniyor..."
$catElectronics = EnsureCategory 'Elektronik'  $null
$catComputers   = EnsureCategory 'Bilgisayar'  $catElectronics
$catPhones      = EnsureCategory 'Telefon'     $catElectronics
$catFurniture   = EnsureCategory 'Mobilya'     $null
$catOffice      = EnsureCategory 'Ofis'        $catFurniture
$catTextile     = EnsureCategory 'Tekstil'     $null
$catFood        = EnsureCategory 'Gida'        $null
$catAccessory   = EnsureCategory 'Aksesuar'    $null
$catTools       = EnsureCategory 'El Aleti'    $null

# --- 4) Tedarikciler ------------------------------------------------------
$supplierSeeds = @(
    @{ name='Anadolu Elektronik A.S.';    taxNumber='1112223334'; phone='+90 212 555 0101'; email='satis@anadoluelektronik.com.tr'; address='IOSB Ikitelli';    isActive=$true },
    @{ name='Bursa Mobilya Ltd.';         taxNumber='2223334445'; phone='+90 224 555 0202'; email='siparis@bursamobilya.com.tr';    address='Bursa NOSAB';     isActive=$true },
    @{ name='Aydin Tekstil San.';         taxNumber='3334445556'; phone='+90 256 555 0303'; email='info@aydintekstil.com.tr';      address='Aydin OSB';       isActive=$true },
    @{ name='Kayseri Gida Tic.';          taxNumber='4445556667'; phone='+90 352 555 0404'; email='satis@kayserigida.com.tr';      address='Kayseri OSB';     isActive=$true },
    @{ name='Izmir Aksesuar Ltd.';        taxNumber='5556667778'; phone='+90 232 555 0505'; email='info@izmiraksesuar.com';        address='Izmir Cigli';     isActive=$true },
    @{ name='Konya El Aletleri San.';     taxNumber='6667778889'; phone='+90 332 555 0606'; email='satis@konyaaletleri.com.tr';    address='Konya OSB';       isActive=$true }
)

function EnsureSupplier($seed) {
    $hit = $existingSuppliers | Where-Object { $_.name -eq $seed.name } | Select-Object -First 1
    if ($hit) { return $hit.id }
    $res = ApiRequest -Method POST -Path '/api/suppliers' -Body $seed
    return $res.data.id
}

Log "Tedarikciler ekleniyor..."
$supplierIds = @()
foreach ($s in $supplierSeeds) { $supplierIds += EnsureSupplier $s }

# --- 5) Musteriler --------------------------------------------------------
$customerSeeds = @(
    @{ name='Ayse Demir';     companyName='Demir Tekstil A.S.';    taxNumber='1010101010'; phone='+90 532 100 0001'; email='ayse.demir@demirtekstil.com';  city='Istanbul'; address='Levent';     notes='VIP musteri';        isActive=$true },
    @{ name='Mehmet Yilmaz';  companyName='Yilmaz Insaat Ltd.';    taxNumber='1010101011'; phone='+90 532 100 0002'; email='mehmet@yilmazinsaat.com';      city='Ankara';   address='Cankaya';    notes=$null;                isActive=$true },
    @{ name='Zeynep Kaya';    companyName='Kaya Lojistik';         taxNumber='1010101012'; phone='+90 532 100 0003'; email='zeynep.kaya@kayalogistic.tr'; city='Izmir';    address='Karsiyaka';  notes='Aylik fatura';       isActive=$true },
    @{ name='Hakan Aydin';    companyName=$null;                   taxNumber='1010101013'; phone='+90 532 100 0004'; email='hakan@example.com';            city='Istanbul'; address='Atasehir';   notes='Bireysel musteri';   isActive=$true },
    @{ name='Elif Sahin';     companyName='Sahin Otomotiv';        taxNumber='1010101014'; phone='+90 532 100 0005'; email='elif@sahinotomotiv.com';       city='Bursa';    address='Nilufer';    notes=$null;                isActive=$true },
    @{ name='Burak Celik';    companyName='Celik Yapi A.S.';       taxNumber='1010101015'; phone='+90 532 100 0006'; email='burak.celik@celikyapi.tr';     city='Antalya';  address='Muratpasa';  notes='Insaat sektoru';     isActive=$true },
    @{ name='Selin Ozturk';   companyName='Ozturk Magazacilik';    taxNumber='1010101016'; phone='+90 532 100 0007'; email='selin@ozturkmagaza.tr';        city='Istanbul'; address='Kadikoy';    notes=$null;                isActive=$true },
    @{ name='Emre Arslan';    companyName='Arslan Group';          taxNumber='1010101017'; phone='+90 532 100 0008'; email='emre.arslan@arslangroup.tr';   city='Adana';    address='Seyhan';     notes='Toptan alici';       isActive=$true },
    @{ name='Cansu Dogan';    companyName='Dogan Tekstil';         taxNumber='1010101018'; phone='+90 532 100 0009'; email='cansu@dogantekstil.com.tr';    city='Denizli';  address='Pamukkale';  notes=$null;                isActive=$true },
    @{ name='Murat Kurt';     companyName='Kurt Tic. Ltd.';        taxNumber='1010101019'; phone='+90 532 100 0010'; email='murat.kurt@kurttic.tr';        city='Konya';    address='Selcuklu';   notes=$null;                isActive=$true },
    @{ name='Deniz Polat';    companyName=$null;                   taxNumber='1010101020'; phone='+90 532 100 0011'; email='deniz.polat@example.com';      city='Istanbul'; address='Sariyer';    notes='Bireysel';           isActive=$true },
    @{ name='Gokhan Erdogan'; companyName='Erdogan Elektronik';    taxNumber='1010101021'; phone='+90 532 100 0012'; email='gokhan@erdoganelek.com';       city='Eskisehir';address='Tepebasi';   notes=$null;                isActive=$false }
)

Log "Musteriler ekleniyor..."
$existingCustomers = (ApiRequest -Method GET -Path '/api/customers?pageNumber=1&pageSize=200').data.items
foreach ($c in $customerSeeds) {
    if ($existingCustomers | Where-Object { $_.name -eq $c.name -or $_.email -eq $c.email }) { continue }
    ApiRequest -Method POST -Path '/api/customers' -Body $c | Out-Null
}

# --- 6) Depolar ve lokasyonlar -------------------------------------------
$warehouseSeeds = @(
    @{ name='Ankara Lojistik Merkezi'; city='Ankara'; address='Ostim OSB';      isActive=$true },
    @{ name='Izmir Liman Deposu';      city='Izmir';  address='Aliaga';          isActive=$true }
)
$warehouseIds = @()
foreach ($w in $warehouseSeeds) {
    $hit = $existingWarehouses | Where-Object { $_.name -eq $w.name } | Select-Object -First 1
    if ($hit) { $warehouseIds += $hit.id; continue }
    $res = ApiRequest -Method POST -Path '/api/warehouses' -Body $w
    $warehouseIds += $res.data.id
}

# Tum mevcut depoları topla
$allWarehouses = (ApiRequest -Method GET -Path '/api/warehouses?pageNumber=1&pageSize=200').data.items
Log "Toplam depo: $($allWarehouses.Count). Her depoya 4 lokasyon eklenecek..."

foreach ($w in $allWarehouses) {
    $existingLocs = (ApiRequest -Method GET -Path "/api/locations?pageNumber=1&pageSize=500&warehouseId=$($w.id)").data.items
    foreach ($corridor in @('A','B','C','D')) {
        foreach ($shelf in @('01','02')) {
            $code = "$corridor-$shelf-01"
            if ($existingLocs | Where-Object { $_.code -eq $code }) { continue }
            $body = @{ warehouseId=$w.id; corridor=$corridor; shelf=$shelf; floor='01'; maxCapacity=200; pickSortOrder=([int]$shelf) }
            $r = ApiRequest -Method POST -Path '/api/locations' -Body $body -RequireSuccess $false
        }
    }
}

# --- 7) Urunler -----------------------------------------------------------
$productSeeds = @(
    @{ name='Ofis Sandalyesi';           sku='OFC-CHR-101'; barcode='8690000010101'; categoryId=$catOffice;     unitPrice=2800;  minimumStockLevel=15 },
    @{ name='Toplanti Masasi';           sku='OFC-DSK-102'; barcode='8690000010102'; categoryId=$catOffice;     unitPrice=4500;  minimumStockLevel=8 },
    @{ name='LED Monitor 27 inc';        sku='ELC-MON-201'; barcode='8690000010103'; categoryId=$catComputers;  unitPrice=6500;  minimumStockLevel=20 },
    @{ name='Mekanik Klavye';            sku='ELC-KBD-202'; barcode='8690000010104'; categoryId=$catComputers;  unitPrice=1450;  minimumStockLevel=25 },
    @{ name='Kablosuz Mouse';            sku='ELC-MSE-203'; barcode='8690000010105'; categoryId=$catComputers;  unitPrice=550;   minimumStockLevel=40 },
    @{ name='USB-C Hub 7 in 1';          sku='ELC-HUB-204'; barcode='8690000010106'; categoryId=$catAccessory;  unitPrice=850;   minimumStockLevel=30 },
    @{ name='Akilli Telefon 128GB';      sku='PHN-SMP-301'; barcode='8690000010107'; categoryId=$catPhones;     unitPrice=18900; minimumStockLevel=12 },
    @{ name='Telefon Kilifi Silikon';    sku='PHN-CSE-302'; barcode='8690000010108'; categoryId=$catAccessory;  unitPrice=85;    minimumStockLevel=80 },
    @{ name='Kulakustu Bluetooth Kulaklik'; sku='ELC-HPH-401'; barcode='8690000010109'; categoryId=$catAccessory; unitPrice=2300; minimumStockLevel=18 },
    @{ name='Ahsap Ofis Masasi';         sku='FRN-DSK-501'; barcode='8690000010110'; categoryId=$catFurniture;  unitPrice=3900;  minimumStockLevel=5 },
    @{ name='Calisma Lambasi LED';       sku='FRN-LMP-502'; barcode='8690000010111'; categoryId=$catFurniture;  unitPrice=420;   minimumStockLevel=30 },
    @{ name='Pamuklu T-Shirt';           sku='TXT-TSH-601'; barcode='8690000010112'; categoryId=$catTextile;    unitPrice=180;   minimumStockLevel=100 },
    @{ name='Polar Sweatshirt';          sku='TXT-SWT-602'; barcode='8690000010113'; categoryId=$catTextile;    unitPrice=320;   minimumStockLevel=60 },
    @{ name='Kanvas Spor Ayakkabi';      sku='TXT-SHO-603'; barcode='8690000010114'; categoryId=$catTextile;    unitPrice=890;   minimumStockLevel=25 },
    @{ name='Filtre Kahve 1kg';          sku='FOD-CFE-701'; barcode='8690000010115'; categoryId=$catFood;       unitPrice=240;   minimumStockLevel=50 },
    @{ name='Yesil Cay Poset 100li';     sku='FOD-TEA-702'; barcode='8690000010116'; categoryId=$catFood;       unitPrice=95;    minimumStockLevel=80 },
    @{ name='Cikolata 100g';             sku='FOD-CHC-703'; barcode='8690000010117'; categoryId=$catFood;       unitPrice=45;    minimumStockLevel=150 },
    @{ name='Tornavida Seti 16 parca';   sku='TLS-SDR-801'; barcode='8690000010118'; categoryId=$catTools;      unitPrice=320;   minimumStockLevel=20 },
    @{ name='Elektrikli Matkap';         sku='TLS-DRL-802'; barcode='8690000010119'; categoryId=$catTools;      unitPrice=1850;  minimumStockLevel=10 },
    @{ name='Olcum Metresi 5m';          sku='TLS-MSR-803'; barcode='8690000010120'; categoryId=$catTools;      unitPrice=65;    minimumStockLevel=40 }
)

Log "Urunler ekleniyor..."
foreach ($p in $productSeeds) {
    if ($existingProducts | Where-Object { $_.sku -eq $p.sku }) { continue }
    $body = @{ name=$p.name; sku=$p.sku; barcode=$p.barcode; unitPrice=$p.unitPrice; minimumStockLevel=$p.minimumStockLevel; categoryId=$p.categoryId; isActive=$true; generateQr=$true }
    ApiRequest -Method POST -Path '/api/products' -Body $body -RequireSuccess $false | Out-Null
}

# Yeni listeleri al
$allProducts   = (ApiRequest -Method GET -Path '/api/products?pageNumber=1&pageSize=500').data.items
$allLocations  = @()
foreach ($w in $allWarehouses) {
    $allLocations += (ApiRequest -Method GET -Path "/api/locations?pageNumber=1&pageSize=500&warehouseId=$($w.id)").data.items
}

Log "Toplam $($allProducts.Count) urun, $($allLocations.Count) lokasyon"

# --- 8) Stok girisleri ----------------------------------------------------
Log "Stok girisleri yapiliyor (her urun 1-2 lokasyona)..."
$rand = [System.Random]::new()
$entryCount = 0
foreach ($p in $allProducts) {
    $targetLocs = PickRandom $allLocations $rand.Next(1,3)
    foreach ($loc in $targetLocs) {
        $qty = $rand.Next(15, 120)
        $body = @{ productId=$p.id; locationId=$loc.id; quantity=$qty; description="Mal kabul - $($p.sku)" }
        $r = ApiRequest -Method POST -Path '/api/stock/entry' -Body $body -RequireSuccess $false
        if ($r) { $entryCount++ }
    }
}
Log "$entryCount stok girisi yapildi"

# --- 9) Stok cikisi + transfer ortuk (10-15 hareket) ---------------------
Log "Stok cikis ve transfer hareketleri..."
$movementCount = 0
for ($i = 0; $i -lt 12; $i++) {
    $balances = (ApiRequest -Method GET -Path '/api/stock/balances?pageNumber=1&pageSize=200').data.items
    $nonZero = $balances | Where-Object { $_.quantity -gt 5 }
    if (-not $nonZero) { break }
    $pick = PickRandom $nonZero 1 | Select-Object -First 1
    $qty = [Math]::Min($pick.quantity, $rand.Next(1, 8))
    if ($i % 3 -eq 0) {
        # transfer (farkli lokasyona)
        $other = $allLocations | Where-Object { $_.id -ne $pick.locationId } | Get-Random
        if ($other) {
            $r = ApiRequest -Method POST -Path '/api/stock/transfer' -Body @{ productId=$pick.productId; fromLocationId=$pick.locationId; toLocationId=$other.id; quantity=$qty; description="Otomatik transfer" } -RequireSuccess $false
            if ($r) { $movementCount++ }
        }
    } else {
        $r = ApiRequest -Method POST -Path '/api/stock/exit' -Body @{ productId=$pick.productId; locationId=$pick.locationId; quantity=$qty; description="Musteri sevkiyati" } -RequireSuccess $false
        if ($r) { $movementCount++ }
    }
}
Log "$movementCount ek hareket olusturuldu"

# --- 10) Siparisler -------------------------------------------------------
Log "Siparisler olusturuluyor..."
$orderCount = 0
$activeProducts = $allProducts | Where-Object { $_.isActive }
$statusFlow = @(2,3,4,5,7,8) # Pending, Approved, InProgress, Completed, Packing, Shipped
for ($i = 0; $i -lt 8; $i++) {
    $supId = $supplierIds | Get-Random
    $picks = PickRandom $activeProducts $rand.Next(1,4)
    $items = @()
    foreach ($pp in $picks) {
        $items += @{ productId=$pp.id; quantity=$rand.Next(2,12); unitPrice=$pp.unitPrice }
    }
    $body = @{ supplierId=$supId; notes="Otomatik olusturulan siparis #$($i+1)"; items=$items }
    $created = ApiRequest -Method POST -Path '/api/orders' -Body $body -RequireSuccess $false
    if ($created -and $created.data) {
        $orderCount++
        # rastgele duruma tasi (yaklasik yarisi pending kalsin)
        if (($i % 2) -eq 0) {
            $newStatus = $statusFlow[$rand.Next(0, $statusFlow.Count)]
            ApiRequest -Method PATCH -Path "/api/orders/$($created.data.id)/status" -Body @{ status = $newStatus } -RequireSuccess $false | Out-Null
        }
    }
}
Log "$orderCount siparis olusturuldu"

# --- 11) Satin alma talepleri --------------------------------------------
Log "Satin alma talepleri olusturuluyor..."
$prCount = 0
foreach ($w in $allWarehouses | Select-Object -First 3) {
    $picks = PickRandom $activeProducts 3
    $lines = @()
    foreach ($pp in $picks) {
        $lines += @{ productId=$pp.id; quantity=$rand.Next(20,60); notes=$null }
    }
    $body = @{ warehouseId=$w.id; title="$($w.name) icin haftalik tamamlama"; notes='Otomatik olusturulmustur'; lines=$lines }
    $pr = ApiRequest -Method POST -Path '/api/purchase-requisitions' -Body $body -RequireSuccess $false
    if (-not $pr -or -not $pr.data) { continue }
    $prCount++
    $prId = $pr.data.id

    # Yaklasik %75: onaya gonder
    if ($rand.Next(0,4) -gt 0) {
        ApiRequest -Method POST -Path "/api/purchase-requisitions/$prId/submit" -Body @{} -RequireSuccess $false | Out-Null

        # %66: onayla, %33 reddet
        if ($rand.Next(0,3) -gt 0) {
            ApiRequest -Method POST -Path "/api/purchase-requisitions/$prId/approve" -Body @{} -RequireSuccess $false | Out-Null
            # Onaylanmis taleplere 1-2 teklif ekle
            $offerSuppliers = PickRandom $supplierIds $rand.Next(1,3)
            foreach ($sid in $offerSuppliers) {
                $qbody = @{ supplierId=$sid; totalAmount=[double]($rand.Next(5000,25000)); currency='TRY'; notes='Otomatik teklif' }
                ApiRequest -Method POST -Path "/api/purchase-requisitions/$prId/quotes" -Body $qbody -RequireSuccess $false | Out-Null
            }
        } else {
            ApiRequest -Method POST -Path "/api/purchase-requisitions/$prId/reject" -Body @{ reason='Butce yetersiz' } -RequireSuccess $false | Out-Null
        }
    }
}
Log "$prCount satin alma talebi olusturuldu"

# --- 12) Yeni kullanicilar -----------------------------------------------
$roles = (ApiRequest -Method GET -Path '/api/roles').data
$opsRoleId      = ($roles | Where-Object { $_.name -eq 'Operations' }    | Select-Object -First 1).id
$staffRoleId    = ($roles | Where-Object { $_.name -eq 'WarehouseStaff' } | Select-Object -First 1).id
$managerRoleId  = ($roles | Where-Object { $_.name -eq 'Manager' }       | Select-Object -First 1).id

$userSeeds = @(
    @{ email='berk.tan@wms.local';   password='Pass@1234'; firstName='Berk';     lastName='Tan';    roleId=$opsRoleId;     isActive=$true; warehouseIds=@() },
    @{ email='ezgi.demir@wms.local'; password='Pass@1234'; firstName='Ezgi';     lastName='Demir';  roleId=$staffRoleId;   isActive=$true; warehouseIds=@() },
    @{ email='ali.kara@wms.local';   password='Pass@1234'; firstName='Ali';      lastName='Kara';   roleId=$managerRoleId; isActive=$true; warehouseIds=@() }
)
$existingUsers = (ApiRequest -Method GET -Path '/api/users?pageNumber=1&pageSize=200').data.items
foreach ($u in $userSeeds) {
    if ($existingUsers | Where-Object { $_.email -eq $u.email }) { continue }
    ApiRequest -Method POST -Path '/api/users' -Body $u -RequireSuccess $false | Out-Null
}

# --- 13) Sonuc ozeti ------------------------------------------------------
Log "Tamam. Ozet:"
$f = @{
    Kategori = (ApiRequest -Method GET -Path '/api/categories').data.Count
    Depo     = (ApiRequest -Method GET -Path '/api/warehouses?pageNumber=1&pageSize=500').data.totalCount
    Lokasyon = (ApiRequest -Method GET -Path '/api/locations?pageNumber=1&pageSize=500').data.totalCount
    Tedarikci= (ApiRequest -Method GET -Path '/api/suppliers?pageNumber=1&pageSize=500').data.totalCount
    Musteri  = (ApiRequest -Method GET -Path '/api/customers?pageNumber=1&pageSize=500').data.totalCount
    Urun     = (ApiRequest -Method GET -Path '/api/products?pageNumber=1&pageSize=500').data.totalCount
    Siparis  = (ApiRequest -Method GET -Path '/api/orders?pageNumber=1&pageSize=500').data.totalCount
    Talep    = (ApiRequest -Method GET -Path '/api/purchase-requisitions?pageNumber=1&pageSize=500').data.totalCount
    Kullanici= (ApiRequest -Method GET -Path '/api/users?pageNumber=1&pageSize=500').data.totalCount
}
foreach ($k in $f.Keys) { "  $($k.PadRight(10)) : $($f[$k])" }
