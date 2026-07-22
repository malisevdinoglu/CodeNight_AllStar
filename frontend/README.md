# CampaignCell Frontend

React 18 + Vite + TypeScript tabanli CampaignCell frontend uygulamasi.

## Sozlesmeler

- Container ic portu `80` olmali; docker compose tarafinda `3000:80` ile yayinlanir.
- Frontend API isteklerini gateway uzerinden yapar: `http://localhost:8080/api/v1/...`.
- Backend hazir degilken MSW mock agi kullanilir.

## Ortam Degiskenleri

```env
VITE_API_URL=http://localhost:8080/api/v1
VITE_API_BASE_URL=http://localhost:8080/api/v1
VITE_USE_MOCKS=true
```

`VITE_API_URL` Osman frontend tarafinda kullanilan ana addir. `VITE_API_BASE_URL`,
master tarafindan gelen sozlesme adini korumak icin fallback olarak desteklenir.

## Komutlar

```bash
npm install
npm run dev
npm run build
npm run lint
```
