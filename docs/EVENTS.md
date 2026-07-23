# Event Kataloğu

> Bu dosya `Core_Principles.md` §8'den üretilir ve kodla (`BuildingBlocks.Events.EventTypes`,
> her servisin `Events/*.cs` payload kayıtları) birebir senkron tutulur. Kod ile bu dosya
> arasında fark oluşursa **kod** kaynak kabul edilir, bu dosya güncellenir.

## Altyapı

- **Taşıyıcı:** RabbitMQ, `campaigncell.events` adlı **topic exchange**.
- **Routing key:** event'in `event_type` değeriyle birebir aynıdır (ör. `campaign.optimized`).
- **Serileştirme:** MassTransit `UseRawJsonSerializer()` — MassTransit'e özgü zarf/header eklenmez,
  JSON gövde aşağıdaki ortak zarfın **birebir** kendisidir. Bu, Python (`aio-pika`) tarafının
  MassTransit'in kendi meta-protokolünü bilmeden sade JSON okuyabilmesi için kasıtlıdır.
- **İsimlendirme:** event JSON alanları **snake_case** (Core_Principles §4) — REST API'lerin
  camelCase sözleşmesinden farklıdır, çünkü bu servisler-arası (ve AI/Python) bir sözleşmedir.
- **Şema kaynağı:** `services/shared/BuildingBlocks/Events/IntegrationEvent.cs` (ortak zarf) +
  her servisin kendi `Events/*.cs` dosyasındaki payload `record`'ları (somut alanlar).

### Ortak zarf

```json
{
  "event_id": "5b6f6e6a-...-uuid",
  "event_type": "campaign.optimized",
  "timestamp": "2026-07-18T14:22:10Z",
  "version": 1,
  "payload": { "...": "event'e özel alanlar, asagida" }
}
```

## Event tablosu

| Event (`event_type`) | Yayıncı | Dinleyen | Kaynak dosya |
|---|---|---|---|
| [`campaign.created`](#campaigncreated) | Campaign | — (ileride audit/loose coupling) | `Campaign.Application/Events/CampaignEvents.cs` |
| [`case.created`](#casecreated) | Campaign | — | `Campaign.Application/Events/CampaignEvents.cs` |
| [`case.assigned`](#caseassigned) | Campaign | — (şu an dinleyen yok; case §8 ayrıca "Gamification sayaç" öngörüyordu, henüz tüketici yazılmadı) | `Campaign.Application/Events/CampaignEvents.cs` |
| [`case.status_changed`](#casestatus_changed) | Campaign | — | `Campaign.Application/Events/CampaignEvents.cs` |
| [`campaign.optimized`](#campaignoptimized) | Campaign | **Gamification** (puan hesaplama) | `Campaign.Application/Events/CampaignEvents.cs` → `Gamification.Application/Events/GamificationEvents.cs` |
| [`case.sla_breached`](#casesla_breached) | Campaign | **Gamification** (-5 puan) | aynı |
| [`segment.overridden`](#segmentoverridden) | Campaign | **AI** (doğruluk metriği) | `Campaign.Application/Events/CampaignEvents.cs` → `services/ai/app/consumers.py` |
| [`offer.responded`](#offerresponded) | Campaign | **AI** (skor ayarı) | aynı |
| [`offer.rated`](#offerrated) | Campaign | **Gamification** (1-2★ → -3 puan) | `Campaign.Application/Events/CampaignEvents.cs` → `Gamification.Application/Events/GamificationEvents.cs` |
| [`badge.earned`](#badgeearned) | Gamification | Frontend (**SignalR push**, RabbitMQ'ya çıkmaz) | `Gamification.Api/Realtime/GameNotifier.cs` |
| [`points.updated`](#pointsupdated) | Gamification | Frontend (**SignalR push**, RabbitMQ'ya çıkmaz) | `Gamification.Api/Realtime/GameNotifier.cs` |

`badge.earned` ve `points.updated`, RabbitMQ'ya **yayınlanmaz** — Gamification bu ikisini doğrudan
SignalR (`GameHub`, `/hubs/game`) üzerinden ilgili uzmanın tarayıcısına push eder. Aynı
`EventTypes` sabitleri SignalR metod adı olarak da kullanılır ki frontend tek bir isim
sözlüğünden dinleyebilsin (REST/RabbitMQ/SignalR arasında isim farkı yok).

---

### `campaign.created`

Yeni bir kampanya oluşturulduğunda (`CreateCampaignCommand`) yayınlanır.

| Alan | Tip | Açıklama |
|---|---|---|
| `campaign_id` | uuid | Kampanya id'si |
| `campaign_number` | string | `CMP-2026-000123` formatı |
| `type` | string (enum) | `EK_PAKET` vb. |
| `target_segment` | string (enum) | Hedef segment |
| `created_by` | uuid | Oluşturan süpervizörün user id'si |

### `case.created`

Kampanyayla birlikte açılan optimizasyon vakası için yayınlanır.

| Alan | Tip | Açıklama |
|---|---|---|
| `case_id` | uuid | Vaka id'si |
| `campaign_id` | uuid | Bağlı kampanya |
| `segment` | string (enum) | AI tahmini segment (AI kapalıysa `BELIRSIZ`) |
| `priority` | string (enum) | `DUSUK`\|`ORTA`\|`YUKSEK`\|`KRITIK` |
| `sla_deadline` | datetime (ISO 8601) | SLA bitiş zamanı |

### `case.assigned`

Bir vaka bir uzmana atandığında (AI otomatik veya süpervizör manuel) yayınlanır.

| Alan | Tip | Açıklama |
|---|---|---|
| `case_id` | uuid | Vaka id'si |
| `expert_id` | uuid | Atanan uzman (Identity user id) |
| `assigned_by` | string | `"SYSTEM"` (AI/otomatik) veya `"MANUEL"` (süpervizör) |

### `case.status_changed`

Case state machine'de her geçiş sonrası yayınlanır (Core_Principles §7 tablosu).

| Alan | Tip | Açıklama |
|---|---|---|
| `case_id` | uuid | Vaka id'si |
| `from_status` | string (enum) | Önceki durum |
| `to_status` | string (enum) | Yeni durum |
| `changed_by` | uuid | Geçişi yapan kullanıcı (sistem geçişlerinde `00000000-0000-0000-0000-000000000000`) |

### `campaign.optimized`

Vaka `TAMAMLANDI` durumuna geçtiğinde yayınlanır — Gamification'ın ana puan tetikleyicisi.

| Alan | Tip | Açıklama |
|---|---|---|
| `case_id` | uuid | Vaka id'si |
| `expert_id` | uuid | İlgili uzman |
| `segment` | string (enum) | Vaka segmenti |
| `priority` | string (enum) | Vaka önceliği (KRITIK & SLA içinde ise +15 kuralı burada değerlendirilir) |
| `conversion_lift` | decimal? | Dönüşüm artışı (varsa; hedef üzeri ise +15) |
| `created_at` | datetime | Vaka açılış zamanı |
| `completed_at` | datetime | Tamamlanma zamanı (süre < 2s ise +5 kuralı bu ikisinden hesaplanır) |

### `case.sla_breached`

SLA Worker (dakikalık `BackgroundService`) deadline'ı geçmiş ve henüz işaretlenmemiş vakalar için yayınlar.

| Alan | Tip | Açıklama |
|---|---|---|
| `case_id` | uuid | Vaka id'si |
| `expert_id` | uuid? | Atanmış uzman (henüz atanmamışsa null) |
| `priority` | string (enum) | Vaka önceliği |
| `breached_at` | datetime | İhlalin işaretlendiği an |

### `segment.overridden`

Süpervizör AI'nin tahmin ettiği segmenti elle değiştirdiğinde (`OverrideSegmentCommand`) yayınlanır — AI servisi bunu doğruluk metriği için dinler.

| Alan | Tip | Açıklama |
|---|---|---|
| `case_id` | uuid | Vaka id'si |
| `predicted_segment` | string (enum) | AI'nin tahmini |
| `corrected_segment` | string (enum) | Süpervizörün düzelttiği değer |
| `changed_by` | uuid | Süpervizör user id'si |

### `offer.responded`

Müşteri bir teklife KABUL/RET yanıtı verdiğinde (`RespondToOfferCommand`) yayınlanır — AI skor ayarı için dinler.

| Alan | Tip | Açıklama |
|---|---|---|
| `offer_id` | uuid | Teklif id'si |
| `subscriber_id` | uuid | Müşteri id'si |
| `campaign_id` | uuid | Kampanya id'si |
| `campaign_type` | string (enum) | Kampanya türü |
| `response` | string | `"KABUL"` veya `"RET"` |

### `offer.rated`

Uzman bir teklifi puanladığında (`RateOfferCommand`, tek seferlik) yayınlanır.

| Alan | Tip | Açıklama |
|---|---|---|
| `offer_id` | uuid | Teklif id'si |
| `subscriber_id` | uuid | Müşteri id'si |
| `expert_id` | uuid? | İlgili uzman |
| `campaign_id` | uuid | Kampanya id'si |
| `stars` | int | 1-5 arası; 1-2★ Gamification'da -3 puan tetikler |

### `badge.earned`

Bir uzman yeni bir rozet kazandığında SignalR üzerinden (RabbitMQ **değil**) ilgili uzmana push edilir. JSON alanları **camelCase**'dir (SignalR `JsonNamingPolicy.CamelCase` ile serileştirir — REST sözleşmesiyle tutarlı, RabbitMQ zarfının snake_case'inden farklıdır).

| Alan | Tip | Açıklama |
|---|---|---|
| `expertId` | uuid | Rozeti kazanan uzman |
| `badgeCode` | string | Rozet kodu (ör. `ILK_TAMAMLANMA`) |

> Not: Core_Principles §8'in orijinal taslağında `badge_name` alanı da öngörülmüştü; mevcut
> `GameNotifier` implementasyonu sadece `badgeCode` gönderiyor (frontend kodu bunu yerel bir
> sözlükten adına çeviriyor). İleri bir iyileştirme olarak payload'a `badgeName` eklenebilir.

### `points.updated`

Bir uzmanın puanı değiştiğinde SignalR üzerinden (RabbitMQ **değil**) ilgili uzmana push edilir. JSON alanları **camelCase**'dir.

| Alan | Tip | Açıklama |
|---|---|---|
| `expertId` | uuid | İlgili uzman |
| `pointsDelta` | int | Bu olayda eklenen/çıkarılan puan (+10, +5, +15, -5, -3 vb.) |
| `totalPoints` | int | Güncel toplam puan |

> Not: Core_Principles §8'in orijinal taslağında `reason` alanı da öngörülmüştü; mevcut
> implementasyon bunu payload'a eklemiyor (sebep sunucu loglarında/DB'de tutuluyor, frontend'e
> taşınmıyor). İleri bir iyileştirme olarak eklenebilir.

---

## Puan kuralları (Gamification consumer'ları)

`campaign.optimized`, `case.sla_breached` ve `offer.rated` event'lerini dinleyen consumer'ların
uyguladığı kurallar (`Gamification.Application/Commands/Process*`, `Domain/Services/PointRuleEngine.cs`):

| Kural | Puan |
|---|---|
| Vaka TAMAMLANDI | +10 |
| Tamamlama süresi < 2 saniye (demo senaryosu) | +5 |
| `conversion_lift` hedefin üzerinde | +15 |
| KRITIK öncelik & SLA içinde tamamlandı | +15 |
| SLA aşımı (`case.sla_breached`) | -5 |
| Teklif 1-2★ puanlandı (`offer.rated`) | -3 |

## Tüketici kuyrukları

Her tüketici servis, paylaşılan topic exchange'e kendi kuyruğunu **açık routing key eşleşmesiyle**
bağlar (`BuildingBlocks.Messaging.IntegrationEventTopologyExtensions.ReceiveIntegrationEvent`):

| Servis | Kuyruk | Dinlediği event |
|---|---|---|
| Gamification | `gamification.campaign-optimized` | `campaign.optimized` |
| Gamification | `gamification.case-sla-breached` | `case.sla_breached` |
| Gamification | `gamification.offer-rated` | `offer.rated` |
| AI (Python, `aio-pika`) | `ai-service.events` | `segment.overridden`, `offer.responded` |
