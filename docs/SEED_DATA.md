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
- **Uzman ID eşlemesi e-posta üzerinden**: servisler arası FK olmadığı için seeder'lar
  Identity seed'inin ürettiği UUID'leri e-posta anahtarıyla eşler (compose sırası: identity önce).
- AI `model_metadata` seed'i JSON'dan değil `ml/metrics.json`'dan gelir (eğitim çıktısı — tek kaynak).
