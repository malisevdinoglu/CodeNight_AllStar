# Mali — Tech Lead / Backend / Mimari / Altyapı Yol Haritası

> Önce `Core_Principles.md` oku. Bu dosya senin adım adım görev listen.
> Sorumluluk: Gateway, Docker Compose, RabbitMQ/MassTransit, üç .NET servisinin Application katmanı, AI Service (FastAPI), güvenlik.

---

## 0. Öncelik Sırası (kritik yol)

1. **İskelet + Compose** (diğer ikisini bloklamaz hale getir) → 2. Identity (auth olmadan kimse ilerleyemez) → 3. Campaign (case'in gövdesi) → 4. AI → 5. Gamification → 6. Güvenlik sertleştirme → 7. CI + testler.

---

## 1. Adım: Monorepo İskeleti + Docker Compose (İLK İŞ)

- [ ] `Core_Principles.md` §3'teki klasör yapısını aç, her .NET servisi için 4 proje + test projesi oluştur (`dotnet new`), solution'lara bağla.
- [ ] `docker-compose.yml`: 4 API + 4 Postgres + Redis + RabbitMQ + gateway + frontend. §9'daki port sözleşmesine birebir uy. Her DB'ye ayrı volume + ayrı kullanıcı/şifre.
- [ ] Her servise Dockerfile (multi-stage: sdk → runtime). Entrypoint'te `dotnet ef database update` yerine **programatik migration**: `app.Services.Migrate()` + seed (İskender'in seed'i çağrılır). Sebep: tek komutta ayağa kalkma şartı.
- [ ] `healthcheck` + `depends_on: service_healthy` zinciri: db → api → gateway.
- [ ] Bu adım bittiğinde commit at: ekip `docker compose up` ile boş ama ayakta bir sistem görmeli. **Osman ve İskender bu andan itibaren bağımsız çalışır.**

## 2. Adım: Gateway (YARP)

- [ ] Route tablosu: Core_Principles §9. `/hubs/**` için WebSocket proxy açık olmalı (SignalR).
- [ ] JWT doğrulama middleware: imza + süre kontrolü; geçerliyse `X-User-Id`, `X-User-Role`, `X-User-Expertise` header'larını ekle, client'tan gelen aynı isimli header'ları **sil** (spoofing önlemi).
- [ ] Rate limiting (`Microsoft.AspNetCore.RateLimiting`): global IP başına 60/dk; `/api/v1/auth/login` ve `/auth/otp/verify` için 5/dk → 429. (Jüri brute-force deneyecek.)
- [ ] CORS: sadece `http://localhost:3000`.
- [ ] Anonim izinli path listesi: `/auth/register`, `/auth/login`, `/auth/otp/*`, `/auth/refresh`, `/health`, `/swagger`.

## 3. Adım: Ortak Yapı Taşları (BuildingBlocks)

Üç serviste tekrar edecek kodu `services/shared/BuildingBlocks` class library'sine koy (NuGet değil, proje referansı):
- [ ] `ApiResponse<T>` zarfı + `ApiResponseFactory`
- [ ] MediatR `ValidationBehavior<,>` (FluentValidation'ı pipeline'da çalıştırır → 400) ve `LoggingBehavior<,>`
- [ ] `GlobalExceptionMiddleware`: `DomainException` → 422/409, geri kalan → 500 (stack trace sızdırma!)
- [ ] Event zarf modeli (`IntegrationEvent`: event_id, event_type, timestamp, version, payload) + MassTransit config extension: **`UseRawJsonSerializer()`**, exchange `campaigncell.events`, routing key = event_type. *Python interop bunun üstünde duruyor — ilk gün RabbitMQ UI'dan mesaj formatını gözünle doğrula.*

## 4. Adım: Identity Service

**Domain:** `User` (rol, expertise list, region, failedLoginCount, lockedUntil), `RefreshToken`, `AuditLog`.

**Command/Query listesi (her biri MediatR handler):**
- `RegisterSubscriberCommand` → GSM formatı doğrula (5xxxxxxxxx), OTP akışını başlat
- `VerifyOtpCommand` → sabit "1234" kabul (simülasyon), kullanıcıyı aktifleştir + token çifti döndür
- `LoginCommand` → e-posta+şifre; başarısızsa sayaç artır; 5'te kilitle (15 dk); kilitliyse **423** + `remainingSeconds`; her deneme audit log
- `RefreshTokenCommand` → **rotation**: eski token'ı revoke et, `replaced_by` zincirini yaz. Revoke edilmiş token gelirse → o kullanıcının TÜM refresh token'larını revoke et (theft koruması) + audit log. Bu akışa unit test yaz — jüri bunu canlı deneyecek.
- `LogoutCommand`, `CreateStaffCommand` (ADMIN; expertise[] + region zorunlu), `GetMeQuery`, `GetExpertsQuery` (internal, Campaign çağırır), `GetAuditLogsQuery` (ADMIN, sayfalı)

**Kurallar:**
- Şifre politikası FluentValidation ile; her kural ayrı mesaj: "En az 1 büyük harf içermelidir" vb. (case şartı: hangi kural ihlal edildi belli olacak)
- BCrypt.Net-Next, work factor 11. Refresh token'lar SHA-256 hash'lenerek saklanır.
- JWT üretimi: Core_Principles §6 payload sözleşmesi.

## 5. Adım: Campaign Service (case'in gövdesi)

**Domain:** `Campaign`, `OptimizationCase`, `CaseStatusHistory`, `Offer`, `OfferRating`, `SubscriberProfile` (İskender modelliyor — onun entity'lerini bekleme, interface'e karşı yaz).

**Kritik akış — `CreateCampaignCommand`:**
1. Kampanya kaydet (`CampaignNumberFactory` → DB sequence → `CMP-2026-000123`)
2. Hedef segmentteki aboneleri çek (kendi DB'sindeki `subscriber_profiles`)
3. **AI çağrısı** (`IAiServiceClient.RecommendAsync`, timeout 3 sn):
   - Başarılı → her abone için Offer üret (skor ≥ 0.60 olanlar; > 0.80 `isPriority: true`), vaka segment/priority ata
   - **Başarısız → kampanya YİNE oluşur:** segment `BELIRSIZ`, priority `ORTA`, `case.created` ile manuel kuyruğa. Catch bloğu + Serilog warning. **Demo adım 7 burada kazanılır.**
4. Outbox ile `campaign.created` + `case.created` yayınla
5. Düşük dönüşüm tahminli segment için `AssignExpertCommand` tetikle → AI `/ai/assign` → en yüksek skorlu uzmana ata → `case.assigned`; kapasite yoksa kuyruk (status YENI kalır, dashboard "bekleyen kuyruk"ta görünür)

**State machine:** `CaseStateMachine` static tablo: `(fromStatus, toStatus) → (allowedRoles, guard)`. `ChangeCaseStatusCommand` tek giriş noktası; kural dışı → `DomainException` → 422; rol dışı → 403 + audit event. `TAMAMLANDI`'ya geçişte `expertNote` zorunlu, `conversion_lift` hesapla (basit: teklif kabul oranındaki artış simülasyonu), `campaign.optimized` yayınla (payload case §9.2 ile birebir).

**Diğer command'lar:** `OverrideSegmentCommand` (→ `segment.overridden` event — AI doğruluğu bunu dinler), `ChangePriorityCommand` (SUPERVIZOR), `ManualAssignCommand` (SUPERVIZOR), `RespondToOfferCommand` (sahiplik kontrolü: `offer.subscriberId == token.sub` — IDOR testi!), `RateOfferCommand` (tek seferlik → ikinci deneme 409; `offer.rated` yayınla), `GetDashboardSummaryQuery` (SUPERVIZOR: segment dağılımı, dönüşüm trendi, SLA uyumu, bekleyen kuyruk).

**SLA Worker (`BackgroundService`, dakikada bir):** `sla_deadline < now && status not in (TAMAMLANDI, YAYINDA, ARSIVLENDI) && !sla_breached` → işaretle + `case.sla_breached` yayınla. Kalan SLA'yı API response'larında `remainingSlaSeconds` olarak döndür (uzman + yönetici ekranı şartı).

## 6. Adım: AI Service (FastAPI) — İskender'in ürettiği veriyle

- [ ] `ml/generate_data.py` (İskender yazar, sen entegre et) → `data/training_data.csv`
- [ ] `ml/train.py`: iki model — (1) RandomForestClassifier → segment (YUKSEK_DEGER/RISKLI_KAYIP/YENI_ABONE/PASIF), (2) GradientBoosting → kabul olasılığı (kampanya tipi + indirim + profil özellikleri). `model.joblib` + metrikler (accuracy, F1) → `docs/AI_APPROACH.md`'ye yaz. Eğitim compose build'inde DEĞİL, önceden çalıştırılıp model dosyası commit'lenir (demo riski sıfırlanır).
- [ ] Endpoint'ler (hepsi `ApiResponse` zarfıyla):
  - `POST /api/v1/ai/recommend` — girdi: `{subscriberProfile, campaigns[]}` → `[{campaignId, recommendationScore, conversionProbability}]`
  - `POST /api/v1/ai/classify` — profil → segment + confidence
  - `POST /api/v1/ai/assign` — `{case, candidates[]}` → skor = `uzmanlik_eslesme*0.5 + bosluk_orani*0.3 + performans*0.2` (**Strategy pattern**: formül sınıfı değiştirilebilir) → sıralı liste
  - `GET /api/v1/ai/metrics/accuracy` → genel + **kategori bazlı kırılım** (+3 bonus)
  - `GET /health`
- [ ] RabbitMQ consumer (aio-pika): `segment.overridden` → `classification_feedback` tablosuna yanlış sınıflandırma yaz; `offer.responded` → RET ise o abone+kampanya tipi için skor düşürme katsayısı güncelle (case §4.5 şartı).
- [ ] Kendi Postgres'i (ai-db): predictions, classification_feedback, model_metadata.
- [ ] **Hardcoded görünme riskine karşı:** demo sırasında iki farklı profile iki farklı skor çıktığını gösterecek örnek hazırla.

## 7. Adım: Gamification Service

- [ ] REST YOK (yalnız okuma endpoint'leri var); yazma tamamen **event-driven** (case şartı).
- [ ] MassTransit consumer'ları: `campaign.optimized` (+10; süre<2s +5; lift>hedef +15; KRITIK&SLA içinde +15), `case.sla_breached` (-5), `offer.rated` (1-2★ → ilgili uzmana -3). Her puan hareketi `point_transactions`'a; idempotency: `event_id` unique index (aynı event iki kez işlenmez).
- [ ] `BadgeEvaluator` (kurallar Iskender.md'deki badge seed'iyle eşleşir): her puan hareketinden sonra koşulları kontrol et → `expert_badges` + `badge.earned`.
- [ ] Seviye: toplam puandan hesaplanır (Bronz 0-499, Gümüş 500-1499, Altın 1500-2999, Platin 3000+).
- [ ] Redis sorted set: `leaderboard:daily:{yyyy-MM-dd}`, `leaderboard:weekly:{yyyy-'W'ww}` → `ZINCRBY`; okuma `ZREVRANGE 0 9`. Postgres source of truth, Redis türetilmiş (çökerse yeniden inşa edilebilir — jüriye anlat).
- [ ] Query'ler: `GetLeaderboardQuery(period)`, `GetProfileQuery(expertId)` (toplam puan, seviye, rozetler, sıralamalar, çözülen vaka, ortalama).
- [ ] **SignalR `GameHub`** (`/hubs/game`): `badge.earned` ve `points.updated` push → Osman toast + canlı tablo bağlar.

## 8. Adım: Güvenlik Sertleştirme Turu (demo öncesi yarım gün)

Core_Principles §10 listesini tek tek jüri gibi test et: Postman'de müşteri token'ıyla süpervizör endpoint'leri, bozuk/süresi dolmuş JWT, revoke edilmiş refresh token'ın tekrar kullanımı, `' OR 1=1 --` girdileri, `<script>` girdileri, ID değiştirerek IDOR, hızlı ardışık login.

## 9. Adım: Test + CI + Dokümantasyon

- [ ] Unit test öncelikleri: state machine geçiş tablosu (tüm geçersiz kombinasyonlar), token rotation + theft, şifre politikası, atama skor formülü, puan hesaplama kuralları, badge koşulları.
- [ ] En az 1 integration test: `CreateCampaign` → AI mock → Offer üretimi.
- [ ] `.github/workflows/ci.yml`: dotnet build+test, pytest, frontend build (+2 bonus).
- [ ] Swagger: her .NET serviste Swashbuckle (`/swagger`); FastAPI `/docs` hazır. EVENTS.md'yi Core_Principles §8'den üret. Ana README + servis README'leri + `docs/AI_APPROACH.md`.

## 10. Demo Provası (sunumdan önce en az 2 kez)

Case §11.3'ün 8 adımını sırayla, kronometreyle: compose up → kampanya oluştur → AI skor/segment göster → doğru uzmana atama → optimizasyonu tamamla → liderlik tablosunda puan → **`docker stop campaigncell-ai-api`** → kampanya oluşturmanın BELIRSIZ ile devam ettiğini göster → servis geri açılınca kuyruktaki mesajların işlendiğini göster (RabbitMQ'nun ekstra kozu) → güvenlik sorularına hazır ol.
