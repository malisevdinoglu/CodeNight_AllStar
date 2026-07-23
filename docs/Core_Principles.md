# CampaignCell — Core Principles & Contracts

> **Bu dosya projenin anayasasıdır.** Tüm ekip üyeleri (Mali, İskender, Osman) buradaki sözleşmelere harfiyen uyar.
> Bir sözleşmeyi değiştirmek isteyen, önce ekibe bildirir ve bu dosyayı günceller. Sözleşme dışı kod merge edilmez.

---

## 1. Kesinleşen Stack

| Katman | Teknoloji | Gerekçe (jüri savunması) |
|---|---|---|
| Backend (Identity, Campaign, Gamification) | .NET 8, ASP.NET Core Minimal API + Controllers | Hafif container, hızlı cold start, olgun ekosistem |
| AI Service | Python 3.12 + FastAPI + scikit-learn | Kendi eğitilmiş model (+8 bonus), polyglot mikroservis kanıtı |
| API Gateway | YARP (.NET 8) | Middleware ile tam kontrollü rate limiting + JWT doğrulama |
| Event Bus | RabbitMQ + MassTransit (+5 bonus) | Retry, DLQ, kalıcı mesaj → servis kapatma demosunda mesaj kaybolmaz |
| Veritabanları | PostgreSQL × 4 (servis başına ayrı container) + Redis (Gamification liderlik) | Database-per-service **fiziksel** ayrım — diskalifiye riskine sıfır tolerans |
| ORM | EF Core 8 + `UseSnakeCaseNamingConvention` | Migration yönetimi, İskender'in alanı |
| Frontend | React 18 + Vite + TypeScript | Hızlı geliştirme, SPA yeterli |
| State | TanStack Query (server state) + Zustand (client state) | Loading/error/cache bedava → UI/UX puanı |
| UI | Tailwind CSS + Recharts + react-hot-toast | Dashboard grafikleri + rozet toast bildirimi |
| Real-time | SignalR (Gamification hub) (+2 bonus) | Rozet toast + canlı liderlik |
| Validasyon | FluentValidation (BE) + zod (FE) | Net hata mesajları (şifre politikası şartı) |
| Loglama | Serilog (structured, JSON) | Audit + debugging |
| Test | xUnit + FluentAssertions + Moq / pytest | Test puanı (10p) |
| CI/CD | GitHub Actions (+2 bonus) | build + test her push'ta |
| API Doc | Swashbuckle / FastAPI otomatik OpenAPI | Zorunlu teslimat |

**Bonus hedefi: 8+5+3+2+2 = tam 20 puan.**

---

## 2. Mimari İlkeler

1. **Database-per-service:** Her servis SADECE kendi DB'sine bağlanır. Başka servisin verisi gerekiyorsa REST çağrısı veya event ile alınır. Connection string'ler asla paylaşılmaz.
2. **Komut = REST, Gerçek = Event:** Cevabı hemen gereken çağrılar (AI skorlama) senkron REST; olan biten her şey (kampanya oluştu, vaka tamamlandı) asenkron event. Gamification'a **asla doğrudan REST çağrısı yapılmaz** — sadece event dinler (case şartı).
3. **Graceful degradation:** AI Service erişilemezse Campaign Service kampanyayı yine oluşturur → `segment: BELIRSIZ`, `priority: ORTA`, manuel kuyruk. Her servis-arası REST çağrısı timeout (3 sn) + try/catch + fallback içerir. **Bu, demonun 7. adımının sigortasıdır.**
4. **Clean Architecture (her .NET serviste):**
   - `Domain` → Entity'ler, enum'lar, domain kuralları. Hiçbir şeye bağımlı değil.
   - `Application` → CQRS Command/Query + Handler (MediatR), Interface'ler, DTO'lar, Validator'lar.
   - `Infrastructure` → EF Core DbContext, Repository implementasyonları, MassTransit, dış servisler.
   - `Api` → Controller'lar, middleware, DI kayıtları. İnce katman: sadece HTTP ↔ MediatR çevirisi.
   - **Bağımlılık yönü daima içe doğru:** Api → Application → Domain. Infrastructure, Application'daki interface'leri implemente eder.
5. **Kullanılan pattern'lar (jüriye anlatılacak liste):**
   - **CQRS + Mediator** (MediatR) — use-case başına handler
   - **Repository + Unit of Work** — EF Core üstünde soyutlama, test edilebilirlik
   - **Table-driven State Machine** — vaka yaşam döngüsü (bkz. §7)
   - **Strategy** — uzman atama skorlama formülü (AI Service'te değiştirilebilir strateji)
   - **Factory** — kampanya/vaka numarası üretimi (`CMP-2026-000123`)
   - **Outbox** — MassTransit transactional outbox: DB commit + event publish atomik
   - **Pipeline Behavior** (MediatR) — validasyon ve logging cross-cutting

---

## 3. Monorepo Klasör Yapısı

```
CodeNight_AllStar/
├─ docker-compose.yml            # TÜM sistem: 4 servis + 4 DB + Redis + RabbitMQ + gateway + frontend
├─ README.md                     # Ana: mimari diyagram, kurulum, demo kullanıcıları
├─ EVENTS.md                     # §8'deki event kataloğunun kopyası
├─ docs/AI_APPROACH.md           # Model, eğitim verisi, süreç (+8 bonus şartı)
├─ .github/workflows/ci.yml
├─ gateway/                      # YARP projesi + README + .env.example
├─ services/
│  ├─ identity/
│  │  ├─ src/Identity.Api / Identity.Application / Identity.Domain / Identity.Infrastructure
│  │  ├─ tests/Identity.UnitTests
│  │  ├─ README.md  +  .env.example  +  Dockerfile
│  ├─ campaign/                  # aynı yapı (Campaign.*)
│  ├─ gamification/              # aynı yapı (Gamification.*)
│  └─ ai/
│     ├─ app/ (main.py, routers/, services/, models/)
│     ├─ ml/ (train.py, generate_data.py, model.joblib)
│     ├─ data/training_data.csv  # repoda paylaşılacak (+8 şartı)
│     ├─ tests/  +  README.md  +  .env.example  +  Dockerfile
└─ frontend/
   ├─ src/ (bkz. Osman.md)
   ├─ README.md  +  .env.example  +  Dockerfile
```

---

## 4. İsimlendirme Standartları

| Bağlam | Standart | Örnek |
|---|---|---|
| C# sınıf/metot/property | PascalCase | `CreateCampaignCommand`, `SlaDeadline` |
| C# interface | `I` + PascalCase | `ICampaignRepository` |
| C# local/parametre | camelCase | `expertId` |
| Python | snake_case (PEP8) | `predict_conversion` |
| TS component | PascalCase | `LeaderboardTable.tsx` |
| TS hook/fonksiyon | camelCase | `useCases.ts` |
| **REST JSON alanları** | **camelCase** | `"campaignNumber"` |
| **Event JSON alanları** | **snake_case** (case dokümanındaki örnekle uyum) | `"case_id"` |
| DB tablo/kolon | snake_case, çoğul tablolar | `optimization_cases.sla_deadline` |
| REST path | kebab-case, çoğul kaynak | `/api/v1/audit-logs` |
| Enum değerleri | Case dokümanındaki Türkçe UPPER_SNAKE **aynen** | `RISKLI_KAYIP`, `EK_PAKET`, `OPTIMIZE_EDILIYOR` |
| Env değişkenleri | UPPER_SNAKE | `RABBITMQ_HOST` |

> Enum'ları İngilizceye çevirmek YASAK — case dokümanı, DB, API, UI aynı kelimeyi kullanır. Çeviri katmanı = bug kaynağı.

---

## 5. Standart API Response Zarfı

Her endpoint (dört serviste de) şunu döner:

```json
// Başarı
{ "success": true, "data": { ... }, "error": null }

// Hata
{ "success": false, "data": null,
  "error": { "code": "CMP_422_INVALID_TRANSITION",
             "message": "TAMAMLANDI durumundan ATANDI durumuna geçilemez.",
             "details": ["..."] } }
```

**HTTP kodları:** 200 OK, 201 Created, 400 validasyon, 401 kimliksiz, 403 yetkisiz (**audit log'a yazılır**), 404, 409 çakışma (örn. ikinci kez puanlama), 422 kural dışı state geçişi, 423 hesap kilitli, 429 rate limit.

**Hata kodu formatı:** `{SERVIS}_{HTTP}_{SEBEP}` → `AUTH_423_ACCOUNT_LOCKED`, `AUTH_400_PASSWORD_POLICY`, `CMP_422_INVALID_TRANSITION`, `AI_503_UNAVAILABLE`.

**Sayfalama:** `?page=1&pageSize=20` → `data: { items: [...], page, pageSize, totalCount }`.

---

## 6. Kimlik Sözleşmesi (JWT)

```json
// Access token payload (15 dk geçerli)
{
  "sub": "a7f3c2e1-...",            // user_id (uuid)
  "role": "PERSONEL",               // MUSTERI | PERSONEL | SUPERVIZOR | ADMIN
  "expertise": ["RISKLI_KAYIP", "YUKSEK_DEGER"],  // sadece personel
  "region": "MARMARA",
  "jti": "...", "exp": 1753190000
}
```

- Refresh token: 7 gün, DB'de **hash'lenmiş** saklanır, rotation zorunlu (bkz. Mali.md §Token).
- Gateway JWT imzasını doğrular ve `X-User-Id`, `X-User-Role` header'larını downstream'e ekler; servisler ayrıca kendi `[Authorize]` politikalarını uygular (defense in depth).
- Servisler arası sistem çağrıları: `X-Internal-Api-Key` header (env'den, compose network'ünde).

**Yetki matrisi (case §3.3) endpoint policy'lerine birebir uygulanır. Her 403 → audit log.**

---

## 7. Vaka State Machine (tek doğru kaynak)

```
YENI → ATANDI                    (Sistem/AI veya SUPERVIZOR; koşul: uzman belirlendi)
ATANDI → OPTIMIZE_EDILIYOR       (atanan PERSONEL)
OPTIMIZE_EDILIYOR → TEST_EDILIYOR (atanan PERSONEL; A/B testi başlatıldı)
TEST_EDILIYOR → OPTIMIZE_EDILIYOR (Sistem; test sonuçlandı)
OPTIMIZE_EDILIYOR → TAMAMLANDI   (atanan PERSONEL; expertNote ZORUNLU)
TAMAMLANDI → YAYINDA             (SUPERVIZOR; onay)
YAYINDA → ARSIVLENDI             (Sistem; geçerlilik doldu)
```

Bu tablo dışındaki her geçiş → **422** + `CMP_422_INVALID_TRANSITION`. Rol uymuyorsa → **403** + audit log.
Her geçiş `case_status_history` tablosuna yazılır. SLA sayacı `TAMAMLANDI`'da durur.

**Öncelik/SLA:** KRITIK 2s (kırmızı), YUKSEK 8s (turuncu), ORTA 24s, DUSUK 72s. `RISKLI_KAYIP` segment → minimum `YUKSEK`.

---

## 8. Event Kataloğu (EVENTS.md'nin kaynağı)

**Altyapı:** RabbitMQ topic exchange `campaigncell.events`. Routing key = `event_type`. MassTransit `RawJsonSerializer` ile yayınlar → Python tarafı sade JSON okur (interop kritik!).

**Ortak zarf:**
```json
{ "event_id": "uuid", "event_type": "campaign.optimized",
  "timestamp": "2026-07-18T14:22:10Z", "version": 1, "payload": { ... } }
```

| Event | Yayıncı | Dinleyen | Payload alanları |
|---|---|---|---|
| `campaign.created` | Campaign | (audit/loose coupling) | campaign_id, campaign_number, type, target_segment, created_by |
| `case.created` | Campaign | — | case_id, campaign_id, segment, priority, sla_deadline |
| `case.assigned` | Campaign | Gamification (sayaç) | case_id, expert_id, assigned_by ("AI"\|"MANUEL") |
| `case.status_changed` | Campaign | — | case_id, from_status, to_status, changed_by |
| `campaign.optimized` | Campaign | **Gamification** | case_id, expert_id, segment, priority, conversion_lift, created_at, completed_at *(case §9.2 örneğiyle birebir)* |
| `case.sla_breached` | Campaign | **Gamification** (-5) | case_id, expert_id, priority, breached_at |
| `segment.overridden` | Campaign | **AI** (doğruluk metriği) | case_id, predicted_segment, corrected_segment, changed_by |
| `offer.responded` | Campaign | **AI** (skor ayarı) | offer_id, subscriber_id, campaign_id, campaign_type, response ("KABUL"\|"RET") |
| `offer.rated` | Campaign | **Gamification** (1-2★ → -3) | offer_id, subscriber_id, expert_id, campaign_id, stars |
| `badge.earned` | Gamification | (SignalR push) | expert_id, badge_code, badge_name |
| `points.updated` | Gamification | (SignalR push) | expert_id, delta, total_points, reason |

**Puan kuralları (Gamification consumer'ları):** TAMAMLANDI +10; süre < 2s ise +5; conversion_lift > hedef ise +15; KRITIK & SLA içinde +15; SLA aşımı -5; 1-2★ -3.

---

## 9. Portlar ve Compose Sözleşmesi

| Container | İç Port | Host Port |
|---|---|---|
| gateway | 8080 | **8080** (tek dış kapı) |
| frontend | 80 | 3000 |
| identity-api / campaign-api / ai-api / gamification-api | 8080 / 8080 / 8000 / 8080 | 5001 / 5002 / 5003 / 5004 (debug için) |
| identity-db / campaign-db / ai-db / gamification-db | 5432 | 5433 / 5434 / 5435 / 5436 |
| rabbitmq | 5672 | 5672, 15672 (yönetim UI) |
| redis | 6379 | 6379 |

- Frontend **sadece** `http://localhost:8080/api/v1/...` üzerinden konuşur. Servis portlarına doğrudan istek YASAK.
- Gateway routing: `/api/v1/auth/**`, `/api/v1/users/**`, `/api/v1/audit-logs/**` → identity; `/api/v1/campaigns/**`, `/api/v1/cases/**`, `/api/v1/subscribers/**`, `/api/v1/offers/**`, `/api/v1/dashboard/**` → campaign; `/api/v1/ai/**` → ai; `/api/v1/game/**`, `/hubs/**` (WebSocket) → gamification.
- Her servis `GET /health` endpoint'i sunar; compose `healthcheck` + `depends_on: condition: service_healthy` kullanır.
- `docker compose up` → migration'lar + seed otomatik çalışır (entrypoint'te). **Tek komut şartı.**

---

## 10. Güvenlik Kontrol Listesi (jüri canlı test edecek!)

- [ ] SQL injection: her yerde EF Core parametrik sorgu / FastAPI'de SQLAlchemy — asla string birleştirme
- [ ] XSS: React zaten escape eder; `dangerouslySetInnerHTML` YASAK; API'de içerik sanitize
- [ ] IDOR: her "kendi kaydı" endpoint'inde `resource.ownerId == token.sub` kontrolü (sadece ID ile getirme YASAK)
- [ ] Token: süre dolmuş/imza bozuk JWT → 401; müşteri token'ı ile süpervizör endpoint'i → 403 + audit
- [ ] Refresh token rotation + theft koruması: geçersiz kılınmış token tekrar kullanılırsa kullanıcının TÜM oturumları kapatılır
- [ ] Rate limiting: gateway'de IP başına dakikada 60 istek; `/auth/login` için 5/dk (brute-force testi)
- [ ] Hesap kilitleme: 5 hatalı giriş → 15 dk kilit, kalan süre response'ta
- [ ] Şifre: bcrypt (work factor 11); politika ihlalinde HANGİ kuralın ihlal edildiği mesajda
- [ ] Secrets: koda gömme YASAK — `.env` + `.env.example`

---

## 11. Git Standartları

- Branch: `main` (daima çalışır) ← `feature/<alan>-<kısa-ad>` (örn. `feature/campaign-state-machine`)
- **Conventional Commits** (jüri "anlamlı commit geçmişi"ni puanlıyor): `feat(campaign): vaka state machine ve 422 kuralları`, `fix(identity): kilitli hesapta kalan süre hesabı`, `test(ai): segment sınıflandırma birim testleri`
- Küçük ve sık commit; "wip", "fix2", "son" gibi mesajlar YASAK.
- Merge önce: build geçer + testler yeşil + bu dosyaya uygunluk.

## 12. Definition of Done

Bir özellik şunlar olmadan bitmiş sayılmaz: çalışan kod + FluentValidation kuralları + en az 1 anlamlı unit test + Swagger'da görünür + response zarfına uygun + yetki matrisi uygulanmış + (varsa) event'i EVENTS.md'de dokümante.
