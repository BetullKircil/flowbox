# FlowBox

Kargo/lojistik backend'i (.NET 9, Minimal API, PostgreSQL, JWT). Öğrenme projesi. Projeyi tanımak için **önce**
`.claude/skills/flowbox-orientation/SKILL.md` dosyasını oku (harita, domain, yetkiler, tuzaklar, eksikler).

## Çalışma biçimi

- Kullanıcıyla **Türkçe** konuş. Kod ve identifier'lar İngilizce; kullanıcıya dönen doğrulama/hata mesajları Türkçe.
- Küçük, doğrulanabilir adımlarla ilerle. Büyük faz planı çıkarma. Bir teknolojiyi (Kafka, Redis vb.) ancak çözdüğü
  problem ortaya çıkınca öner.
- Gerçek bir tasarım kararı kullanıcıya aitse (örn. 403 mü 404 mü) kısa bir öneriyle birlikte sor; geri kalanını kendin karar ver.
- **Yeni koda yorum satırı ekleme.**
- Kullanıcı commit atar. Sorulmadan `git commit` / `git push` yapma. Her özellik ayrı branch + PR ile `main`'e gider;
  `main`'e doğrudan değişiklik yapma.
- Şifre, secret ya da token değerlerini yanıtlarda yazma. Dev değerleri `appsettings.Development.json`'da durur.

## Kod kuralları

- Katman akışı: `Endpoint` (sadece HTTP) → `Service` (iş kuralı, `IService`, somut sınıf) → `IRepository` → EF Core.
  İsimlendirme bubilet-panel reposuna benzer; yeni dosyaları mevcut örneklere bakarak aynı desende yaz.
- Yeni repository'yi `AddRepositories()`'a elle kaydet (servisler reflection ile otomatik kaydolur).
- Entity tiplerini feature namespace'i içinde tam nitelikli yaz (`Data.Ef.Models.Shipment`), yoksa `CS0118` alırsın.
- Her yeni endpoint'e bilinçli bir yetki ver (`RequireAuthorization`). Auth opt-in: unutursan endpoint anonim olur.
- Migration: `dotnet ef migrations add` ile üret, içeriğine bak. Dosyayı elle silme, `dotnet ef migrations remove` kullan.
  Zorunlu FK/kolon eklemek yerel veriyi bozar, kullanıcıya `docker compose down -v` gerektiğini söyle.

## Test ve doğrulama

- Yeni özellik = **unit + integration test**. Bitirmeden ikisini de çalıştır, sonucu dürüstçe raporla.
- Integration testler Docker ister. `FlowBox.sln` repo kökünde, `docker-compose.yml` `src/` içinde.

```bash
dotnet test src/FlowBox.UnitTests/FlowBox.UnitTests.csproj
dotnet test src/FlowBox.IntegrationTests/FlowBox.IntegrationTests.csproj
```

## İş akışları

**Yeni özellik**
1. Kullanıcı branch'i açtığını söyleyince başla. `main` üzerindeysen önce uyar.
2. Skill'i ve ilgili mevcut dosyaları oku, aynı desende yaz.
3. Sıra: entity/enum → repository (+ DI kaydı) → service → endpoint + validator → migration → testler.
4. `dotnet build` ve iki test setini çalıştır, hepsi yeşil olmadan "bitti" deme.
5. Özet ver: ne değişti, bilerek neyi açık bıraktın, kullanıcının yapacağı adımlar (varsa DB sıfırlama) ve commit komutları.

**Hata ayıklama**
1. Önce gerçek nedeni bul: log, yığın izi, `docker compose logs api`. Tahminle kod değiştirme.
2. Ortam sorunu (Docker kapalı, yanlış dizin, eski image) ile kod sorununu ayır. Ortam sorununda kullanıcıya ne yapacağını söyle.
3. Düzelttikten sonra aynı yolla doğrula ve neyin bozuk olduğunu açıkça yaz.

**Şema değişikliği**
1. Entity'yi değiştir, `dotnet ef migrations add <Ad>` çalıştır.
2. Üretilen migration'ı oku. `defaultValue: Guid.Empty` gibi bir şey ya da mevcut satırlarda FK ihlali riski varsa kullanıcıya söyle.
3. Yerel DB'de veri olup olmadığına bak, gerekiyorsa `docker compose down -v` iste (silmeden önce ne olduğunu kontrol et).
4. Integration testler migration'ları sıfırdan uyguladığı için ikinci doğrulama olur.

**Yük testi**
- `k6 run load-tests/tracking-lookup.js` (API `localhost:8080`'de ayakta olmalı). Sonucu p95 ve hata oranıyla özetle,
  sistem kırılmadıysa yeni teknoloji önerme.

**Teslim**
1. Kullanıcıya şu komutları ver (sen çalıştırma): `git add -A`, kısa İngilizce bir mesajla `git commit -m "add ..."`,
   `git push -u origin <branch>`.
2. Kullanıcı PR açar, CI yeşil olunca merge eder.
3. Merge sonrası: `git checkout main`, `git pull`, yeni branch.
