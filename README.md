# Depo ve Stok Yönetim Sistemi (WMS)

Kurumsal ölçek için hazırlanmış **ASP.NET Core 8 Web API + Vue 3** tabanlı depo otomasyonu. Mimari; **Clean / Onion yaklaşımı**, **Repository + UnitOfWork**, **Service katmanı**, **DTO + FluentValidation + AutoMapper**, **JWT + Refresh Token**, **SignalR**, **Serilog**, **EF Core (MSSQL)** ve **Docker** desteği içerir.

## Çözüm yapısı

```
WarehouseManagementSystem/
├── src/
│   ├── WarehouseManagement.Domain/          # Entities, enums, ortak taban sınıfları
│   ├── WarehouseManagement.Application/     # Servisler, DTO, doğrulama, AutoMapper
│   ├── WarehouseManagement.Infrastructure/# EF Core, JWT, SignalR, Excel/PDF export
│   └── WarehouseManagement.API/             # REST + Swagger + middleware + controllers
├── frontend/                                # Vue 3 + Vite + PrimeVue + Tailwind + Pinia
├── Dockerfile                               # API imajı
├── docker-compose.yml                       # MSSQL + API + UI (nginx proxy)
└── README.md
```

## Önemli modüller

- Kimlik doğrulama: JWT access token + refresh token, rol tabanlı yetkilendirme (`Admin`, `WarehouseStaff`, `Operations`, `Manager`)
- Dashboard: ürün/depo özeti, günlük giriş–çıkış toplamları, kritik stok listesi, son hareketler
- Ürün / kategori / depo / lokasyon CRUD, lokasyon kodu (ör. `A-01-B-03`)
- Stok: giriş, çıkış, transfer, sayım (sayım farkı), hareket geçmişi, kritik stok bildirimi + SignalR ile canlı güncelleme
- Sipariş ve tedarikçi yönetimi
- Kullanıcı yönetimi (Admin)
- Raporlar: kritik stok ve günlük hareket Excel; ürün hareket özeti PDF (QuestPDF)

## Veritabanı tabloları (EF ile üretilir)

`Users`, `Roles`, `Categories`, `Products`, `Warehouses`, `Locations`, `StockBalances`, `StockMovements`, `Orders`, `OrderItems`, `Suppliers`, `Notifications`

**Not:** İlk çalıştırmada projede EF migration dosyası yoksa `DbInitializer`, şema için `EnsureCreated` kullanır. Üretim ortamında EF migration kullanmanız önerilir (`dotnet ef migrations add ...`).

## Kurulum (geliştirme)

### Gereksinimler

- .NET SDK **8.x**
- Node.js **20+** (frontend)
- SQL Server (LocalDB / Docker / Kurumsal instance)

### 1) Bağlantı dizesi ve JWT

`src/WarehouseManagement.API/appsettings.Development.json` içindeki `ConnectionStrings:DefaultConnection` değerini kendi SQL Server bilginize göre güncelleyin.

`Jwt:Secret` için **en az 32 karakter** güçlü bir anahtar kullanın.

### 2) API’yi çalıştırma

```powershell
cd src/WarehouseManagement.API
dotnet restore
dotnet run
```

Swagger: `https://localhost:7288/swagger` (launchSettings’e göre)

### 3) Frontend

```powershell
cd frontend
npm install
npm run dev
```

Varsayılan UI: `http://localhost:5173`

- Geliştirmede `VITE_API_URL` boş bırakılırsa Vite proxy ile `/api` ve `/hubs` istekleri API’ye yönlendirilir (`frontend/vite.config.ts`).
- Üretim Docker senaryosunda UI nginx üzerinden `/api` ve `/hubs` adreslerini API konteynerine proxylar (`frontend/nginx.conf`).

### 4) Seed verisi ve örnek kullanıcılar

API ilk açılışta roller + örnek kullanıcıları oluşturur (şifreler kurumsal ortamda mutlaka değiştirilmelidir):

| E-posta               | Rol             | Şifre        |
|-----------------------|-----------------|-------------|
| admin@wms.local       | Admin           | Admin@123   |
| depo@wms.local        | WarehouseStaff  | Staff@123   |
| operasyon@wms.local   | Operations      | Ops@123     |
| yonetici@wms.local    | Manager         | Manager@123 |

## EF Core migration (önerilen üretim yolu)

Tasarım zamanı factory: `src/WarehouseManagement.Infrastructure/Persistence/AppDbContextFactory.cs`

Örnek komutlar:

```powershell
dotnet tool install --global dotnet-ef
cd src/WarehouseManagement.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../WarehouseManagement.API
dotnet ef database update --startup-project ../WarehouseManagement.API
```

Migration kullandığınızda `DbInitializer` içindeki `EnsureCreated` dalını devre dışı bırakmak için `__EFMigrationsHistory` dolu olacağından otomatik olarak `Migrate()` yolu seçilir.

## Docker ile çalıştırma

```powershell
docker compose up --build
```

- API: `http://localhost:8080/swagger`
- UI (nginx): `http://localhost:8081`

SQL SA şifresi `docker-compose.yml` içindedir; kurumsal kullanımda **zorunlu olarak** güçlü parola ve sırlar yönetimi uygulayın.

## Mimari notlar

- **API yanıt sarmalayıcı:** `ApiResponse<T>`
- **Sayfalama:** `PaginationParameters` + `PagedResult<T>`
- **Özel durumlar:** `ExceptionHandlingMiddleware`
- **Gerçek zamanlı:** `StockHub` (`/hubs/stock`), ürün bazlı grup için hub metotları (`SubscribeProduct`)
- **Excel:** ClosedXML · **PDF:** QuestPDF (Community lisansı geliştirme için uygundur; kurumsal kullanımda lisans koşullarını doğrulayın)

## Güvenlik checklist (üretim)

- `Jwt:Secret` ve DB parolalarını sırlar kasasında tutun
- HTTPS terminasyonu (reverse proxy) ve güvenlik başlıkları
- Rate limit kurallarını (`appsettings.json` → `IpRateLimiting`) ortamınıza göre sıkılaştırın
- Seed kullanıcıları devre dışı bırakın veya şifreleri sıfırlayın

## Lisans

Bu depo örnek / başlangıç amaçlıdır; kurumsal dağıtım öncesi güvenlik gözden geçirmesi ve kurumsal politikalarınıza uyum zorunludur.
