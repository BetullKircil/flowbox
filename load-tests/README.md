# Yük testleri (k6)

## Çalıştırmadan önce

API'nin Docker'da ayakta olması lazım (`localhost:8080` üzerinden erişilebilir):

```bash
cd src
docker compose up --build
```

## Çalıştırma

Başka bir terminalde, repo kökünden:

```bash
k6 run load-tests/tracking-lookup.js
```

Farklı bir adreste çalıştırmak isterseniz (örn. staging):

```bash
k6 run -e BASE_URL=http://staging.example.com load-tests/tracking-lookup.js
```

## Senaryo ne yapıyor

`tracking-lookup.js`: önce bir sipariş oluşturup bir tracking number alıyor, sonra
o tek kargoyu **giderek artan sayıda eşzamanlı kullanıcı** (20 → 100 → 300 VU) ile
tekrar tekrar sorguluyor — "müşteri kargom nerede diye sürekli soruyor" senaryosunun
simülasyonu. Toplam ~3.5 dakika sürüyor.

## Sonuçları okurken nelere bakılır

Test bitince terminalde bir özet basılır. En çok önem verilecekler:

- **`http_req_duration`** (özellikle `p(95)`) — isteklerin %95'i ne kadar sürede
  dönüyor. Eşik: 300ms altı (`thresholds` içinde tanımlı — aşılırsa k6 testi
  başarısız (❌) olarak işaretler).
- **`http_req_failed`** — kaç isteğin hata döndüğü. Eşik: %1 altı.
- **VU sayısı arttıkça bu iki metriğin nasıl bozulduğu** — asıl aranan şey bu.
  Sistem 20 VU'da rahatsa, 300 VU'da p95 aniden fırlıyorsa, "nerede tıkanıyoruz"
  sorusunun cevabı orada.

## Bilinen bir gözlem

`Shipments.TrackingNumber` kolonunda şu an bir index yok — her sorgu tabloyu
tam tarıyor. Bu test tek bir kargoyla çalıştığı için (tabloda az satır varken)
bunu göstermez; ama tabloda milyonlarca satır olduğunda bu, tam olarak
vizyon dokümanındaki "10 milyon shipment var, sorgu 8 saniye sürüyor" senaryosu
haline gelir. Bugün kapsamımızın dışında tutuyoruz, ileride "database
optimization" adımında ele alınacak.
