---
name: flowbox-orientation
description: >-
  FlowBox projesinde herhangi bir işe başlamadan ÖNCE oku. Projenin haritasını, katman/isimlendirme
  kurallarını, domain modelini (Order → Shipment, statü pipeline'ı), auth ve rol/sahiplik kurallarını,
  test/migration/Docker/CI komutlarını ve bilinen eksikleri anlatır. Şu durumlarda kullan: projeye uzun
  süre sonra dönüldüğünde, "bu projede ne yapılmış / nasıl çalışıyor" sorulduğunda, yeni endpoint,
  service, repository, entity, migration, test ya da yetki kuralı eklenirken, bir test/CI/Docker hatası
  ayıklanırken. Orients a fresh session on the FlowBox logistics API (.NET 9, PostgreSQL, JWT, xUnit).
---

# FlowBox — oryantasyon

FlowBox, gerçek bir kargo/lojistik backend'ini simüle eden bir **öğrenme projesidir**. Amaç CRUD değil;
event-driven mimari, cache, güvenlik, ölçekleme gibi konuları **gerçek bir problem ortaya çıktığında**
eklemek. Çalışma biçimi: "bugün ne yapıyoruz" diyerek küçük, doğrulanabilir adımlar. Büyük faz planı
çıkarılmaz; teknoloji ancak çözdüğü problem görüldüğünde eklenir (örn. yük testi Redis'e ihtiyaç
göstermedi, o yüzden Redis yok).

## Teknolojiler

.NET 9, ASP.NET Core **Minimal API**, EF Core 9 + Npgsql (**PostgreSQL 16**), FluentValidation, JWT Bearer
(`PasswordHasher<T>` ile şifre hash'i), OpenAPI + Scalar (`/scalar/v1`), Docker + docker-compose,
xUnit + FluentAssertions, Testcontainers, GitHub Actions, k6 (yük testi).

## Klasör haritası (`src/FlowBox.Api/`)

Katman klasörleri düz durur, içlerinde özellik (feature) klasörleri vardır: `Shipment`, `Courier`, `Order`, `Auth`.
İsimlendirme iş yerindeki **bubilet-panel** reposundan alınmıştır.

| Klasör | Ne var |
|---|---|
| `Endpoints/<Feature>/` | `IEndpoint` sınıfları: sadece HTTP (isteği al, Service'i çağır, sonucu HTTP cevabına çevir) |
| `Service/<Feature>/` | İş kuralları. `IService` boş marker arayüzdür; servisler **somut sınıf** olarak inject edilir |
| `Repositories/<Feature>/` | `IxxxRepository` + `EfXxxRepository` (bubilet-panel'de yok; DB'siz unit test için bilerek eklendi) |
| `Data/Ef/` | `FlowBoxDbContext` + `Models/` (entity'ler) |
| `Domain/Shipment/` | Saf iş kuralı: `ShipmentStatusTransitions` |
| `Validators/<Feature>/` | FluentValidation kuralları |
| `Extensions/` | `ClaimsPrincipalExtensions.ToCaller()` (JWT'den `Caller(UserId, Role)`) |
| `Enums/` | `ShipmentStatus`, `OrderStatus`, `UserRole` (modül altında değil, düz) |
| `Registry/` | `ServiceCollectionExtensions` (DI) + `EndpointRouteBuilderExtensions` (`MapEndpoints`) |
| `Migrations/` | EF migration'ları |

İstek akışı: `Endpoint (static Handle)` → `Service` → `IRepository` → `EF Core`. Handler bağımlılıkları
**handler parametresi** olarak alır (constructor değil). Endpoint'ler `Program.cs`'te reflection ile bulunur
(`IEndpoint`, parametresiz constructor şart); servisler `AddService()` ile otomatik kaydolur (`IService`).
**Repository'ler otomatik kaydolmaz**: `AddRepositories()` içine elle eklenir.

Servis metotları HTTP bilmez; sonucu discriminated record olarak döner (`UpdateShipmentStatusResult.Success /
ShipmentNotFound / InvalidTransition` gibi), endpoint bunu `switch` ile HTTP status'una çevirir.

## Domain

- **Order**: müşterinin talebi, `CustomerId` (→ `User`) taşır. Bugün oluşturulduğu an senkron olarak bir
  Shipment'e dönüşür (1:1). İleride ödeme onayı gibi adımlar araya girince bu ayrım işe yarayacak.
- **Shipment**: asıl takip birimi (`TrackingNumber`, `OrderId`). **ShipmentTrackingEvent**: her statü
  değişikliğinde bir kayıt (silinmez/güncellenmez). **ShipmentAssignment**: kurye atama geçmişi.
- **Courier** (Name, Phone) ve **User** (Email unique, PasswordHash, Role) birbirine **bağlı değil** (bkz. eksikler).
- **Statü akışı**: `Created → PickedUp → ArrivedAtSortingCenter → Sorted → InTransit →
  ArrivedAtDistributionCenter → OutForDelivery → Delivered`. `Failed`, terminal olmayan her durumdan
  erişilebilir. Adım atlanamaz; `Delivered`/`Failed` sonrası değişiklik yapılamaz.

## API ve yetkiler

| Endpoint | Kim |
|---|---|
| `POST /api/auth/register` | Herkes (her zaman **Customer** oluşturur; gövdedeki `role` yok sayılır) |
| `POST /api/auth/login` | Herkes |
| `POST /api/admin/users` | Admin (Courier/Admin/Customer hesabı oluşturur) |
| `POST /api/orders`, `GET /api/orders/mine` | Customer, Admin |
| `GET /api/shipments/{tn}`, `/tracking`, `/history` | Giriş yapmış herkes + **sahiplik**: Admin hepsini, Customer sadece kendi siparişinin kargosunu görür; başkasınınki **404** (403 değil, varlığı sızdırmamak için) |
| `PATCH /api/shipments/{tn}/status` | Courier, Admin |
| `PATCH /api/shipments/{tn}/assign`, `POST /api/couriers`, `GET /api/shipments` | Admin |

Yetki kuralı endpoint'te `.RequireAuthorization(policy => policy.RequireRole(...))` ile verilir; sahiplik
kuralı Service'te (`ShipmentService.CanAccess`). Auth **opt-in**: yeni endpoint'e `RequireAuthorization`
eklemeyi unutursan anonim olur.

## Auth

- JWT (HS256), rol claim'i `ClaimTypes.Role`. `Jwt:Issuer/Audience/ExpiryMinutes` `appsettings.json`'da;
  **`Jwt:Secret` boşsa uygulama açılışta hata verip durur**. Dev secret `appsettings.Development.json`'da,
  production'da `Jwt__Secret` ortam değişkeninden gelmeli.
- **İlk Admin**: Admin'i sadece Admin oluşturabildiği için `AdminSeed:Email/Password` ayarı açılışta (idempotent)
  bir Admin yaratır; dev değeri `appsettings.Development.json`'da, production'da `AdminSeed__Email` /
  `AdminSeed__Password` ortam değişkenleri. Mevcut bir kullanıcının rolünü değiştirmez.
- Yanlış şifre ile bilinmeyen email aynı cevabı (401) verir, bilerek.

## Tuzaklar (yeni gelenleri yakalayan kurallar)

1. **Namespace–tip çakışması.** Feature klasör adları entity adlarıyla aynı (`Shipment`, `Courier`, `Order`), bu
   yüzden `FlowBox.Api.Service.Order` içinde çıplak `Order`/`Shipment`/`Courier` yazmak `CS0118 'X' is a namespace
   but is used like a type` verir. Entity'yi **tam nitelikli** yaz: `Data.Ef.Models.Shipment`.
   (`User` çakışmaz, klasör adı `Auth`.)
2. **Migration dosyasını elle silme.** `dotnet ef migrations add` snapshot'ı da ilerletir; dosyayı elle silersen
   snapshot ileride kalır ve sonraki migration eksik çıkar. Doğrusu `dotnet ef migrations remove` (DB'ye bağlanır,
   Docker açık olmalı). Namespace taşımaları boş migration üretir, zararsızdır.
3. **Zorunlu FK/kolon eklemek mevcut yerel veriyi bozar** (örn. `Orders.CustomerId`): eski satırlar için migration
   uygulanamaz, API açılmaz. Yerel DB'yi sıfırla: `docker compose down -v`.
4. `FluentValidation` doğrulaması `AddValidatorsFromAssemblyContaining<Program>()` ile otomatik taranır; validator
   sınıfı eklemek yeterli. Endpoint `IValidator<TRequest>`'i parametre olarak alır.
5. **Yeni koda yorum satırı eklenmez** (proje sahibi istemedi); eski dosyalardaki yorumlar kalabilir.
6. Yol karışıklığı: `FlowBox.sln` **repo kökünde**, `docker-compose.yml` `src/` içinde. Terminal `src/` içindeyken
   `src/...` ile başlayan yol yazma.

## Test

- `FlowBox.UnitTests` (**Docker gerekmez**, saniyeler): `Domain/` (statü kuralları), `Service/<Feature>/`
  (sahte repository'lerle iş kuralları), `Validators/`, `Fakes/` (`Fake*Repository`).
- `FlowBox.IntegrationTests` (**Docker gerekir**): `WebApplicationFactory` + Testcontainers ile gerçek Postgres,
  her sınıf kendi container'ında, migration'lar sıfırdan uygulanır. `TestAuthHelper.AuthenticateAsAsync(rol)` rol
  için gerçek akışla token alır (Customer: register; Courier/Admin: seed Admin'in `/api/admin/users`'ı).
  Factory `AdminSeed`'i test değerleriyle ezer.
- Son durum: **60 unit + 24 integration**, hepsi yeşil. Yeni özellik = hem unit hem integration test.

```bash
dotnet test src/FlowBox.UnitTests/FlowBox.UnitTests.csproj
dotnet test src/FlowBox.IntegrationTests/FlowBox.IntegrationTests.csproj
```

## Çalıştırma

```bash
cd src && docker compose up --build -d     # API: http://localhost:8080  (Scalar: /scalar/v1)
docker compose down -v                      # DB'yi sıfırlar (yerel veri silinir)
```

- Compose'ta Postgres için healthcheck var, API `condition: service_healthy` ile onu bekler.
- Compose içinde DB host'u `db`; Rider/DataGrip gibi **dışarıdan** bağlanırken `localhost:5432`
  (kullanıcı/şifre/DB adı `docker-compose.yml`'de).
- Scalar'da JWT için "Authorize" kutusu yok (OpenAPI'ye security scheme eklenmedi); `Authorization: Bearer <token>`
  header'ını curl/Postman ile elle ver.

## CI ve yük testi

- `.github/workflows/ci.yml`: `main`'e push ve PR'da restore → build → unit → integration (runner'ın Docker'ı ile).
  Her özellik ayrı branch + PR; CI yeşil olunca merge.
- `load-tests/tracking-lookup.js` (k6): kendi Customer'ını kaydedip `GET /api/shipments/{tn}`'e 20→100→300 VU ile yük
  bindirir. Taban çizgisi: p95 ≈ 8–14 ms, %0 hata, ~125 istek/sn. Çalıştırma: `k6 run load-tests/tracking-lookup.js`.

## Bilinen eksikler / sıradaki işler

1. **Courier sahipliği yok**: Courier şu an tüm kargoları görebiliyor ve güncelleyebiliyor. Önce `User` ↔ `Courier`
   bağlanmalı, sonra "kurye sadece kendisine atanmış kargoları görür".
2. **`TrackingNumber` rastgele 6 haneli, unique kısıt ve index yok**: çakışma mümkün, tablo büyüyünce sorgu yavaşlar.
3. Admin'in oluşturduğu sipariş Admin hesabına yazılır.
4. Refresh token, rate limiting, hesap kilitleme yok. JWT 60 dk.
5. Container'da DataProtection anahtarları kalıcı değil (uyarı verir; şu an kullanılan bir özellik yok).
6. Vizyondan yapılmayanlar: Kafka/event-driven, Redis, Elasticsearch, observability, mikroservislere bölme,
   mobil uygulamalar, rota optimizasyonu, AI tahminleme, harici servisler (SMS, harita, ödeme).

## Yeni özellik eklerken kontrol listesi

1. Branch aç (`feature/...`). 2. Entity/enum → `Data/Ef/Models`, `Enums`. 3. Repository arayüzü + EF implementasyonu →
`AddRepositories()`'a kaydet. 4. Service (somut sınıf, `: IService`) + sonuç record'ları. 5. Endpoint (`IEndpoint`,
`RequireAuthorization`) + Validator. 6. Migration (`dotnet ef migrations add`, içeriğine bak). 7. Unit + integration
test. 8. `dotnet test` (ikisi de), commit, PR, CI yeşil, merge.
