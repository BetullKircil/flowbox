import http from 'k6/http';
import { check, sleep } from 'k6';

// Senaryo: "Müşteri kargosunu sürekli sorguluyor" (FlowBox vizyonundaki
// "GET /shipments/{id}/location → Redis" problemi). Önce bir kargo oluşturuyoruz,
// sonra o tek kargoyu giderek artan sayıda sanal kullanıcı (VU) ile tekrar tekrar
// sorguluyoruz. Amaç: cache olmadan bu endpoint kaç eşzamanlı kullanıcıya kadar
// dayanıyor, p95 gecikme nerede bozuluyor — bunu görüp bir sonraki adıma
// (Redis cache) somut bir sebeple geçeceğiz.

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';

export const options = {
  scenarios: {
    tracking_lookup: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '20s', target: 20 },   // ısınma
        { duration: '40s', target: 20 },
        { duration: '20s', target: 100 },  // orta yük
        { duration: '40s', target: 100 },
        { duration: '20s', target: 300 },  // sistemi zorla
        { duration: '40s', target: 300 },
        { duration: '20s', target: 0 },    // soğuma
      ],
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<300'], // isteklerin %95'i 300ms altında dönmeli
    http_req_failed: ['rate<0.01'],   // hata oranı %1'i geçmemeli
  },
};

// setup() sadece bir kez, testin başında çalışır — tüm VU'ların paylaşacağı
// tracking number'ı burada üretiyoruz.
export function setup() {
  const res = http.post(
    `${BASE_URL}/api/orders`,
    JSON.stringify({ origin: 'Istanbul', destination: 'Konya', weight: 3 }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  if (res.status !== 201) {
    throw new Error(`Setup başarısız: sipariş oluşturulamadı (status=${res.status}, body=${res.body})`);
  }

  const body = JSON.parse(res.body);
  return { trackingNumber: body.trackingNumber };
}

// default() her VU için tekrar tekrar çalışan asıl senaryo.
export default function (data) {
  const res = http.get(`${BASE_URL}/api/shipments/${data.trackingNumber}`);

  check(res, {
    'status 200': (r) => r.status === 200,
  });

  sleep(1); // gerçek bir kullanıcının art arda tıklamak yerine biraz beklemesini simüle eder
}
