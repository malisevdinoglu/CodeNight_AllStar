# Seed (Demo) Verisi — Tek Doğru Kaynak

> `docs/seed/*.json` dosyaları tüm servislerin seeder'larının veri kaynağıdır.
> C# seeder'lar (Infrastructure/Seed) ve AI seed'i bu dosyalardaki değerleri birebir kullanır —
> elle kopyalanan değer YOK, tutarsızlık riski sıfır. Seeder'lar idempotenttir
> (`if (await db.Users.AnyAsync()) return;`) — iki kez `compose up` → duplicate yok.

## Demo Kullanıcıları (ana README'ye kopyalanacak tablo)

| Rol | E-posta / GSM | Şifre / OTP | Not |
|---|---|---|---|
| Admin | `admin@campaigncell.com` | `Admin.2026!` | Personel hesabı oluşturur, audit log görür |
| Süpervizör | `supervizor@campaigncell.com` | `Super.2026!` | Dashboard, manuel atama, onay |
| Uzman (churn) | `deniz.karaca@campaigncell.com` | `Uzman.2026!` | Uzmanlık: RISKLI_KAYIP — Altın seviye |
| Uzman (değer) | `merve.aksoy@campaigncell.com` | `Uzman.2026!` | Uzmanlık: YUKSEK_DEGER — Gümüş seviye |
| Uzman (yeni/pasif) | `kaan.erdem@campaigncell.com` | `Uzman.2026!` | Uzmanlık: YENI_ABONE + PASIF — Bronz |
| Uzman (genel) | `ece.yildiz@campaigncell.com` | `Uzman.2026!` | Tüm segmentler — yeni başlamış |
| Abone (örnek) | GSM `5321104501` (Ahmet Yılmaz) | OTP `1234` | YUKSEK_DEGER profili |
| Abone (örnek) | GSM `5309934417` (Elif Şahin) | OTP `1234` | RISKLI_KAYIP profili |

Tüm aboneler (10 adet) OTP `1234` ile girer — tam liste `seed/identity_users.json`.

## Dosyalar

| Dosya | Hedef servis | İçerik |
|---|---|---|
| `seed/identity_users.json` | Identity | Admin, süpervizör, 4 uzman (farklı uzmanlık kombinasyonları — atama algoritması demo'da farklı sonuçlar versin), 10 abone |
| `seed/subscriber_profiles.json` | Campaign | 10 abonenin kullanım profili: 3 YUKSEK_DEGER, 3 RISKLI_KAYIP, 2 YENI_ABONE, 2 PASIF |
| `seed/campaigns_and_cases.json` | Campaign | 2 kampanya + 3 vaka (YENI/ATANDI/OPTIMIZE_EDILIYOR; biri KRITIK, SLA'sı yaklaşmış; biri BELIRSIZ → manuel kuyruk) |
| `seed/gamification.json` | Gamification | 4 uzmanın puanları (350/750/1600/80 → dört seviye ekranda), rozetler, dün+bugün leaderboard |

## Tasarım Kararları

- **Vaka zamanları relative** (`createdHoursAgo`): seed hangi saatte çalışırsa çalışsın
  KRITIK vakanın SLA'sı "30 dk kaldı" görünür — demo her zaman canlı hissettirir.
- **`expectedSegment` alanı** doğrulama içindir: AI modeli seed profillerini sınıflandırdığında
  beklenen segmenti bulmalı (entegrasyon testi verisi olarak da kullanılır).
- **Servisler arası ID koordinasyonu — sabit (deterministik) GUID'ler:** Database-per-service'te
  bir servis diğerinin ürettiği ID'yi runtime'da soramaz. Bu yüzden demo verisi önceden
  koordine edilmiş sabit GUID'lerle seed'lenir. Identity abone/uzmanı şu GUID'lerle üretir,
  Campaign (`subscriber_profiles`) ve Gamification (`expert_scores`) **aynı** sabitleri kullanır
  (FK değil — cross-service değer eşleşmesi). Her servisin `Persistence/Seeding/SeedIds.cs`'i
  bu tabloyu birebir taşır; değer değişirse üç seeder birden güncellenir:

  | Varlık | Sabit GUID |
  |---|---|
  | Admin | `a0000000-…-000000000001` |
  | Süpervizör | `50000000-…-000000000001` |
  | Uzman Deniz (RISKLI_KAYIP) | `e0000000-…-000000000001` |
  | Uzman Merve (YUKSEK_DEGER) | `e0000000-…-000000000002` |
  | Uzman Kaan (YENI_ABONE+PASIF) | `e0000000-…-000000000003` |
  | Uzman Ece (tüm segmentler) | `e0000000-…-000000000004` |
  | Abone 1–10 | `b0000000-…-0000000000NN` (NN = 01–10) |

- **Seeder'lar C# içinde (dosya IO yok):** Docker build context servis klasörüyle sınırlı
  (`docs/seed` image'a girmez); bu yüzden runtime seed verisi her servisin `Seeding/*DataSeeder.cs`'inde
  gömülüdür. `docs/seed/*.json` insan-okunur tasarım referansı ve bu tablonun kaynağıdır.
- **Vaka zamanları relative:** SLA sabitleri `createdAt`'e eklenir (KRITIK +2s, YUKSEK +8s, ORTA +24s)
  → seed ne zaman koşarsa koşsun KRITIK vaka ~30 dk kalmış görünür.
- AI `model_metadata` seed'i JSON'dan değil `ml/metrics.json`'dan gelir (eğitim çıktısı — tek kaynak).
