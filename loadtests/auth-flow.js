import http from 'k6/http';
import { check } from 'k6';

export const options = { vus: 5, duration: '30s' };
const baseUrl = __ENV.BASE_URL || 'http://localhost:5000';

export default function () {
  const email = __ENV.AUTH_EMAIL;
  const password = __ENV.AUTH_PASSWORD;
  const login = http.post(`${baseUrl}/api/v1/auth/login`, JSON.stringify({ email, password }), {
    headers: { 'Content-Type': 'application/json' },
  });
  check(login, { 'login succeeds or is rate-limited': r => r.status === 200 || r.status === 429 });
  if (login.status === 200) {
    const refresh = http.post(`${baseUrl}/api/v1/auth/refresh`);
    check(refresh, { 'refresh succeeds': r => r.status === 200 });
  }
}
