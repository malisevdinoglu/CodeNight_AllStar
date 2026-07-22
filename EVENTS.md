# EVENTS.md — Servisler Arası Event Kataloğu

> CampaignCell'de servisler arası iletişimin kuralı: **Komut = REST, Olan biten = Event.**
> Cevabı hemen gereken çağrılar (AI skorlama) senkron REST; gerçekleşmiş her olay asenkron event.
> **Gamification Service'e asla doğrudan REST çağrısı yapılmaz** — yalnızca event dinler (case §6 şartı).
>
> Bu doküman koddaki gerçek sözleşmelerden üretilmiştir:
> `services/shared/BuildingBlocks/Events/`, `services/campaign/src/Campaign.Application/Events/CampaignEvents.cs`,
> `services/gamification/src/Gamification.Application/Events/GamificationEvents.cs`.

---

## 1. Altyapı

| Konu | Değer |
|---|---|
| Broker | RabbitMQ 3.13 (`rabbitmq:5672`, yönetim UI `:15672`) |
| Kütüphane | MassTransit (.NET tarafı), aio-pika (Python/AI tarafı) |
| Exchange | **`campaigncell.events`** — tek ortak **topic** exchange |
| Routing key | Event'in `event_type` değeri (örn. `campaign.optimized`) |
| Serileştirme | **Raw JSON** (`UseRawJsonSerializer`) — MassTransit'e özel zarf **eklenmez** |
| Atomiklik | **Transactional Outbox** (Campaign): DB commit + event publish tek transaction |

**Neden raw JSON + snake_case?** AI Service Python'dur. MassTransit'in kendi zarfını kullansaydık
Python tarafı mesajı çözemezdi. Bu yüzden tüm event JSON alanları **snake_case**'dir ve
yayın `PublishIntegrationEventAsync` üzerinden yapılır — bu helper routing key'i otomatik
`event_type` yapar. Çıplak `IPublishEndpoint.Publish` **kullanılmaz** (routing key boş kalır,
Python mesajı yakalayamaz).

---

## 2. Ortak Zarf

Tüm event'ler `IntegrationEvent` zarfını taşır:

```json
{
  "event_id": "a7f3c2e1-0000-0000-0000-000000000001",
  "event_type": "campaign.optimized",
  "timestamp": "2026-07-22T14:22:10Z",
  "version": 1,
  "payload": { }
}
```

| Alan | Tip | Açıklama |
|---|---|---|
| `event_id` | uuid | Benzersiz. **Tüketicide idempotency anahtarı** — `point_transactions.event_id` UNIQUE olduğu için aynı event iki kez puanlanamaz (RabbitMQ redelivery koruması). |
| `event_type` | string | Routing key ile aynı değer. |
| `timestamp` | ISO-8601 UTC | Yayın anı. |
| `version` | int | Şema sürümü (şu an tümü `1`). |
| `payload` | object | Event'e özel alanlar (aşağıda). |

---

## 3. Event Kataloğu

| Event | Yayıncı | Tüketici | Amaç |
|---|---|---|---|
| `campaign.created` | Campaign | — (audit/loose coupling) | Kampanya oluşturuldu |
| `case.created` | Campaign | — | Optimizasyon vakası açıldı |
| `case.assigned` | Campaign | Gamification (sayaç) | Vaka uzmana atandı |
| `case.status_changed` | Campaign | — | State machine geçişi |
| `campaign.optimized` | Campaign | **Gamification** | Vaka TAMAMLANDI → puan/rozet |
| `case.sla_breached` | Campaign | **Gamification** | SLA aşıldı → −5 puan |
| `segment.overridden` | Campaign | **AI** | Uzman AI segmentini değiştirdi → doğruluk metriği |
| `offer.responded` | Campaign | **AI** | Abone teklifi yanıtladı → skor ayarı |
| `offer.rated` | Campaign | **Gamification** | Abone 1-5 yıldız verdi → düşük puan cezası |

### Gamification tüketici kuyrukları (dayanıklı)

| Kuyruk | Dinlediği routing key |
|---|---|
| `gamification.campaign-optimized` | `campaign.optimized` |
| `gamification.case-sla-breached` | `case.sla_breached` |
| `gamification.offer-rated` | `offer.rated` |

---

## 4. Payload Sözleşmeleri

### `campaign.created`
```json
{
  "campaign_id": "uuid", "campaign_number": "CMP-2026-000123",
  "type": "EK_PAKET", "target_segment": "YUKSEK_DEGER", "created_by": "uuid"
}
```

### `case.created`
```json
{
  "case_id": "uuid", "campaign_id": "uuid",
  "segment": "RISKLI_KAYIP", "priority": "KRITIK",
  "sla_deadline": "2026-07-22T16:22:10Z"
}
```

### `case.assigned`
```json
{ "case_id": "uuid", "expert_id": "uuid", "assigned_by": "AI" }
```
`assigned_by`: `"AI"` (akıllı atama) veya `"MANUEL"` (yönetici ataması).

### `case.status_changed`
```json
{
  "case_id": "uuid", "from_status": "ATANDI",
  "to_status": "OPTIMIZE_EDILIYOR", "changed_by": "uuid"
}
```

### `campaign.optimized` — Gamification'ın ana girdisi
```json
{
  "case_id": "uuid", "expert_id": "uuid",
  "segment": "RISKLI_KAYIP", "priority": "YUKSEK",
  "conversion_lift": 0.18,
  "created_at": "2026-07-22T13:40:02Z",
  "completed_at": "2026-07-22T14:22:10Z"
}
```
Gamification bu event'ten süreyi (`completed_at − created_at`) ve dönüşümü hesaplayıp
puanı yazar, rozet koşullarını kontrol eder.

### `case.sla_breached`
```json
{ "case_id": "uuid", "expert_id": "uuid|null", "priority": "KRITIK", "breached_at": "..." }
```
`expert_id` null olabilir (vaka henüz atanmamışken SLA aşılabilir).

### `segment.overridden` — AI doğruluk metriği
```json
{
  "case_id": "uuid", "predicted_segment": "PASIF",
  "corrected_segment": "RISKLI_KAYIP", "changed_by": "uuid"
}
```
AI Service bunu `classification_feedback` tablosuna yazar.
**Doğruluk = 1 − (feedback sayısı / toplam prediction).**

### `offer.responded` — AI skor ayarı
```json
{
  "offer_id": "uuid", "subscriber_id": "uuid", "campaign_id": "uuid",
  "campaign_type": "EK_PAKET", "response": "RET"
}
```
`response`: `"KABUL"` | `"RET"`. `RET` gelirse AI, o abone + kampanya tipi için
`score_adjustments` tablosuna ceza yazar → benzer kampanyaların öneri skoru düşer (case §4.5).

### `offer.rated`
```json
{
  "offer_id": "uuid", "subscriber_id": "uuid", "expert_id": "uuid|null",
  "campaign_id": "uuid", "stars": 2
}
```
1-2 yıldız → uzmana **−3 puan** (alakasız teklif cezası).

---

## 5. Puanlama Kuralları (Gamification tüketicileri)

`campaign.optimized` / `case.sla_breached` / `offer.rated` event'lerinden türetilir:

| Koşul | Puan |
|---|---|
| Optimizasyon tamamlandı | **+10** |
| Süre < 2 saat (hız bonusu) | **+5** |
| Dönüşüm hedefi aşıldı | **+15** |
| KRITIK vaka SLA içinde tamamlandı | **+15** |
| SLA aşımı | **−5** |
| Abone 1-2 yıldız verdi | **−3** |

---

## 6. Gerçek Zamanlı Bildirim (SignalR — RabbitMQ değil)

İki "event" RabbitMQ'dan **geçmez**; Gamification bunları doğrudan SignalR ile
istemciye push eder (`/hubs/game`):

| Sinyal | Tetikleyici | İçerik |
|---|---|---|
| `badge.earned` | Rozet koşulu sağlandı | `expert_id`, `badge_code`, `badge_name` |
| `points.updated` | Puan değişti | `expert_id`, `delta`, `total_points`, `reason` |

Gamification RabbitMQ'ya **yayın yapmaz** (bu yüzden Outbox'ı da yoktur) — yalnızca tüketir
ve sonucu SignalR ile yayar. Frontend rozet toast'ı ve canlı liderlik tablosunu buradan alır.

---

## 7. Dayanıklılık ve Hata Yönetimi

- **Outbox (Campaign):** Event, iş verisiyle aynı DB transaction'ında outbox tablosuna yazılır;
  ayrı bir süreç RabbitMQ'ya iletir. Yani "DB'ye yazıldı ama event kayboldu" durumu **imkânsız**.
  Demo senaryosunun 7. adımında (servis kapatma) mesajların kaybolmamasının sebebi budur.
- **Idempotency:** Tüketici tarafında `event_id` UNIQUE kısıtı ile korunur — RabbitMQ bir mesajı
  tekrar teslim ederse puan iki kez yazılmaz.
- **Dayanıklı kuyruklar:** Gamification kapalıyken yayınlanan event'ler kuyrukta bekler,
  servis açılınca işlenir (mesaj kaybı yok).
- **Graceful degradation:** AI Service erişilemezse Campaign kampanyayı yine oluşturur
  (`segment: BELIRSIZ`, `priority: ORTA`, manuel kuyruk) — bu REST tarafındadır, event akışını etkilemez.
