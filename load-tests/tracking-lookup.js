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
  const jsonHeaders = { 'Content-Type': 'application/json' };

  const email = `loadtest-${Date.now()}-${Math.floor(Math.random() * 1e6)}@flowbox.test`;
  const password = 'LoadTestPass123!';

  const registerRes = http.post(
    `${BASE_URL}/api/auth/register`,
    JSON.stringify({ email, password }),
    { headers: jsonHeaders }
  );
  if (registerRes.status !== 201) {
    throw new Error(`Setup başarısız: kayıt olunamadı (status=${registerRes.status}, body=${registerRes.body})`);
  }

  const loginRes = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({ email, password }),
    { headers: jsonHeaders }
  );
  if (loginRes.status !== 200) {
    throw new Error(`Setup başarısız: giriş yapılamadı (status=${loginRes.status})`);
  }
  const token = JSON.parse(loginRes.body).token;

  const orderRes = http.post(
    `${BASE_URL}/api/orders`,
    JSON.stringify({ origin: 'Istanbul', destination: 'Konya', weight: 3 }),
    { headers: { ...jsonHeaders, Authorization: `Bearer ${token}` } }
  );
  if (orderRes.status !== 201) {
    throw new Error(`Setup başarısız: sipariş oluşturulamadı (status=${orderRes.status}, body=${orderRes.body})`);
  }

  return { token, trackingNumber: JSON.parse(orderRes.body).trackingNumber };
}

// default() her VU için tekrar tekrar çalışan asıl senaryo.
export default function (data) {
  const res = http.get(`${BASE_URL}/api/shipments/${data.trackingNumber}`, {
    headers: { Authorization: `Bearer ${data.token}` },
  });

  check(res, {
    'status 200': (r) => r.status === 200,
  });

  sleep(1); // gerçek bir kullanıcının art arda tıklamak yerine biraz beklemesini simüle eder
}
