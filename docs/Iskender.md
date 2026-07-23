# İskender — Veritabanı / Modeller / Context Yol Haritası

> Önce `Core_Principles.md` oku. Bu dosya senin adım adım görev listen.
> Sorumluluk: 4 servisin şemaları, EF Core entity + DbContext + configuration + migration, seed (mock) data, AI eğitim veri seti.
> Kural: DB tablo/kolonları snake_case (`UseSnakeCaseNamingConvention` paketi), enum'lar string olarak saklanır (Türkçe UPPER_SNAKE — okunabilirlik + debug kolaylığı).

---

## 0. Çalışma Şekli

Her servisin **kendi** DbContext'i kendi Infrastructure projesinde yaşar; servisler arası FK YOKTUR — başka servisin ID'si sadece `uuid` kolon olarak tutulur (örn. Campaign'deki `assigned_expert_id` Identity'ye FK değildir). Bu, database-per-service'in şemadaki karşılığıdır ve jüriye böyle anlatılır.

Sıra: Identity → Campaign → Gamification → AI eğitim verisi → Seed. Her şema bitince migration + commit → Mali handler'ları bağlar.

---

## 1. Identity DB (`identity-db`, şema: IdentityDbContext)

```
users
  id uuid PK (default gen_random_uuid())
  first_name varchar(60) NOT NULL
  last_name varchar(60) NOT NULL
  gsm_number varchar(10) UNIQUE NULL        -- 5xxxxxxxxx, sadece aboneler
  email varchar(120) UNIQUE NULL            -- personel/admin zorunlu, abone opsiyonel
  password_hash varchar(200) NULL           -- abonede NULL (OTP ile girer)
  role varchar(20) NOT NULL                 -- MUSTERI | PERSONEL | SUPERVIZOR | ADMIN
  region varchar(30) NULL
  is_active bool NOT NULL default true
  failed_login_count int NOT NULL default 0
  locked_until timestamptz NULL             -- kilit: now + 15 dk
  created_at timestamptz NOT NULL

user_expertises                              -- personelin uzmanlık alanları (N adet)
  id uuid PK
  user_id uuid FK->users NOT NULL
  segment_type varchar(20) NOT NULL          -- YUKSEK_DEGER | RISKLI_KAYIP | YENI_ABONE | PASIF
  UNIQUE(user_id, segment_type)

refresh_tokens
  id uuid PK
  user_id uuid FK->users NOT NULL
  token_hash varchar(64) NOT NULL            -- SHA-256, düz token ASLA saklanmaz
  expires_at timestamptz NOT NULL            -- +7 gün
  revoked_at timestamptz NULL
  replaced_by_id uuid NULL FK->refresh_tokens  -- rotation zinciri (theft tespiti)
  created_by_ip varchar(45)
  INDEX(user_id), INDEX(token_hash)

audit_logs
  id bigserial PK
  user_id uuid NULL                          -- başarısız girişte bilinmeyebilir
  action_type varchar(50) NOT NULL           -- LOGIN_SUCCESS, LOGIN_FAILED, ACCOUNT_LOCKED,
                                             -- ROLE_CHANGED, ACCESS_DENIED, CAMPAIGN_DELETED, STATUS_CHANGED_CRITICAL
  occurred_at timestamptz NOT NULL
  ip_address varchar(45) NOT NULL
  success bool NOT NULL
  resource_id varchar(60) NULL
  details jsonb NULL
  INDEX(user_id, occurred_at DESC)
```

- [ ] Entity + `IEntityTypeConfiguration` sınıfları (Fluent API; data annotation kullanma — Application katmanı temiz kalsın)
- [ ] Migration: `InitialIdentity`

## 2. Campaign DB (`campaign-db`, şema: CampaignDbContext)

```
subscriber_profiles                          -- abone kullanım profili (AI'nın girdisi)
  subscriber_id uuid PK                      -- Identity'deki user id ile AYNI değer (FK değil!)
  gsm_number varchar(10) NOT NULL
  current_plan varchar(40) NOT NULL          -- örn. "GNC 20GB", "Platinum 40GB"
  tenure_months int NOT NULL
  avg_monthly_data_gb numeric(6,2) NOT NULL
  avg_monthly_call_minutes int NOT NULL
  monthly_spend_tl numeric(8,2) NOT NULL
  package_purchase_count int NOT NULL        -- son 6 ay ek paket alımı
  complaint_count int NOT NULL
  days_since_last_activity int NOT NULL
  past_acceptance_rate numeric(3,2) NOT NULL -- 0.00-1.00
  current_segment varchar(20) NULL           -- AI'nın son sınıflandırması

campaigns
  id uuid PK
  campaign_number varchar(20) UNIQUE NOT NULL   -- CMP-2026-000123 (sequence: campaign_number_seq)
  title varchar(150) NOT NULL
  type varchar(20) NOT NULL                  -- EK_PAKET | TARIFE_YUKSELTME | CIHAZ_FIRSATI | SADAKAT
  target_segment varchar(20) NOT NULL
  discount_rate numeric(5,2) NOT NULL        -- yüzde
  valid_from date NOT NULL
  valid_until date NOT NULL
  status varchar(20) NOT NULL default 'AKTIF'   -- AKTIF | ARSIVLENDI
  created_by uuid NOT NULL
  created_at timestamptz NOT NULL

optimization_cases
  id uuid PK
  case_number varchar(20) UNIQUE NOT NULL    -- OPT-2026-000045
  campaign_id uuid FK->campaigns NOT NULL
  segment varchar(20) NOT NULL               -- + BELIRSIZ (AI kapalıyken)
  priority varchar(10) NOT NULL              -- DUSUK | ORTA | YUKSEK | KRITIK
  status varchar(25) NOT NULL default 'YENI' -- state machine (Core_Principles §7)
  assigned_expert_id uuid NULL
  sla_deadline timestamptz NOT NULL          -- created_at + SLA(priority)
  sla_breached bool NOT NULL default false
  expert_note text NULL                      -- TAMAMLANDI'da zorunlu
  conversion_lift numeric(4,2) NULL
  created_at / completed_at timestamptz
  INDEX(status, priority), INDEX(assigned_expert_id, status)

case_status_history
  id bigserial PK
  case_id uuid FK->optimization_cases
  from_status / to_status varchar(25)
  changed_by uuid NOT NULL                   -- veya 'SYSTEM' sentinel uuid
  note text NULL
  changed_at timestamptz NOT NULL

offers
  id uuid PK
  campaign_id uuid FK->campaigns NOT NULL
  subscriber_id uuid NOT NULL                -- FK değil (cross-service)
  recommendation_score numeric(3,2) NOT NULL -- 0.60 altı zaten yaratılmaz
  conversion_probability numeric(3,2) NOT NULL
  is_priority bool NOT NULL                  -- skor > 0.80
  status varchar(15) NOT NULL default 'SUNULDU'  -- SUNULDU | KABUL | RET
  responded_at timestamptz NULL
  created_at timestamptz NOT NULL
  UNIQUE(campaign_id, subscriber_id)

offer_ratings
  id uuid PK
  offer_id uuid FK->offers UNIQUE NOT NULL   -- UNIQUE = tek seferlik puanlama garantisi (409)
  subscriber_id uuid NOT NULL
  stars smallint NOT NULL CHECK (1<=stars<=5)
  created_at timestamptz NOT NULL
```

- [ ] `CampaignNumberFactory` için sequence'ler: `campaign_number_seq`, `case_number_seq` (migration'da raw SQL)
- [ ] MassTransit Outbox tabloları: `AddEntityFrameworkOutbox` migration'ı (Mali ile birlikte)

## 3. Gamification DB (`gamification-db` + Redis)

```
expert_scores
  expert_id uuid PK
  display_name varchar(120) NOT NULL         -- event'ten denormalize (Identity'ye sormamak için)
  total_points int NOT NULL default 0
  completed_case_count int NOT NULL default 0
  fast_completion_count int NOT NULL default 0     -- <2 saat sayacı (Hız Ustası)
  target_exceeded_count int NOT NULL default 0     -- (Dönüşüm Kralı)
  riskli_kayip_saved_count int NOT NULL default 0  -- (Churn Avcısı)
  updated_at timestamptz

point_transactions
  id bigserial PK
  expert_id uuid NOT NULL
  event_id uuid UNIQUE NOT NULL              -- idempotency: aynı event iki kez puanlanmaz
  reason varchar(40) NOT NULL                -- OPTIMIZASYON_TAMAMLANDI, HIZ_BONUSU, HEDEF_ASILDI,
                                             -- KRITIK_SLA_ICINDE, SLA_ASIMI, DUSUK_PUAN
  points int NOT NULL                        -- +10 +5 +15 +15 -5 -3
  case_id uuid NULL
  created_at timestamptz NOT NULL
  INDEX(expert_id, created_at)

badges (seed ile dolu)
  code varchar(30) PK        -- ILK_KAMPANYA, HIZ_USTASI, DONUSUM_KRALI, MARATONCU, CHURN_AVCISI, UZMAN
  name / description varchar
  
expert_badges
  expert_id + badge_code composite PK
  earned_at timestamptz

segment_completion_counts                    -- UZMAN rozeti (tek segmentte 50)
  expert_id + segment composite PK
  completed_count int
```

**Redis anahtar sözleşmesi:** `leaderboard:daily:2026-07-22`, `leaderboard:weekly:2026-W30` (sorted set, member=expert_id, score=puan; TTL 30 gün). Profil sıralaması `ZREVRANK`.

## 4. AI DB (`ai-db`, SQLAlchemy modelleri)

```
predictions (id, subscriber_id, campaign_id, recommendation_score, conversion_probability,
             predicted_segment, model_version, created_at)
classification_feedback (id, case_id, predicted_segment, corrected_segment, corrected_by, created_at)
score_adjustments (subscriber_id, campaign_type, penalty numeric, updated_at, PK(subscriber_id, campaign_type))
model_metadata (version PK, trained_at, accuracy, f1_macro, notes)
```

Doğruluk metriği = `1 - (feedback sayısı / toplam prediction)`; kategori kırılımı `predicted_segment` group by (+3 bonus verisi buradan çıkar).

## 5. AI Eğitim Veri Seti (`services/ai/ml/generate_data.py`) — +8 bonusun temeli

- [ ] **300 satır** sentetik abone örneği üret (minimum 100 isteniyor; 300 güvenli). Kolonlar: `subscriber_profiles` alanları + `label_segment` + kampanya tipi başına `accepted` (0/1) geçmişi.
- [ ] Gerçekçi Türkçe personalar kural + gürültüyle üret (case'in verdiği örnek mantık):
  - **YUKSEK_DEGER:** yüksek harcama (400-900 TL), yüksek data (30-80 GB), düşük şikayet, yüksek kabul oranı
  - **RISKLI_KAYIP:** düşen kullanım, şikayet 3+, 30+ gün pasiflik, düşük kabul; churn sinyali
  - **YENI_ABONE:** tenure < 6 ay, orta kullanım
  - **PASIF:** düşük data/arama, 60+ gün aktivite yok, ek paket almıyor
  - Kabul etiketi mantığı: yüksek data kullanan EK_PAKET'i kabul etmeye yatkın, RISKLI_KAYIP SADAKAT indirimini kabul etmeye yatkın, PASIF çoğunlukla RET vb. + %10-15 gürültü (model %100 çıkmasın — gerçekçilik)
- [ ] Çıktı: `data/training_data.csv` **repoya commit'lenir** + üretim mantığı `docs/AI_APPROACH.md`'ye yazılır (case şartı).

## 6. Seed (Mock) Data — demo senaryosunun yakıtı

Her servisin `Infrastructure/Seed/` klasöründe idempotent seeder (`if (await db.Users.AnyAsync()) return;`). Compose ayağa kalkınca otomatik çalışır.

- [ ] **Identity:** 1 admin (`admin@campaigncell.com` / `Admin.2026!`), 1 süpervizör (`supervizor@campaigncell.com` / `Super.2026!`), 4 uzman — farklı expertise kombinasyonları: churn uzmanı (RISKLI_KAYIP), değer uzmanı (YUKSEK_DEGER), yeni abone uzmanı (YENI_ABONE+PASIF), genel (hepsi) — atama algoritmasının farklı sonuçlar vermesi demo'da görünür olsun. 10 abone (gerçekçi Türk isimleri, 5xx GSM'ler). **Bu bilgiler ana README'deki "Demo Kullanıcıları" tablosuna da yazılır.**
- [ ] **Campaign:** seed'deki 10 aboneye karşılık `subscriber_profiles` satırları — segment arketiplerine uygun dağıt (3 YUKSEK_DEGER, 3 RISKLI_KAYIP, 2 YENI_ABONE, 2 PASIF). 2 hazır kampanya + 3 optimizasyon vakası (YENI/ATANDI/OPTIMIZE_EDILIYOR karışık; biri KRITIK ve SLA'sı yaklaşmış — dashboard dolu görünsün).
- [ ] **Gamification:** 4 uzmana başlangıç puanları (350/750/1600/80 → dört seviye de ekranda görünür), 2-3 rozet dağıt, dün+bugün için leaderboard hareketleri.
- [ ] **AI:** model_metadata satırı + birkaç prediction (doğruluk metriği ekranı boş kalmasın; 1-2 feedback satırı → %90 civarı doğruluk görünür).

## 7. Teslim Kontrol Listesi

- [ ] 4 migration seti temiz çalışıyor (`compose down -v && compose up` ile sıfırdan doğrulama)
- [ ] Seed idempotent (iki kez up → duplicate yok)
- [ ] Tüm enum değerleri Core_Principles §4'teki Türkçe sabitlerle birebir
- [ ] training_data.csv commit'li, generate_data.py tekrar üretilebilir (seed=42)
- [ ] Entity configuration'larda: unique index'ler (özellikle `offer_ratings.offer_id`, `point_transactions.event_id`), check constraint'ler, cascade davranışları bilinçli seçilmiş
