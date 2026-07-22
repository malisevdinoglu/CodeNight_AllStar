# CampaignCell

**Turkcell CodeNight 2026 — Final** · Yapay zekâ destekli, mikroservis mimarili kişiselleştirilmiş kampanya ve öneri platformu.

Turkcell abonelerine **doğru teklifi, doğru anda, doğru kişiye** sunar: AI abone profilini analiz eder,
en uygun kampanyayı önerir, dönüşüm olasılığını tahmin eder ve düşük dönüşümlü segmentleri uygun kampanya
uzmanına otomatik atar. Uzmanlar optimizasyon yaptıkça puan/rozet kazanır; yöneticiler tüm kampanya
performansını ve modelin isabetini tek ekrandan izler.

---

## Mimari

```
                          ┌───────────────┐
        Frontend  ───────▶│  API Gateway  │  :8080  (tek dış kapı)
        (React)           │     YARP      │  JWT doğrulama + rate limiting
                          └───────┬───────┘
              ┌───────────────┬───┴───────────┬────────────────┐
              ▼               ▼               ▼                ▼
      ┌──────────────┐ ┌─────────────┐ ┌───────────┐ ┌────────────────┐
      │   Identity   │ │  Campaign   │ │    AI     │ │  Gamification  │
      │   .NET 8     │ │   .NET 8    │ │  FastAPI  │ │    .NET 8      │
      └──────┬───────┘ └──────┬──────┘ └─────┬─────┘ └───────┬────────┘
             ▼                ▼              ▼               ▼
        identity-db      campaign-db       ai-db      gamification-db + Redis
         (Postgres)       (Postgres)     (Postgres)      (Postgres)

                    ┌───────────────────────────────┐
                    │ RabbitMQ · campaigncell.events│  ◀── asenkron event akışı
                    └───────────────────────────────┘
```

**Database-per-service:** Her servisin kendi PostgreSQL **container**'ı ve kendi volume'ü vardır.
Servisler arası doğrudan veritabanı erişimi yoktur; başka servisin kimliği yalnızca `uuid` kolon
olarak tutulur — **cross-service foreign key YOKTUR**.

| Servis | Sorumluluk | Teknoloji |
|---|---|---|
| **API Gateway** | Tek giriş noktası, routing, JWT doğrulama, rate limiting | YARP (.NET 8) |
| **Identity** | Kayıt/giriş (GSM+OTP, e-posta+şifre), JWT + refresh token rotation, rol/yetki matrisi, audit log, hesap kilitleme | .NET 8, EF Core |
| **Campaign** | Kampanya yaşam döngüsü, vaka state machine, SLA takibi, teklif ve puanlama | .NET 8, EF Core, MassTransit Outbox |
| **AI** | Öneri skorlama, segment sınıflandırma, akıllı uzman ataması, doğruluk takibi | Python 3.12, FastAPI, scikit-learn |
| **Gamification** | Puan, rozet, seviye, liderlik — **yalnızca event dinler** | .NET 8, EF Core, Redis, SignalR |

**İletişim kuralı:** Komut = REST (senkron), olan biten = Event (asenkron, RabbitMQ).
Event kataloğu ve payload sözleşmeleri: **[EVENTS.md](EVENTS.md)**.
AI yaklaşımı, eğitim verisi ve model süreci: **[docs/AI_APPROACH.md](docs/AI_APPROACH.md)**.

---

## Kurulum — tek komut

**Gereksinim:** Docker Desktop (Compose v2) ve ~10 GB boş disk.

```bash
git clone <repo-url> && cd CodeNight_AllStar
cp .env.example .env
docker compose up --build
```

Bu kadar. Compose ayağa kalkarken **veritabanı migration'ları ve demo seed verisi otomatik
çalışır** — ekstra komut gerekmez. Tüm servisler healthcheck'lidir; `depends_on` zinciri
başlatma sırasını kendiliğinden doğru kurar.

### Erişim adresleri

| Ne | Adres |
|---|---|
| **Uygulama (Frontend)** | http://localhost:3000 |
| **API Gateway** (tek dış kapı) | http://localhost:8080 |
| RabbitMQ yönetim UI | http://localhost:15672 |
| Identity / Campaign / AI / Gamification (debug) | :5001 / :5002 / :5003 / :5004 |
| PostgreSQL — identity / campaign / ai / gamification | :5433 / :5434 / :5435 / :5436 |

> Frontend **yalnızca** Gateway (`:8080`) üzerinden konuşur; servis portlarına doğrudan istek
> atılmaz. Servis portları sadece geliştirme/debug amaçlıdır.

### Sıfırdan temiz başlatma

```bash
docker compose down -v && docker compose up --build
```

---

## Demo Kullanıcıları

Seed verisi otomatik yüklenir; tüm hesaplar hazırdır (ayrıntı: [docs/SEED_DATA.md](docs/SEED_DATA.md)).

### Personel / yönetim — e-posta + şifre

| Rol | E-posta | Şifre | Yetki |
|---|---|---|---|
| **Admin** | `admin@campaigncell.com` | `Admin.2026!` | Personel hesabı oluşturur, rol yönetir, audit log görür |
| **Süpervizör** | `supervizor@campaigncell.com` | `Super.2026!` | Dashboard, manuel atama, onay, tüm kayıtlar |
| **Uzman** (churn) | `deniz.karaca@campaigncell.com` | `Uzman.2026!` | Uzmanlık `RISKLI_KAYIP` — Altın seviye (1600 puan) |
| **Uzman** (değer) | `merve.aksoy@campaigncell.com` | `Uzman.2026!` | Uzmanlık `YUKSEK_DEGER` — Gümüş (750) |
| **Uzman** (yeni/pasif) | `kaan.erdem@campaigncell.com` | `Uzman.2026!` | `YENI_ABONE` + `PASIF` — Bronz (350) |
| **Uzman** (genel) | `ece.yildiz@campaigncell.com` | `Uzman.2026!` | Tüm segmentler — Bronz (80) |

> Uzmanların uzmanlık alanları **bilerek farklı** seçildi: akıllı atama algoritmasının farklı
> vakaları farklı kişilere yönlendirdiği demoda görünür olsun diye.

### Aboneler — GSM + OTP

**OTP kodu tüm aboneler için `1234`** (case §3.1 simülasyonu).

| GSM | Ad Soyad | Segment profili |
|---|---|---|
| `5321104501` | Ahmet Yılmaz | YUKSEK_DEGER |
| `5335562309` | Ayşe Demir | YUKSEK_DEGER |
| `5427718845` | Mehmet Kaya | YUKSEK_DEGER |
| `5309934417` | Elif Şahin | RISKLI_KAYIP |
| `5548220196` | Mustafa Çelik | RISKLI_KAYIP |
| `5361447083` | Zeynep Arslan | RISKLI_KAYIP |
| `5053396728` | Emre Doğan | YENI_ABONE |
| `5442685134` | Fatma Koç | YENI_ABONE |
| `5317059262` | Burak Aydın | PASIF |
| `5386671950` | Selin Öztürk | PASIF |

### Hazır demo verisi

- **2 kampanya** ve **3 optimizasyon vakası** — biri `KRITIK` önceliğinde ve SLA'sı ~30 dakika
  sonra doluyor (dashboard'da kırmızı görünür), biri `BELIRSIZ` segmentle manuel atama kuyruğunda
- **Gamification:** 4 uzman dört ayrı seviyede, 6 rozetlik katalog, dağıtılmış rozetler
- **AI:** eğitilmiş model künyesi + 10 tahmin + 1 düzeltme → doğruluk metriği **%90** görünür

---

## Öne çıkan özellikler

**Güvenlik.** Parolalar bcrypt (work factor 11) ile hash'lenir. JWT (15 dk) + refresh token
**rotation** ve theft koruması: geçersiz kılınmış bir token yeniden kullanılırsa kullanıcının
tüm oturumları kapatılır. 5 hatalı girişte 15 dakika hesap kilidi (kalan süre yanıtta döner).
Yetki matrisi endpoint seviyesinde uygulanır, her 403 audit log'a yazılır. Gateway'de rate
limiting. Tüm sorgular parametriktir (EF Core / SQLAlchemy) — string birleştirme yoktur.

**Dayanıklılık.** Transactional Outbox sayesinde "veritabanına yazıldı ama event kayboldu"
durumu imkânsızdır. AI Service erişilemezse Campaign kampanyayı yine oluşturur
(`segment: BELIRSIZ`, `priority: ORTA`, manuel kuyruk) — **bir servis çökse bile sistemin
geri kalanı çalışmaya devam eder.**

**AI.** Kendi ürettiğimiz 300 satırlık sentetik veri setiyle eğitilmiş scikit-learn modelleri:
segment sınıflandırma **%96.7 doğruluk**, kampanya tipi başına kabul olasılığı tahmini.
Eğitim verisi ve üretim betiği repodadır (`services/ai/data`, `services/ai/ml`), süreç
[docs/AI_APPROACH.md](docs/AI_APPROACH.md)'de anlatılır.

**Gerçek zamanlı.** SignalR ile rozet bildirimi (toast) ve canlı liderlik tablosu.

---

## Proje yapısı

```
├─ docker-compose.yml       # tüm sistem: 4 servis + 4 DB + Redis + RabbitMQ + gateway + frontend
├─ EVENTS.md                # event kataloğu ve payload sözleşmeleri
├─ docs/
│  ├─ AI_APPROACH.md        # model, eğitim verisi, süreç
│  ├─ SEED_DATA.md          # demo verisi ve sabit GUID sözleşmesi
│  └─ seed/                 # seed verisinin kaynak JSON'ları
├─ gateway/                 # YARP — routing, JWT, rate limiting
├─ services/
│  ├─ identity/ campaign/ gamification/   # .NET 8 — Clean Architecture
│  │   └─ src/{Domain, Application, Infrastructure, Api} + tests/
│  ├─ ai/                   # FastAPI + scikit-learn (app/, ml/, data/)
│  └─ shared/BuildingBlocks # ortak event zarfı, MassTransit, middleware
└─ frontend/                # React 18 + Vite + TypeScript
```

Her servisin kendi `README.md` ve `.env.example` dosyası vardır.

---

## Ekip

| Kişi | Sorumluluk |
|---|---|
| **Mali** | Tech Lead — mimari, backend iş mantığı, gateway |
| **İskender** | Veritabanı şemaları, EF Core / SQLAlchemy modelleri, migration, seed, AI eğitim verisi |
| **Osman** | Frontend (React), UI/UX, API tüketimi |
