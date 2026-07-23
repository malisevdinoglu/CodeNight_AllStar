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
VITE_GAME_HUB_URL=http://localhost:8080/hubs/game
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

## Demo Hesaplari

Gercek backend seed verisi (bkz. kok `README.md` / `docs/SEED_DATA.md`) - asagidakiler
`VITE_USE_MOCKS=false` oldugunda (production build, `.env.production`) gecerlidir. Onceden
burada mock donemine ait sahte `.local` hesaplar ve `Password1` sifresi yaziyordu; bunlar
gercek backend'de calismiyordu (401), production build'de kafa karistiriyordu - kaldirildi.

| Rol | Giris |
|---|---|
| PERSONEL | `deniz.karaca@campaigncell.com` / `Uzman.2026!` (+3 diger uzman, bkz. kok README) |
| SUPERVIZOR | `supervizor@campaigncell.com` / `Super.2026!` |
| ADMIN | `admin@campaigncell.com` / `Admin.2026!` |
| MUSTERI | GSM `5321104501` (Ahmet Yilmaz), OTP `1234` - "Kod Gonder" adimi backend'de `POST /auth/otp/request` gerektirir |

## Rol Rotalari

| Rol | Ana rota | Kapsam |
|---|---|---|
| MUSTERI | `/offers` | Teklif listesi, detay, kabul/ret, yildiz puanlama |
| PERSONEL | `/cases` | Kampanya olusturma, vaka listesi, vaka detay/state akisi, oyun profili |
| SUPERVIZOR | `/dashboard` | Grafikler, AI dogruluk, SLA, manuel atama kuyrugu, tum vakalar |
| ADMIN | `/admin/staff` | Personel olusturma, gecici sifre, audit log |

## Mocktan Gercek API'ye Gecis

Backend hazir oldugunda `.env` icinde `VITE_USE_MOCKS=false` yapilir.
Endpoint farklari sayfalara yayilmadan `src/api/*.api.ts` dosyalarinda karsilanir.
