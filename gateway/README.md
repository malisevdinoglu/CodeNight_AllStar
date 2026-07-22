# API Gateway (YARP)

Sistemin tek dış kapısı (`http://localhost:8080`). Frontend ve dış istemciler yalnız buradan geçer;
servis portlarına (5001-5004) doğrudan istek yasak (Core_Principles §9).

## Sorumluluklar (Faz 2 — tamamlandı)

- **JWT doğrulama**: `Jwt:Secret`/`Jwt:Issuer`/`Jwt:Audience` ile imza + süre kontrolü (Core_Principles §6 payload sözleşmesi: `sub`, `role`, `expertise`, `region`, `jti`, `exp`).
- **Header enjeksiyonu**: geçerli token varsa `X-User-Id`, `X-User-Role`, `X-User-Expertise` downstream'e eklenir; client'tan gelen aynı isimli header'lar her istekte silinir (anti-spoofing).
- **Rate limiting**: global IP başına 60/dk; `POST /api/v1/auth/login` ve `POST /api/v1/auth/otp/verify` için ayrıca 5/dk → aşımda 429 (`GATEWAY_429_RATE_LIMIT_EXCEEDED`, ApiResponse zarfında).
- **CORS**: sadece `http://localhost:3000`.
- **Route tablosu**: `appsettings.json` → `ReverseProxy:Routes/Clusters`, Core_Principles §9'a birebir (identity/campaign/ai/gamification + `/hubs/**`).
- **Anonim path'ler**: `/auth/register`, `/auth/login`, `/auth/otp/*`, `/auth/refresh`, `/health`. Bunların dışındaki her route `authenticated` politikası ister (`/auth/logout` dahil — bilinçli karar, aşağıya bakın).

## Tasarım notları / ekip için açık noktalar

- **JWT sırrı**: `Jwt__Secret` env değişkeni root `.env`'deki `JWT_SECRET` ile besleniyor; **Identity servisi (Faz 4) token imzalarken aynı sırrı, aynı `Issuer` (`campaigncell-identity`) ve `Audience` (`campaigncell`) değerlerini kullanmalı** — aksi halde Gateway 401 döner. Değerler `appsettings.json`'da sabit, tek gizli alan `Secret`.
- **`/api/v1/auth/logout` neden `authenticated`?** Core_Principles §2 anonim listesinde yok; kullanıcının kendi refresh token'ını iptal etmesi için zaten geçerli bir access token'a sahip olması gerekir. Ekip bu varsayımla mutabıksa değişiklik gerekmez.
- **`/api/v1/ai/**` gateway üzerinden `authenticated`**: AI servisinin Campaign tarafından yapılan iç çağrıları (recommend/classify/assign) gateway'den geçmez, doğrudan Docker network'ü üzerinden servis-servis gider (`X-Internal-Api-Key` ile, Core_Principles §6). Gateway route'u yalnızca frontend'in `/ai/metrics/accuracy` gibi okuma uçlarına erişimi için var.
- **Rate limiting IP anahtarı**: `HttpContext.Connection.RemoteIpAddress` kullanılıyor. Tek makineden çalışan demo/jüri ortamında bu genelde tek bir Docker network IP'sine düşer (tüm istekler aynı "kova"yı paylaşır) — bu, brute-force testi senaryosu için yeterli ve beklenen davranış; gerçek çoklu-istemci prod ortamı için `X-Forwarded-For` + trusted proxy config gerekir (kapsam dışı).
- Gateway'in kendi `GlobalExceptionMiddleware`'i yok (bu, BuildingBlocks'un parçası — Faz 3). 401/403/429 gövdeleri şimdilik doğrudan middleware içinde ApiResponse formatında üretiliyor.
