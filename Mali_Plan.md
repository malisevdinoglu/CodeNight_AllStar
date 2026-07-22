# Mali — Uygulama Planı

> Kaynak: `Mali.md` (görev listesi) + `Core_Principles.md` (anayasa — tüm sözleşmeler buradan).
> Çalışma modeli: Her faz sonunda checkpoint → kısa özet + önerilen commit mesajları → onayınla sonraki faza geçiş.
> Commit'ler Mali tarafından atılır; her checkpoint'te Conventional Commits formatında hazır mesajlar verilir.
> Ekip bağımlılıkları: İskender/Osman parçaları için **sözleşmeye birebir uyan geçici stub'lar** yazılır; gerçek parçalar geldiğinde dosya değişimiyle oturur, mimari değişiklik gerekmez.

---

## Faz 1 — Monorepo İskeleti + Docker Compose (kritik yol, ekipteki blokajı kaldırır)

**Yapılacaklar**
- §3 klasör yapısı: `gateway/`, `services/{identity,campaign,gamification}` her biri Api/Application/Domain/Infrastructure + UnitTests, `services/ai/`, `frontend/` (Osman için placeholder), `docs/`, `.github/workflows/`.
- Her .NET servisi: .NET 8, Clean Architecture referans yönü (Api → Application → Domain; Infrastructure → Application interface'leri), solution dosyaları.
- `docker-compose.yml`: §9 port sözleşmesine birebir — gateway 8080, frontend 3000, API'ler 5001-5004, DB'ler 5433-5436, RabbitMQ 5672/15672, Redis 6379. Her DB ayrı volume + ayrı kullanıcı/şifre (`.env` + `.env.example`, secrets koda gömülmez).
- Dockerfile'lar (multi-stage sdk→runtime; AI için python:3.12-slim). Entrypoint'te programatik migration: `app.Services.Migrate()` + seed çağrısı (İskender'in seed'i için interface + geçici minimal seed stub).
- `GET /health` her serviste; compose `healthcheck` + `depends_on: service_healthy` zinciri: db → api → gateway.
- Frontend stub: Osman'ın yapısına dokunmayan, port 3000'de ayakta duran minimal container.

**Çıktı:** `docker compose up` → boş ama tamamen ayakta sistem. Osman ve İskender bağımsız çalışmaya başlar.

## Faz 2 — Gateway (YARP)

- Route tablosu §9'a birebir; `/hubs/**` WebSocket proxy (SignalR).
- JWT middleware: imza + süre; geçerliyse `X-User-Id`, `X-User-Role`, `X-User-Expertise` ekle; client'tan gelen aynı header'ları sil (anti-spoofing).
- Rate limiting: global IP 60/dk; `/auth/login` ve `/auth/otp/verify` 5/dk → 429.
- CORS: yalnız `http://localhost:3000`. Anonim path listesi: register, login, otp/*, refresh, health, swagger.

## Faz 3 — BuildingBlocks (shared class library, proje referansı)

- `ApiResponse<T>` + `ApiResponseFactory` (§5 zarfı, hata kodu formatı `{SERVIS}_{HTTP}_{SEBEP}`).
- MediatR `ValidationBehavior` (FluentValidation → 400) + `LoggingBehavior` (Serilog).
- `GlobalExceptionMiddleware`: `DomainException` → 422/409; diğerleri → 500, stack trace sızdırmaz.
- `IntegrationEvent` zarfı (event_id, event_type, timestamp, version, payload — snake_case) + MassTransit config: `UseRawJsonSerializer()`, topic exchange `campaigncell.events`, routing key = event_type. RabbitMQ UI'dan mesaj formatı gözle doğrulanır (Python interop sigortası).

## Faz 4 — Identity Service

- Domain: `User` (rol, expertise[], region, failedLoginCount, lockedUntil), `RefreshToken` (SHA-256 hash saklama), `AuditLog`.
- Handler'lar: Register (GSM 5xxxxxxxxx + OTP), VerifyOtp (sabit "1234" → aktifleştir + token çifti), Login (5 hata → 15 dk kilit → 423 + `remainingSeconds`, her deneme audit), RefreshToken (**rotation + theft koruması**: revoke edilmiş token → kullanıcının TÜM token'ları revoke + audit), Logout, CreateStaff (ADMIN), GetMe, GetExperts (internal, `X-Internal-Api-Key`), GetAuditLogs (ADMIN, sayfalı).
- Şifre politikası: FluentValidation, kural başına ayrı Türkçe mesaj. BCrypt work factor 11. JWT payload §6 sözleşmesi birebir.
- Unit testler: token rotation + theft senaryosu (jüri canlı deneyecek), şifre politikası, kilitleme.

## Faz 5 — Campaign Service (case'in gövdesi)

- Domain: `Campaign`, `OptimizationCase`, `CaseStatusHistory`, `Offer`, `OfferRating`; `SubscriberProfile` İskender'in — repository interface'ine karşı yazılır, geçici EF konfigürasyonu stub olarak eklenir.
- `CreateCampaignCommand`: numara Factory (DB sequence → `CMP-2026-000123`) → segment aboneleri → AI çağrısı (3 sn timeout) → skor ≥ 0.60 Offer, > 0.80 `isPriority` → **AI çökerse kampanya yine oluşur**: `BELIRSIZ` + `ORTA` + manuel kuyruk + Serilog warning (demo adım 7 sigortası) → Outbox ile `campaign.created` + `case.created` → düşük dönüşümde `AssignExpertCommand` → `/ai/assign` → `case.assigned`; kapasite yoksa YENI kalır (bekleyen kuyruk).
- State machine: §7 tablosu `CaseStateMachine` static tablo `(from, to) → (roles, guard)`; tek giriş `ChangeCaseStatusCommand`; kural dışı → 422 `CMP_422_INVALID_TRANSITION`, rol dışı → 403 + audit. `TAMAMLANDI`: expertNote zorunlu, `conversion_lift` hesabı, `campaign.optimized` (payload §8'e birebir). Her geçiş `case_status_history`'ye.
- Diğerleri: OverrideSegment (`segment.overridden`), ChangePriority + ManualAssign (SUPERVIZOR), RespondToOffer (**IDOR**: `offer.subscriberId == token.sub`), RateOffer (tek seferlik → 409; `offer.rated`), GetDashboardSummary (SUPERVIZOR).
- SLA Worker (dakikalık BackgroundService): deadline aşımı → işaretle + `case.sla_breached`; response'larda `remainingSlaSeconds`.
- Unit test: state machine tüm geçersiz kombinasyonlar; integration test: CreateCampaign → AI mock → Offer üretimi.

## Faz 6 — AI Service (FastAPI)

- Başlangıçta **kural tabanlı deterministik fallback** ile endpoint'ler ayakta (İskender'in `generate_data.py` verisi beklenirken); veri gecikirse geçici sentetik CSV opsiyonu hazır. Veri gelince `ml/train.py`: RandomForest (segment) + GradientBoosting (kabul olasılığı) → `model.joblib` commit'lenir (compose build'inde eğitim YOK), metrikler → `docs/AI_APPROACH.md`.
- Endpoint'ler (ApiResponse zarfı): `/ai/recommend`, `/ai/classify`, `/ai/assign` (skor = uzmanlık*0.5 + boşluk*0.3 + performans*0.2, **Strategy pattern**), `/ai/metrics/accuracy` (kategori kırılımı, +3), `/health`.
- aio-pika consumer: `segment.overridden` → `classification_feedback`; `offer.responded` RET → skor düşürme katsayısı.
- ai-db (kendi Postgres'i): predictions, classification_feedback, model_metadata. Parametrik sorgular (SQLAlchemy).
- Demo hazırlığı: iki farklı profil → iki farklı skor örneği ("hardcoded değil" kanıtı).

## Faz 7 — Gamification Service

- Yazma tamamen event-driven, REST yalnız okuma (case şartı — Campaign'den Gamification'a REST yasak).
- Consumer'lar + puan kuralları §8: +10 / +5 / +15 / +15 / -5 / -3. `point_transactions` + idempotency (`event_id` unique index).
- `BadgeEvaluator` (İskender'in badge seed'iyle eşleşen kural interface'i, geçici stub kurallar), seviye eşikleri (Bronz/Gümüş/Altın/Platin).
- Redis sorted set liderlik (daily/weekly, `ZINCRBY`/`ZREVRANGE`); Postgres source of truth, Redis türetilmiş.
- Query'ler: GetLeaderboard, GetProfile. SignalR `GameHub` (`/hubs/game`): `badge.earned` + `points.updated` push.
- Unit test: puan kuralları, badge koşulları, idempotency.

## Faz 8 — Güvenlik Sertleştirme (jüri simülasyonu)

§10 listesi tek tek test edilir: rol ihlali (403+audit), bozuk/expired JWT, revoke edilmiş refresh token, SQL injection girdileri, XSS girdileri, IDOR (ID değiştirme), brute-force (429/423). Bulunan açıklar aynı fazda kapatılır.

## Faz 9 — Test + CI + Dokümantasyon

- Eksik unit testler tamamlanır (atama skor formülü dahil); tümü yeşil.
- `.github/workflows/ci.yml`: dotnet build+test, pytest, frontend build.
- Swagger her .NET serviste, FastAPI `/docs`. `EVENTS.md` §8'den üretilir. Ana README (mimari diyagram, kurulum, demo kullanıcıları) + servis README'leri + `docs/AI_APPROACH.md`.

## Faz 10 — Demo Provası

Case §11.3'ün 8 adımı kronometreyle en az 2 kez: compose up → kampanya → AI skor/segment → atama → optimizasyon → liderlik puanı → `docker stop campaigncell-ai-api` → BELIRSIZ ile devam kanıtı → servis dönünce kuyruk işleme → güvenlik soru hazırlığı. Prova senaryosu dokümanı yazılır.

---

## Kesitler (her fazda geçerli)

- **Definition of Done (§12):** çalışan kod + FluentValidation + ≥1 anlamlı unit test + Swagger + response zarfı + yetki matrisi + event dokümantasyonu.
- **İsimlendirme (§4):** REST JSON camelCase, event JSON snake_case, DB snake_case çoğul, enum'lar Türkçe UPPER_SNAKE aynen (çeviri yasak).
- **Checkpoint çıktısı:** değişen dosya listesi + doğrulama sonucu (build/test) + hazır commit mesajları (Conventional Commits, feature branch önerisiyle).
- **Doğrulama notu:** .NET build/test'leri sandbox'ta çalıştırıp doğrularım; `docker compose up` uçtan uca doğrulaması senin makinede yapılır, her fazda net talimat veririm.

## Bağımlılık ve teslim sırası

Faz 1 → 2 → 3 → 4 → 5 → 6 → 7 sıralı (Mali.md §0 kritik yolu). Faz 6, İskender'in verisi gelmeden fallback ile tamamlanabilir; model eğitimi veri gelince yapılır. Faz 8-10 tüm servisler ayaktayken.
